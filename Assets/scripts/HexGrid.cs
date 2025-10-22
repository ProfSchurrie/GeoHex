/*
 * Project: GeoHex
 * File: HexGrid.cs
 * Author: Sören Coremans
 * Description:
 * Core map management system for GeoHex. Responsible for generating, organizing,
 * and linking all hexagonal cells and chunks.
 * The HexGrid orchestrates procedural world generation, color mapping, and
 * data exchange with SharedHexGridChunks for multiplayer synchronization.
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

/// <summary>
/// Central grid controller responsible for constructing and managing the hexagonal map.
/// The HexGrid maintains all logical <see cref="HexCell"/> objects, groups them into
/// <see cref="HexGridChunk"/>s for rendering, and coordinates with shared networked data.
/// </summary>
public class HexGrid : MonoBehaviour
{
    /// <summary>
    /// Singleton instance for global access to the active HexGrid.
    /// </summary>
    public static HexGrid Instance { get; private set; }

    /// <summary>
    /// Total number of cells along the X and Z axes.
    /// Determined by <see cref="chunkCountX"/> × <see cref="HexMetrics.chunkSizeX"/>
    /// and <see cref="chunkCountZ"/> × <see cref="HexMetrics.chunkSizeZ"/>.
    /// </summary>
    int cellCountX, cellCountZ;

    /// <summary>
    /// Number of chunks horizontally (X) and vertically (Z) that compose the grid.
    /// </summary>
    public int chunkCountX = 4, chunkCountZ = 3;

    /// <summary>
    /// Prefab for individual logical cells (non-visual data layer).
    /// </summary>
    public HexCell cellPrefab;

    /// <summary>
    /// Prefab for coordinate labels.
    /// </summary>
    public Text cellLabelPrefab;

    /// <summary>
    /// Noise texture used by <see cref="HexMetrics"/> to introduce local variation
    /// in terrain generation and break visual uniformity.
    /// </summary>
    public Texture2D noiseSource;

    /// <summary>
    /// Array storing all logical <see cref="HexCell"/> instances that make up the map.
    /// </summary>
    HexCell[] cells;

    /// <summary>
    /// Prefab for <see cref="HexGridChunk"/> objects that group and render cells.
    /// </summary>
    public HexGridChunk chunkPrefab;

    /// <summary>
    /// Array storing all instantiated chunks composing the visible grid.
    /// </summary>
    HexGridChunk[] chunks;

    /// <summary>
    /// Seed used to initialize procedural components of the grid.
    /// Ensures deterministic generation for identical input data.
    /// </summary>
    public int seed;

    /// <summary>
    /// Color palette mapping terrain types to their representative colors.
    /// </summary>
    Color[] terrainColors = {
        Color.yellow,
        Color.green,
        Color.blue,
        Color.white,
        new Color(0, 0.5f, 0)
    };

    /// <summary>
    /// Color palette mapping countries to distinct display colors.
    /// Used primarily for the country map mode.
    /// </summary>
    Color[] countryColors = {
        Color.white,
        new Color(0.420f, 0.506f, 0.224f),
        new Color(0.941f, 0.714f, 0.180f),
        new Color(0.276f, 0.525f, 0.235f),
        new Color(0.204f, 0.478f, 0.612f),
        new Color(0.404f, 0.675f, 0.694f)
    };

    private void Awake()
    {
        // Initialize singleton
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of HexGrid detected. Destroying the duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        // Clear singleton reference when destroyed
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Initializes map generation and starts the game setup.
    /// Configures <see cref="HexMetrics"/> with noise, seed, and color palettes,
    /// then creates and populates the grid chunks and cells.
    /// </summary>
    public void StartGame()
    {
        HexMetrics.noiseSource = noiseSource;
        HexMetrics.InitializeHashGrid(seed);
        HexMetrics.terrainColors = terrainColors;
        HexMetrics.countryColors = countryColors;

        cellCountX = chunkCountX * HexMetrics.chunkSizeX;
        cellCountZ = chunkCountZ * HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    /// <summary>
    /// Instantiates all <see cref="HexGridChunk"/>s and parents them to this grid.
    /// </summary>
    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++)
        {
            for (int x = 0; x < chunkCountX; x++)
            {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    /// <summary>
    /// Instantiates all logical <see cref="HexCell"/>s and assigns them to chunks.
    /// </summary>
    void CreateCells()
    {
        cells = new HexCell[cellCountZ * cellCountX];

        for (int z = 0, i = 0; z < cellCountZ; z++)
        {
            for (int x = 0; x < cellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void OnEnable()
    {
        // Reinitialize HexMetrics if re-enabled
        if (!HexMetrics.noiseSource)
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexMetrics.terrainColors = terrainColors;
        }
    }

    /// <summary>
    /// Retrieves a <see cref="HexCell"/> at a given grid position.
    /// </summary>
    /// <param name="position">grid position to sample.</param>
    /// <returns>The corresponding <see cref="HexCell"/>.</returns>
    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * cellCountX + coordinates.Z / 2;
        return cells[index];
    }

    /// <summary>
    /// Creates and configures a new <see cref="HexCell"/> at given coordinates.
    /// Handles positioning, neighbor assignment, and chunk integration.
    /// </summary>
    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        // Assign neighboring cells
        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }
        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, cells[i - cellCountX]);
                if (x < cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, cells[i - cellCountX + 1]);
                }
            }
        }

        // Create coordinate label
        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.coordinates.ToStringOnSeparateLines();
        cell.uiRect = label.rectTransform;

        // Default starting elevation and water level
        cell.Elevation = 2;
        cell.WaterLevel = 2;

        AddCellToChunk(x, z, cell);
    }

    /// <summary>
    /// Assigns a given cell to its corresponding <see cref="HexGridChunk"/>
    /// based on its world coordinates.
    /// </summary>
    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;
        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    /// <summary>
    /// Saves the current map state by writing all cell data to a binary stream.
    /// Used by the in-editor map building tool.
    /// </summary>
    public void Save(BinaryWriter writer)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Save(writer);
        }
    }

    /// <summary>
    /// Loads map data from a binary stream and refreshes all chunks to reflect changes.
    /// </summary>
    public void Load(BinaryReader reader)
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i].Load(reader);
        }
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }
    }

    /// <summary>
    /// Links this grid's chunks with their networked <see cref="SharedHexGridChunk"/> counterparts
    /// for multiplayer synchronization.
    /// </summary>
    /// <param name="sharedChunk">The shared chunk instance from the network layer.</param>
    /// <param name="index">Chunk index within the grid.</param>
    public void LinkChunks(SharedHexGridChunk sharedChunk, int index)
    {
        chunks[index].sharedChunk = sharedChunk;
        sharedChunk.chunk = chunks[index];
    }

    /// <summary>
    /// Toggles between terrain and country display modes using numeric hotkeys.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && HexMetrics.mapMode != 0)
        {
            HexMetrics.mapMode = 0;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) && HexMetrics.mapMode != 1)
        {
            HexMetrics.mapMode = 1;
        }
        else
        {
            return;
        }

        // Refresh all chunks to apply visual mode changes
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i].Refresh();
        }
    }
}
