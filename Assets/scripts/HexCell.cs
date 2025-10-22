/*
 * Project: GeoHex
 * File: HexCell.cs
 * Author: Sören Coremans (based on original work by Jasper Flick)
 * Source: Adapted and extended from the "Hex Map" tutorial by Jasper Flick
 *         https://catlikecoding.com/unity/tutorials/hex-map/
 *
 * Description:
 * Core data container for individual hex tiles on the map grid.
 * 
 * Handles:
 *  • Elevation, terrain, vegetation, and urbanization data
 *  • Road and river logic (including validation and synchronization)
 *  • Tile colorization depending on map mode and ownership
 *  • Synchronization of shared state across clients via SharedTileInfo
 *  • Save/load serialization for editor mode and deterministic map generation
 *
 * Notes:
 *  • Many geometric utilities (rivers, elevation, terrain) are inherited from
 *    Jasper Flick’s implementation.
 *  • Multiplayer synchronization and shared data handling (SharedTileInfo)
 *    were added by Sören Coremans.
 */

using System;
using UnityEngine;
using System.IO;
using Unity.Netcode;

/// <summary>
/// Logical data container for a single hex tile. Tracks elevation, water,
/// rivers, roads, ownership, and terrain type, and coordinates updates with
/// its parent <see cref="HexGridChunk"/> and networked <see cref="SharedTileInfo"/>.
/// </summary>
public class HexCell : MonoBehaviour
{
    /// <summary>
    /// Grid coordinates of this cell (hex coordinate system).
    /// </summary>
    public HexCoordinates coordinates;

    /// <summary>
    /// Network-synchronized state for values shared across clients
    /// (water/urban/plant levels, specials, ownership checks, etc.).
    /// </summary>
    public SharedTileInfo sharedTileInfo;

    [SerializeField]
    private int country;

    /// <summary>
    /// Index of the country that owns this cell.
    /// </summary>
    public int Country
    {
        get { return country; }
        set
        {
            if (country != value)
            {
                country = value;
                RefreshSelfOnly();
            }
        }
    }

    int terrainTypeIndex;

