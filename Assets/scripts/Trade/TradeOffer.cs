/*
 * Project: GeoHex
 * File: TradeOffer.cs
 * Author: Sören Coremans
 * Description:
 * Represents a player's current trade proposal before it becomes a full TradeDeal.
 * Each player owns exactly one TradeOffer instance, which acts as a communication bridge
 * between the client UI, the Trade system, and the server. Once confirmed, its data is
 * transferred into a new TradeDeal object for processing and synchronization.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Network-synchronized data container representing a player's active trade offer.
/// Each player maintains exactly one instance, which stores the details of a pending
/// trade request until it is processed by the server and converted into a <see cref="TradeDeal"/>.
/// </summary>
public class TradeOffer : NetworkBehaviour
{
    /// <summary>
    /// The ID of the nation initiating the trade.
    /// </summary>
    public NetworkVariable<int> offeringCountry =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Deprecated legacy field from the old food system (retained for compatibility).
    /// </summary>
    public NetworkVariable<float> offeredFood =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// The one-time money transfer amount.  
    /// Positive = payment from the offering nation to the recipient.  
    /// Negative = payment from the recipient back to the offering nation.
    /// </summary>
    public NetworkVariable<float> offeredMoney =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Amount of food exchanged per round for the duration of the deal.
    /// </summary>
    public NetworkVariable<float> offeredFoodRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Amount of money exchanged per round for the duration of the deal.
    /// Positive = outgoing payments from the offering nation; negative = incoming.
    /// </summary>
    public NetworkVariable<float> offeredMoneyRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Amount of energy exchanged per round for the duration of the deal.
    /// Follows the same positive/negative direction convention as money.
    /// </summary>
    public NetworkVariable<float> offeredEnergyRounds =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Number of rounds (each ≈10 seconds) that the deal will remain active after acceptance.
    /// </summary>
    public NetworkVariable<int> rounds =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// The ID of the nation receiving this trade offer.
    /// </summary>
    public NetworkVariable<int> otherCountry =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Indicates whether the player has sent a trade request.  
    /// 0 = idle, 1 = trade request pending.
    /// </summary>
    public NetworkVariable<ushort> confirmOffer =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Indicates whether the server has already processed this offer.  
    /// 0 = not processed, 1 = processed.
    /// </summary>
    public NetworkVariable<ushort> offerProcessed =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Spawn();
        }
    }

    private void Start()
    {
        Debug.Log("Trade offer started");

        Player player = transform.parent.GetComponent<Player>();
        if (player == null)
        {
            Debug.Log("Player not found in trade offer start");
            return;
        }

        // Link this offer to its owning player instance
        player.tradeOffer = this;

        // Add event listener for when the server finishes processing the offer
        if (IsOwner)
        {
            offerProcessed.OnValueChanged += OfferProcessed;
        }

        Debug.Log("Trade offer finished");
    }

    /// <summary>
    /// Spawns the offer into the current network session (server-only).
    /// </summary>
    public void Spawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    public override void OnDestroy()
    {
        // Remove listener when the owning client is destroyed
        if (IsOwner)
        {
            offerProcessed.OnValueChanged -= OfferProcessed;
        }
    }

    /// <summary>
    /// Called when the player attempts to send a trade request.
    /// Validates the target and ensures the previous offer has been processed
    /// before notifying the server of a new trade.
    /// </summary>
    public void trade()
    {
        if (offeringCountry.Value == otherCountry.Value)
        {
            Debug.Log("A country can't trade with one self!");
            return;
        }

        if (offerProcessed.Value != 0)
        {
            Debug.Log("Wait for Server to Process the offer");
            return;
        }

        Debug.Log("Confirmed trade!");
        confirmOffer.Value = 1;
    }

    /// <summary>
    /// Event callback triggered when the server marks the offer as processed.
    /// Resets <see cref="confirmOffer"/> so the player can submit another trade.
    /// </summary>
    private void OfferProcessed(ushort oldValue, ushort newValue)
    {
        confirmOffer.Value = 0;
    }

    /// <summary>
    /// Transfers network ownership of this trade offer to another client.
    /// Only executable by the server.
    /// </summary>
    /// <param name="newOwnerClientId">The client ID to assign ownership to.</param>
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
