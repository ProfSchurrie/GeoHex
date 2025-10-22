/*
 * Project: GeoHex
 * File: TradeDeal.cs
 * Author: Sören Coremans
 * Description:
 * Represents a temporary, network-synchronized trade negotiation between two nations.
 * Each TradeDeal instance exists while an offer is being reviewed or processed.
 * Once accepted or rejected, the deal is transferred into the respective Nation classes
 * for long-term execution of recurring trade effects.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


/// <summary>
/// Represents a pending trade deal between two nations.
/// This component synchronizes trade parameters (e.g., money or resource flow)
/// between host and clients and manages acceptance, rejection, and local processing.
/// </summary>
/// <remarks>
/// <para>
/// TradeDeal objects are short-lived synchronization containers. Once accepted,
/// their data is transferred to the participating nations, after which the object
/// is destroyed. Regular resource transfers are then handled by the <see cref="Nation"/> class.
/// </para>
/// <para>
/// A TradeDeal may include immediate single transfers (e.g., money) or ongoing
/// round-based transfers (e.g., energy, food, money over time).
/// </para>
/// </remarks>
public class TradeDeal : NetworkBehaviour
{
    /// <summary>
    /// The ID of the nation offering the deal.
    /// </summary>
    public NetworkVariable<int> offeringCountry =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Single, immediate money transfer amount.
    /// Positive values indicate payment from the offering nation to the receiver;
    /// negative values indicate payment in the opposite direction.
    /// </summary>
    public NetworkVariable<float> offeredMoney =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Deprecated. Left for compatibility with older food-system logic.
    /// </summary>
    public NetworkVariable<float> offeredFood =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Amount of food transferred per round for the duration of the trade deal.
    /// (Obsolete and no longer used in current builds.)
    /// </summary>
    public NetworkVariable<float> offeredFoodRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Amount of money transferred each round for the duration of the trade deal.
    /// Positive = outgoing payments from the offering nation;
    /// negative = incoming payments.
    /// </summary>
    public NetworkVariable<float> offeredMoneyRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Amount of energy transferred each round for the duration of the trade deal.
    /// Positive and negative values follow the same direction convention as money.
    /// </summary>
    public NetworkVariable<float> offeredEnergyRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Total number of rounds (each 10 seconds) before the trade deal expires.
    /// </summary>
    public NetworkVariable<int> rounds =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// The ID of the nation receiving the offer.
    /// </summary>
    public NetworkVariable<int> otherCountry =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Current acceptance state of the trade:
    /// 0 = pending, 1 = accepted, 2 = rejected.
    /// </summary>
    public NetworkVariable<ushort> accept =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// The round in which the deal was accepted or declined.
    /// (Mostly informational — not used in current logic.)
    /// </summary>
    public NetworkVariable<int> round =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Indicates whether the offer has already been displayed to the receiving player.
    /// </summary>
    public bool shown = false;

    /// <summary>
    /// Tracks whether the deal has already been processed locally
    /// and handed off to the <see cref="Nation"/> for ongoing transfers.
    /// </summary>
    public bool processedLocal = false;

    /// <summary>
    /// Prefab for the UI element displayed when a player receives a trade offer.
    /// Contains buttons for accepting or declining the deal.
    /// </summary>
    public TradeReceived tradeReceivedPrefab;

    private void Awake()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Spawn();
        }
    }

    /// <summary>
    /// Spawns the trade deal object into the network session (server-only).
    /// </summary>
    public void Spawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    /// <summary>
    /// Displays this trade offer to the receiving player via a <see cref="TradeReceived"/> UI window.
    /// </summary>
    public void ShowOffer()
    {
        TradeReceived tradeReceived = Instantiate(tradeReceivedPrefab);
        tradeReceived.TradeDeal = this;
        tradeReceived.transform.parent = GameHUD.Instance.transform.parent.transform;
        tradeReceived.gameObject.SetActive(true);
        shown = true;
    }

    /// <summary>
    /// Called by the receiving player to accept or reject the trade deal.
    /// Adjusts the nation's gold immediately and forwards the deal for recurring transfers.
    /// </summary>
    /// <param name="isAccepted">Whether the deal was accepted (true) or declined (false).</param>
    public void Accept(bool isAccepted)
    {
        if (isAccepted)
        {
            accept.Value = 1;
            Nation nation = Player.CurrentPlayer.nation;
            nation.changeGold(offeredMoney.Value);

            if (rounds.Value > 0)
            {
                nation.AddTradeDeal(
                    offeredMoneyRounds.Value,
                    offeredFoodRounds.Value,
                    offeredEnergyRounds.Value,
                    rounds.Value
                );
            }
        }
        else
        {
            accept.Value = 2;
        }

        round.Value = Player.CurrentPlayer.nation.GetTime();
    }

    /// <summary>
    /// Called by the offering nation once the recipient has made a decision.
    /// If accepted, it deducts funds and schedules outgoing recurring transfers.
    /// </summary>
    public void Process()
    {
        if (accept.Value == 1)
        {
            Nation nation = Player.CurrentPlayer.nation;
            nation.changeGold(-offeredMoney.Value);

            if (rounds.Value > 0)
            {
                nation.AddTradeDeal(
                    -offeredMoneyRounds.Value,
                    -offeredFoodRounds.Value,
                    -offeredEnergyRounds.Value,
                    rounds.Value
                );
            }
        }

        processedLocal = true;
        // Note: In future refactors, consider setting this flag only once accept != 0
    }

    /// <summary>
    /// Transfers network ownership of this deal to a specified client.
    /// Only executable by the server.
    /// </summary>
    /// <param name="newOwnerClientId">The client ID that should assume ownership.</param>
    public void ChangeOwnership(ulong newOwnerClientId)
    {
        if (!IsServer)
        {
            Debug.LogError("Only the server can change ownership.");
            return;
        }

        if (NetworkObject != null)
        {
            NetworkObject.ChangeOwnership(newOwnerClientId);
            Debug.Log($"Ownership changed to client {newOwnerClientId}");
        }
        else
        {
            Debug.LogError("NetworkObject is null. Ensure the object is spawned.");
        }
    }
}