    /// <summary>
    /// Terrain type index of the cell.
    /// 0 = Desert, 1 = Grasslands, 2 = Ocean, 3 = Hills, 4 = Forest.
    /// </summary>
    public int TerrainTypeIndex
    {
        get { return terrainTypeIndex; }
        set
        {
            if (terrainTypeIndex != value)
            {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    [SerializeField]
    bool[] roads;

    /// <summary>
    /// Special feature index (delegated to <see cref="SharedTileInfo"/>).
    /// </summary>
    public int SpecialIndex
    {
        get
        {
            if (sharedTileInfo == null)
            {
                Debug.Log("No shared info!");
                return 0;
            }
            return sharedTileInfo.specialIndex.Value;
        }
        set
        {
            if (sharedTileInfo.specialIndex.Value != value)
            {
                sharedTileInfo.SetSpecialIndex(value);
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Whether the cell has a special feature (based on <see cref="SpecialIndex"/>).
    /// </summary>
    public bool IsSpecial
    {
        get
        {
            if (sharedTileInfo == null) return false;
            return SpecialIndex > 0;
        }
    }

    /// <summary>
    /// Vegetation level (delegated to <see cref="SharedTileInfo"/>).
    /// </summary>
    public int PlantLevel
    {
        get
        {
            if (sharedTileInfo == null) return 0;
            return sharedTileInfo.plantLevel.Value;
        }
        set
        {
            if (sharedTileInfo.plantLevel.Value != value)
            {
                sharedTileInfo.SetPlantLevel(value);
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Urbanization level (delegated to <see cref="SharedTileInfo"/>).
    /// </summary>
    public int UrbanLevel
    {
        get
        {
            if (sharedTileInfo == null) return 0;
            return sharedTileInfo.urbanLevel.Value;
        }
        set
        {
            if (sharedTileInfo.urbanLevel.Value != value)
            {
                sharedTileInfo.SetUrbanLevel(value);
                RefreshSelfOnly();
            }
        }
    }

    /// <summary>
    /// Water level (delegated to <see cref="SharedTileInfo"/>).
    /// Setting triggers river validation and visual refresh.
    /// </summary>
    public int WaterLevel
    {
        get
        {
            if (sharedTileInfo == null) return 0;
            return sharedTileInfo.waterLevel.Value;
        }
        set
        {
            if (sharedTileInfo == null || sharedTileInfo.waterLevel.Value == value)
                return;

            sharedTileInfo.SetWaterLevel(value);
            ValidateRivers();
            Refresh();
        }
    }

    /// <summary>
    /// Index used by building effectiveness calculations.
    /// Mirrors <see cref="TerrainTypeIndex"/>, but returns 5 if the cell is underwater.
    /// </summary>
    public int EffectivenessIndex
    {
        get
        {
            int effectivenessIndex = TerrainTypeIndex;
            if (IsUnderwater) effectivenessIndex = 5;
            return effectivenessIndex;
        }
    }

    /// <summary>
    /// True if the water level is higher than the terrain elevation.
    /// </summary>
    public bool IsUnderwater
    {
        get { return WaterLevel > elevation; }
    }

    bool hasIncomingRiver, hasOutgoingRiver;

    /// <summary>
    /// Returns whether a road exists through the given edge.
    /// </summary>
    public bool HasRoadThroughEdge(HexDirection direction) => roads[(int)direction];

    /// <summary>
    /// Adds a road along the given edge if allowed (no river through that edge and elevation diff ≤ 1).
    /// </summary>
    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int)direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
        {
            SetRoad((int)direction, true);
        }
    }

    /// <summary>
    /// Removes all roads connected to this cell.
    /// </summary>
    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++)
        {
            if (roads[i]) SetRoad(i, false);
        }
    }

    /// <summary>
    /// Sets the presence of a road on a specific edge and mirrors it on the neighbor.
    /// Triggers visual refresh for both cells.
    /// </summary>
    void SetRoad(int index, bool state)
    {
        roads[index] = state;
        neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
        neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    /// <summary>
    /// Absolute elevation difference between this cell and its neighbor on a given edge.
    /// </summary>
    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;
        return difference >= 0 ? difference : -difference;
    }

    /// <summary>
    /// True if the cell has any roads.
    /// </summary>
    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < roads.Length; i++)
                if (roads[i]) return true;
            return false;
        }
    }

    HexDirection incomingRiver, outgoingRiver;

    /// <summary>
    /// Y-coordinate of the river bed surface for this cell.
    /// </summary>
    public float StreamBedY
    {
        get { return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
    }

    /// <summary>
    /// Y-coordinate of the river surface for this cell.
    /// </summary>
    public float RiverSurfaceY
    {
        get { return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    /// <summary>
    /// Y-coordinate of the open water surface for this cell.
    /// </summary>
    public float WaterSurfaceY
    {
        get { return (WaterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    /// <summary>
    /// Returns whether <paramref name="neighbor"/> is a valid downhill (or level-to-water) river destination.
    /// </summary>
    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (elevation >= neighbor.elevation || WaterLevel == neighbor.elevation);
    }

    /// <summary>
    /// True if a river flows into this cell from a neighbor.
    /// </summary>
    public bool HasIncomingRiver { get { return hasIncomingRiver; } }

    /// <summary>
    /// True if a river flows out from this cell to a neighbor.
    /// </summary>
    public bool HasOutgoingRiver { get { return hasOutgoingRiver; } }

    /// <summary>
    /// Direction from which the river begins or ends on this cell (only valid when exactly one of in/out exists).
    /// </summary>
    public HexDirection IncomingRiver { get { return incomingRiver; } }

    /// <summary>
    /// Direction to which the river leaves this cell (only valid when outgoing exists).
    /// </summary>
    public HexDirection OutgoingRiver { get { return outgoingRiver; } }

    /// <summary>
    /// True if the cell has any river (incoming or outgoing).
    /// </summary>
    public bool HasRiver { get { return hasIncomingRiver || hasOutgoingRiver; } }

    /// <summary>
    /// True if the cell is a river start/end (i.e., only incoming or only outgoing).
    /// </summary>
    public bool HasRiverBeginOrEnd { get { return hasIncomingRiver != hasOutgoingRiver; } }

    /// <summary>
    /// True if a river crosses the edge in <paramref name="direction"/>.
    /// </summary>
    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return (hasIncomingRiver && incomingRiver == direction) ||
               (hasOutgoingRiver && outgoingRiver == direction);
    }

    /// <summary>
    /// Display color of the cell. Uses terrain or country palette depending on map mode.
    /// Dimmed for cells not owned by the local player.
    /// </summary>
    public Color Color
    {
        get
        {
            float fog = 0.5f;
            if (sharedTileInfo && sharedTileInfo.IsOwner) fog = 1f;

            if (HexMetrics.mapMode == 1)
            {
                return HexMetrics.countryColors[country] * fog;
            }
            return HexMetrics.terrainColors[terrainTypeIndex] * fog;
        }
    }

    /// <summary>
    /// UI RectTransform for the coordinate label (development/debug view).
    /// </summary>
    public RectTransform uiRect;

    /// <summary>
    /// The chunk this cell belongs to (used for batched mesh refresh).
    /// </summary>
    public HexGridChunk chunk;

    /// <summary>
    /// Elevation level of the cell. Setting updates position, validates rivers,
    /// removes invalid roads, and refreshes visuals.
    /// </summary>
    public int Elevation
    {
        get { return elevation; }
        set
        {
            if (elevation == value) return;

            RefreshPosition(value);
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++)
            {
                if (roads[i] && GetElevationDifference((HexDirection)i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    /// <summary>
    /// Local-space position (read-only convenience).
    /// </summary>
    public Vector3 Position => transform.localPosition;

    int elevation = int.MinValue;

    [SerializeField]
    HexCell[] neighbors;

    /// <summary>
    /// Gets the neighbor cell in the given direction.
    /// </summary>
    public HexCell GetNeighbor(HexDirection direction) => neighbors[(int)direction];

    /// <summary>
    /// Sets the neighbor for this cell and back-links this cell on the neighbor.
    /// </summary>
    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int)direction] = cell;
        cell.neighbors[(int)direction.Opposite()] = this;
    }

    /// <summary>
    /// Edge type between this cell and its neighbor in the given direction
    /// (Flat, Slope, or Cliff) derived from elevation difference.
    /// </summary>
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int)direction].elevation);
    }

    /// <summary>
    /// Edge type between this cell and <paramref name="otherCell"/>.
    /// </summary>
    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    /// <summary>
    /// Direction of the single river edge if this cell is a river source/sink.
    /// </summary>
    public HexDirection RiverBeginOrEndDirection
    {
        get { return hasIncomingRiver ? incomingRiver : outgoingRiver; }
    }

    /// <summary>
    /// Returns the tile improvement (building) given the current special/urban/plant values.
    /// </summary>
    public TileImprovement getImprovement()
    {
        return HexMapEditor.getImprovement(SpecialIndex, UrbanLevel, PlantLevel);
    }

    /// <summary>
    /// Refreshes this cell's chunk and any neighboring chunks that border it.
    /// </summary>
    void Refresh()
    {
        if (chunk)
        {
            chunk.Refresh();
            for (int i = 0; i < neighbors.Length; i++)
            {
                HexCell neighbor = neighbors[i];
                if (neighbor != null && neighbor.chunk != chunk)
                {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    /// <summary>
    /// Removes the outgoing river, updating the neighbor’s incoming river accordingly.
    /// </summary>
    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) return;

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// Removes the incoming river, updating the neighbor’s outgoing river accordingly.
    /// </summary>
    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver) return;

        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    /// <summary>
    /// Removes both incoming and outgoing rivers from this cell.
    /// </summary>
    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    /// <summary>
    /// Sets a new outgoing river in <paramref name="direction"/> if valid.
    /// Removes conflicting existing rivers and disables any road on that edge.
    /// </summary>
    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction) return;

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) return;

        RemoveOutgoingRiver();
        if (hasIncomingRiver && incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }

    /// <summary>
    /// Refreshes only this cell’s chunk (no neighbor splash).
    /// </summary>
    public void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    /// <summary>
    /// Validates existing river assignments and removes any that are no longer valid.
    /// </summary>
    void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver)))
        {
            RemoveOutgoingRiver();
        }
        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    /// <summary>
    /// Recomputes world-space position from current elevation and noise perturbation
    /// and updates the UI label depth to match.
    /// </summary>
    void RefreshPosition()
    {
        Vector3 position = transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    /// <summary>
    /// Assigns a new elevation value and then refreshes world-space position.
    /// </summary>
    void RefreshPosition(int value)
    {
        elevation = value;
        Vector3 position = transform.localPosition;
        position.y = value * HexMetrics.elevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        transform.localPosition = position;

        Vector3 uiPosition = uiRect.localPosition;
        uiPosition.z = -position.y;
        uiRect.localPosition = uiPosition;
    }

    /// <summary>
    /// Serializes the cell’s key state to a binary stream.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        writer.Write((byte)terrainTypeIndex);
        writer.Write((byte)elevation);
        writer.Write((byte)sharedTileInfo.waterLevel.Value);
        writer.Write((byte)sharedTileInfo.urbanLevel.Value);
        writer.Write((byte)sharedTileInfo.plantLevel.Value);
        writer.Write((byte)SpecialIndex);
        writer.Write((byte)country);

        if (hasIncomingRiver) writer.Write((byte)(incomingRiver + 128));
        else writer.Write((byte)0);

        if (hasOutgoingRiver) writer.Write((byte)(outgoingRiver + 128));
        else writer.Write((byte)0);

        int roadFlags = 0;
        for (int i = 0; i < roads.Length; i++)
            if (roads[i]) roadFlags |= 1 << i;

        writer.Write((byte)roadFlags);
    }

    /// <summary>
    /// Deserializes the cell’s key state from a binary stream and refreshes visuals.
    /// On the server, writes values back into <see cref="SharedTileInfo"/> so they sync.
    /// On clients, consumes the bytes (state arrives via network variables).
    /// </summary>
    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        elevation = reader.ReadByte();
        RefreshPosition();

        if (NetworkManager.Singleton.IsServer)
        {
            sharedTileInfo.SetWaterLevel(reader.ReadByte());
            sharedTileInfo.SetUrbanLevel(reader.ReadByte());
            sharedTileInfo.SetPlantLevel(reader.ReadByte());
            sharedTileInfo.SetSpecialIndex(reader.ReadByte());
        }
        else
        {
            // Skip values read on server; clients will receive them via netcode
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
            reader.ReadByte();
        }

        country = reader.ReadByte();

        byte riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasIncomingRiver = true;
            incomingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            hasOutgoingRiver = true;
            outgoingRiver = (HexDirection)(riverData - 128);
        }
        else
        {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < roads.Length; i++)
        {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }
}