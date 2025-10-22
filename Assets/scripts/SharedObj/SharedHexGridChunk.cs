/*
 * Project: GeoHex
 * File: SharedHexGridChunk.cs
 * Author: Sören Coremans
 * Description:
 * Networked companion to a local HexGridChunk. Manages the lifecycle and
 * ownership of all SharedTileInfo instances belonging to a chunk and links
 * them to their local HexCell counterparts. The server spawns this chunk,
 * creates the per-tile network objects, and assigns/transfer ownership as
 * nations are selected and territory is distributed.
 *
 * Parallel hierarchy:
 *  • HexGridChunk (local, rendering & triangulation)
 *  • SharedHexGridChunk (networked, per-tile state authority & ownership)
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Networked manager for all <see cref="SharedTileInfo"/> objects in a grid chunk.
/// Server-owned; responsible for spawning tiles, linking them to the local
/// <see cref="HexGridChunk"/>, and transferring ownership to clients based on nation.
/// </summary>
public class SharedHexGridChunk : NetworkBehaviour
{
    /// <summary>
    /// Server-assigned stable index for this shared chunk within the grid.
    /// Readable by everyone; writable by server only.
    /// </summary>
    public NetworkVariable<int> uniqueIndex =
        new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Local (non-networked) chunk that renders the terrain and holds <see cref="HexCell"/>s.
    /// Linked during grid setup so shared tiles can be paired with cells.
    /// </summary>
    public HexGridChunk chunk;

    /// <summary>
    /// All networked tile objects (one per cell in this chunk).
    /// </summary>
    private SharedTileInfo[] tiles;

    /// <summary>
    /// Prefab used to instantiate per-tile networked state.
    /// </summary>
    public SharedTileInfo SharedTileInfoPrefab;

    /// <summary>
    /// Server-only spawn of this network object.
    /// </summary>
    public void Spawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    private void Start()
    {
        gameObject.name = $"SharedHexGridChunk_{uniqueIndex.Value}";
    }

    /// <summary>
    /// Server: creates all <see cref="SharedTileInfo"/> children for this chunk and
    /// assigns their <see cref="SharedTileInfo.uniqueIndex"/>. Linking to local
    /// cells is performed later via <see cref="GetAllChildTiles"/>.
    /// </summary>
    public void CreateTiles()
    {
        tiles = new SharedTileInfo[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        for (int i = 0; i < tiles.Length; i++)
        {
            tiles[i] = Instantiate(SharedTileInfoPrefab);
            tiles[i].transform.SetParent(transform);
            tiles[i].uniqueIndex.Value = i;
        }
    }

    /// <summary>
    /// Discovers all <see cref="SharedTileInfo"/> children beneath this object,
    /// links each to its corresponding <see cref="HexCell"/> inside <see cref="chunk"/>,
    /// and caches them locally.
    /// </summary>
    /// <remarks>
    /// Requires that <see cref="chunk"/> has already been created and populated with cells.
    /// </remarks>
    public SharedTileInfo[] GetAllChildTiles()
    {
        // Get all child transforms of this GameObject
        Transform[] childTransforms = GetComponentsInChildren<Transform>();

        // Create a list to store found SharedTileInfo components
        List<SharedTileInfo> tileList = new List<SharedTileInfo>();

        int i = 0;

        // Iterate over all child transforms
        foreach (Transform child in childTransforms)
        {
            // Check if the child has a SharedTileInfo component
            SharedTileInfo tile = child.GetComponent<SharedTileInfo>();
            if (tile != null)
            {
                tileList.Add(tile);
                chunk.LinkCells(tile, tile.uniqueIndex.Value);
                i++;
            }
        }

        tiles = tileList.ToArray();

        // Convert the list to an array and return it
        return tileList.ToArray();
    }

    /// <summary>
    /// Server: transfers network ownership of all tiles in this chunk that belong to
    /// <paramref name="nationID"/> so the target client can author their values.
    /// </summary>
    public void TransferOwnership(ulong clientId, int nationID)
    {
        foreach (SharedTileInfo cell in tiles)
        {
            if (cell.hexCell.Country == nationID)
            {
                cell.ChangeOwnership(clientId);
            }
        }
    }

    /// <summary>
    /// Populates a <see cref="Nation"/> with all cells in this chunk that match
    /// <paramref name="nationID"/> and refreshes the local chunk visuals.
    /// </summary>
    public void AssignCellToNation(Nation nation, int nationID)
    {
        foreach (SharedTileInfo cell in tiles)
        {
            if (cell.hexCell.Country == nationID)
            {
                nation.AddCell(cell.hexCell);
            }
        }
        chunk.Refresh();
    }
}
