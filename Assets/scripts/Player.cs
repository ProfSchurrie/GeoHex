/*
 * Project: GeoHex
 * File: Player.cs
 * Author: Sören Coremans
 * Description:
 * Networked player entity. Manages nation selection workflow, per-player TradeOffer,
 * and links to the owned Nation object. The server authoritatively assigns nations and
 * transfers ownership of spawned network objects to the owning client.
 *
 * Flow:
 *  1) Owner calls RequestNation(n).
 *  2) Server observes request; if available, spawns Nation, assigns id/ownership,
 *     updates country and countryAssigned.
 *  3) Client UI closes the selector when assignment is confirmed.
 */

using System;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    /// <summary>
    /// Singleton reference to the local player's Player component (owner on this client).
    /// Set once on the owning client during Update when the object is ready.
    /// </summary>
    public static Player CurrentPlayer { get; private set; }

    /// <summary>
    /// The assigned nation tag for this player. 0 = unassigned.
    /// Server-writable; visible to all.
    /// </summary>
    private NetworkVariable<ushort> country =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// The nation tag the player requested to play as.
    /// Owner-writable; visible to all.
    /// </summary>
    private NetworkVariable<ushort> countryRequest =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Whether the player has submitted a nation request.
    /// Owner-writable; visible to all.
    /// </summary>
    private NetworkVariable<bool> countryRequested =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Whether the server has assigned a nation to this player.
    /// Server-writable; visible to all.
    /// </summary>
    private NetworkVariable<bool> countryAssigned =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// This player's persistent TradeOffer object (one per player).
    /// </summary>
    public TradeOffer tradeOffer;

    /// <summary>
    /// Prefab from which the server instantiates the player's TradeOffer.
    /// </summary>
    public TradeOffer tradeOfferPrefab;

    /// <summary>
    /// Tracks whether the nation selection UI has been closed on the owning client.
    /// (UI concern currently managed here.)
    /// </summary>
    bool nationsClosed = false;

    /// <summary>
    /// The Nation object owned by this player once assigned by the server.
    /// </summary>
    public Nation nation;

    /// <summary>
    /// Prefab from which the server instantiates the Nation.
    /// </summary>
    public Nation nationPrefab;

    private void Start()
    {
        Debug.Log("Player started");

        // Register with the authoritative game server.
        HexServer.Instance.AddPlayer(this);

        // Server spawns the per-player TradeOffer and transfers ownership to the player.
        if (NetworkManager.Singleton.IsServer)
        {
            tradeOffer = Instantiate(tradeOfferPrefab);
            tradeOffer.ChangeOwnership(NetworkObject.OwnerClientId);
            tradeOffer.transform.SetParent(transform);
        }

        Debug.Log("Player finished");
    }

    private void Update()
    {
        // Establish the local singleton when the owner is ready.
        // (Historically placed in Update to avoid race conditions if client Start ran before server ownership was finalized.)
        if (CurrentPlayer == null && IsOwner)
        {
            CurrentPlayer = this;
            HexMapEditor.Instance.player = this;
        }

        // Close nation selection UI on the owning client once assignment is confirmed.
        if (!nationsClosed && countryAssigned.Value && IsOwner)
        {
            NationSelector.Instance.gameObject.SetActive(false);
            nationsClosed = true;
        }

        // Server-side: process nation requests and assign nations if available.
        if (NetworkManager.Singleton.IsServer && !countryAssigned.Value && countryRequested.Value)
        {
            nation = Instantiate(nationPrefab);
            nation.id.Value = countryRequest.Value;
            nation.ChangeOwnership(NetworkObject.OwnerClientId);
            nation.transform.SetParent(transform);

            // Assign the nation via HexServer; transfer tile ownership and related setup occurs there.
            country.Value = (ushort)HexServer.Instance.getCountry(NetworkObject.OwnerClientId, countryRequest.Value);
            countryAssigned.Value = true;
        }
    }

    /// <summary>
    /// Owner-only: requests control of the given nation tag.
    /// The server will validate availability and assign if possible.
    /// </summary>
    public void RequestNation(ushort nation)
    {
        if (IsOwner && !countryAssigned.Value)
        {
            countryRequest.Value = nation;
            countryRequested.Value = true;
        }
    }

    /// <summary>
    /// Returns the assigned nation's tag (0 if unassigned).
    /// </summary>
    public ushort GetCountry()
    {
        return country.Value;
    }
}
