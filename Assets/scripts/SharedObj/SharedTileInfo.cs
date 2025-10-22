/*
 * Project: GeoHex
 * File: SharedTileInfo.cs
 * Author: Sören Coremans
 * Description:
 * Network-synchronized state for a single hex tile. Mirrors mutable values
 * (plant/urban/water levels and special index) across clients using NetworkVariables,
 * and links back to the local HexCell for visual refreshes. Ownership is transferred
 * per-tile so that the owning client can author writes while others receive updates.
 *
 * Parallel hierarchy:
 *  • HexCell (local, renders & gameplay logic)
 *  • SharedTileInfo (networked, authoritative per-owner writes)
 *
 * Notes:
 *  • uniqueIndex is assigned by the server and used to link to the corresponding HexCell.
 *  • Set* mutators only write when IsOwner; others receive value changes via OnValueChanged.
 *  • OnChange triggers a lightweight chunk refresh on non-owners to keep visuals in sync.
 */

using UnityEngine;
using Unity.Netcode;

public class SharedTileInfo : NetworkBehaviour
{
    /// <summary>
    /// Back-reference to the local logical cell this network object represents.
    /// Assigned by the grid/chunk linking step.
    /// </summary>
    public HexCell hexCell;

    /// <summary>
    /// Server-assigned stable index used to link this shared object to its HexCell.
    /// Writable by Server only; readable by Everyone.
    /// </summary>
    public NetworkVariable<int> uniqueIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Vegetation level (Owner-writable, Everyone-readable).
    /// </summary>
    public NetworkVariable<int> plantLevel =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Special feature index (Owner-writable, Everyone-readable).
    /// </summary>
    public NetworkVariable<int> specialIndex =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Urbanization level (Owner-writable, Everyone-readable).
    /// </summary>
    public NetworkVariable<int> urbanLevel =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Water level (Owner-writable, Everyone-readable).
    /// </summary>
    public NetworkVariable<int> waterLevel =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// Server-only spawn of this network object.
    /// </summary>
    public void Awake()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    /// <summary>
    /// Binds name and change listeners for synchronized fields.
    /// </summary>
    private void Start()
    {
        gameObject.name = $"SharedTileInfo_{uniqueIndex.Value}";

        // Register change callbacks so non-owners refresh visuals on updates.
        plantLevel.OnValueChanged += OnChange;
        urbanLevel.OnValueChanged += OnChange;
        specialIndex.OnValueChanged += OnChange;
        waterLevel.OnValueChanged += OnChange;
    }

    /// <summary>
    /// Unregisters change listeners on destroy.
    /// </summary>
    private void OnDestroy()
    {
        plantLevel.OnValueChanged -= OnChange;
        urbanLevel.OnValueChanged -= OnChange;
        specialIndex.OnValueChanged -= OnChange;
        // NOTE: Consider unsubscribing waterLevel as well (symmetry with Start()) if needed by lifecycle.
        // waterLevel.OnValueChanged -= OnChange;
    }

    /// <summary>
    /// (Debug) Example write to plant level. Not used in production.
    /// </summary>
    public void ChangeTest(float value)
    {
        // NetworkVariable is int; cast is intentional for quick testing.
        plantLevel.Value = (ushort)value;
    }

    /// <summary>
    /// Refreshes local visuals when a synced value changes on a non-owning client.
    /// Owners already trigger refreshes when they set values.
    /// </summary>
    private void OnChange(int oldValue, int newValue)
    {
        if (!IsOwner && hexCell != null)
        {
            hexCell.RefreshSelfOnly();
        }
    }

    /// <summary>
    /// Owner-guarded setter for <see cref="plantLevel"/>.
    /// </summary>
    public void SetPlantLevel(int plantLevel)
    {
        if (IsOwner)
        {
            this.plantLevel.Value = plantLevel;
        }
    }

    /// <summary>
    /// Owner-guarded setter for <see cref="urbanLevel"/>.
    /// </summary>
    public void SetUrbanLevel(int urbanLevel)
    {
        if (IsOwner)
        {
            this.urbanLevel.Value = urbanLevel;
        }
    }

    /// <summary>
    /// Owner-guarded setter for <see cref="specialIndex"/>.
    /// </summary>
    public void SetSpecialIndex(int specialIndex)
    {
        if (IsOwner)
        {
            this.specialIndex.Value = specialIndex;
        }
    }

    /// <summary>
    /// Owner-guarded setter for <see cref="waterLevel"/>.
    /// </summary>
    public void SetWaterLevel(int waterLevel)
    {
        if (IsOwner)
        {
            this.waterLevel.Value = waterLevel;
        }
    }

    /// <summary>
    /// Server-only: transfers ownership of this shared tile to another client
    /// (used when assigning nations/territory).
    /// </summary>
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
