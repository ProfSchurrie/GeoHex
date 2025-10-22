/*
 * Project: GeoHex
 * File: HexMapEditor.cs
 * Author: Sören Coremans (adapted from Catlike Coding’s Hex Map tutorial)
 *
 * Description:
 * In-editor map building tool. Lets the designer paint terrain, elevation, water,
 * vegetation, urbanization, specials, rivers, roads, and nations onto the grid.
 * Input is driven by simple mouse/keyboard (left-click to apply) and a set of
 * UI toggles/sliders wired via the lightweight handler scripts in /UI.
 *
 * Scope:
 *  • Used for world authoring; not exposed to players in normal gameplay.
 *  • Can be enabled for runtime editing in the future (separation of concerns recommended).
 *
 * Highlights:
 *  • Left-click paints currently “active” values if their corresponding “apply” flags are set.
 *  • Dragging across cells can lay rivers/roads depending on mode.
 *  • “I” toggles an on-hover info readout (tile improvement + terrain type).
 *  • Right mouse button clears build-layer flags (special/urban/plant).
 *  • ValidateBuild() enforces nation ownership, research gates, terrain effectiveness,
 *    and available funds (charges GoldBuildCost when placing).
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using DefaultNamespace;
using UnityEngine.UI;

public class HexMapEditor : MonoBehaviour
{
    /// <summary>Singleton reference for editor handlers to access.</summary>
    public static HexMapEditor Instance { get; private set; }

    /// <summary>Local player (used for nation requests).</summary>
    public Player player;

    /// <summary>Target grid being edited.</summary>
    public HexGrid hexGrid;

    /// <summary>Small HUD text used for tile info readout (toggle with 'I').</summary>
    public Text infoText;

    // Active brush values (set by UI)
    int activeElevation = -1;
    int activeWaterLevel = -1;
    int activeUrbanLevel = -1;
    int activePlantLevel = -1;
    int activeSpecialIndex = -1;
    int activeTerrainTypeIndex = -1;
    int activeCountry;

    // Which layers to apply when painting (set by UI)
    bool applyElevation = false;
    bool applyWaterLevel = false;
    bool applyUrbanLevel = false;
    bool applyPlantLevel = false;
    bool applySpecialIndex = false;
    bool applyCountry = false;

    bool showInfo = false;

    // Drag helpers (for roads/rivers)
    bool isDrag;
    HexDirection dragDirection;
    HexCell previousCell;

    enum OptionalToggle { Ignore, Yes, No }

    OptionalToggle riverMode, roadMode;

    /// <summary>UI slider hook for elevation.</summary>
    public void SetElevation(float elevation) => activeElevation = (int)elevation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        // Toggle info overlay
        if (Input.GetKeyDown(KeyCode.I))
        {
            infoText.text = "";
            showInfo = !showInfo;
        }

        if (showInfo && !EventSystem.current.IsPointerOverGameObject())
        {
            ShowInfo();
        }

        // Primary paint (LMB) when not over UI
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            previousCell = null;
        }

        // RMB clears placement “content” flags (quick cancel)
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            applySpecialIndex = false;
            applyUrbanLevel = false;
            applyPlantLevel = false;
        }
    }

    /// <summary>Shows a minimal cell info HUD (improvement + terrain type) under the cursor.</summary>
    void ShowInfo()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(inputRay, out var hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            infoText.text = currentCell.getImprovement() + " " + currentCell.TerrainTypeIndex;
        }
        else
        {
            previousCell = null;
            infoText.text = "";
        }
    }

    /// <summary>Handles “paint” over the hovered cell; supports drag for river/road.</summary>
    void HandleInput()
    {
        Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(inputRay, out var hit))
        {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                isDrag = false;
            }
            EditCell(currentCell);
            previousCell = currentCell;
        }
        else
        {
            previousCell = null;
        }
    }

    /// <summary>
    /// Validates placement: nation ownership, research prereqs, terrain effectiveness,
    /// underwater/offshore rules, and available gold. Charges build cost on success.
    /// </summary>
    bool ValidateBuild(HexCell cell)
    {
        if (Nation.CurrentNation != null && Nation.CurrentNation.id.Value != 0)
        {
            // Compute target improvement from staged values (or current)
            TileImprovement improvement = getImprovement(
                applySpecialIndex ? activeSpecialIndex : 0,
                applyUrbanLevel ? activeUrbanLevel : 0,
                applyPlantLevel ? activePlantLevel : 0);

            (Stats stats, _) = Nation.GetCellStats(improvement);

            // Reject if unchanged, ineffective on this terrain, or insufficient funds
            if (cell.getImprovement() == improvement ||
                stats.Effectiveness[cell.EffectivenessIndex] == 0f ||
                Nation.CurrentNation.GetStats().Gold < -stats.GoldBuildCost)
                return false;

            // Placement gates
            if ((improvement == TileImprovement.ImprovedWindPark || improvement == TileImprovement.WindPark) &&
                cell.IsUnderwater &&
                (cell.WaterLevel - cell.Elevation != 1 || (Research.Instance == null || !Research.Instance.ROffshore)))
                return false;

            if (improvement == TileImprovement.HydroPowerPlant &&
                (!cell.HasRiver || !Research.Instance.RWater))
                return false;

            if (improvement == TileImprovement.NuclearPowerPlant &&
                (Research.Instance == null || !Research.Instance.RNuclear))
                return false;

            if (improvement == TileImprovement.GasPowerPlant &&
                (Research.Instance == null || !Research.Instance.RGas))
                return false;

            if (improvement == TileImprovement.GreenCity &&
                (Research.Instance == null || !Research.Instance.RGreenCity))
                return false;

            if (improvement == TileImprovement.SolarPark &&
                (Research.Instance == null || !Research.Instance.RSolar))
                return false;

            // Charge gold up front
            Nation.CurrentNation.changeGold(stats.GoldBuildCost);
        }
        return true;
    }

    /// <summary>
    /// Applies staged edits to a cell based on active flags and values,
    /// and lays roads/rivers when dragging between neighbors.
    /// </summary>
    void EditCell(HexCell cell)
    {
        if (!ValidateBuild(cell)) return;
        if (!cell) return;

        if (activeTerrainTypeIndex >= 0) cell.TerrainTypeIndex = activeTerrainTypeIndex;
        if (applyElevation) cell.Elevation = activeElevation;
        if (applyWaterLevel) cell.WaterLevel = activeWaterLevel;
        if (applySpecialIndex) cell.SpecialIndex = activeSpecialIndex;
        if (applyUrbanLevel) cell.UrbanLevel = activeUrbanLevel;
        if (applyPlantLevel) cell.PlantLevel = activePlantLevel;
        if (applyCountry) cell.Country = activeCountry;

        if (riverMode == OptionalToggle.No) cell.RemoveRiver();
        if (roadMode == OptionalToggle.No) cell.RemoveRoads();

        if (isDrag)
        {
            HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
            if (otherCell)
            {
                if (riverMode == OptionalToggle.Yes) otherCell.SetOutgoingRiver(dragDirection);
                if (roadMode == OptionalToggle.Yes) otherCell.AddRoad(dragDirection);
            }
        }
    }

    /// <summary>Determines whether we’re dragging from the previous cell into the current cell.</summary>
    void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++)
        {
            if (previousCell.GetNeighbor(dragDirection) == currentCell)
            {
                isDrag = true;
                return;
            }
        }
        isDrag = false;
    }

    // ===== UI handler hooks =====

    public void SetApplyElevation(bool toggle) => applyElevation = toggle;

    public void SetRiverMode(int mode) => riverMode = (OptionalToggle)mode;

    public void SetRoadMode(int mode) => roadMode = (OptionalToggle)mode;

    public void SetApplyWaterLevel(bool toggle) => applyWaterLevel = toggle;

    public void SetWaterLevel(float level) => activeWaterLevel = (int)level;

    public void SetApplyUrbanLevel(bool toggle) => applyUrbanLevel = toggle;

    public void SetUrbanLevel(float level) => activeUrbanLevel = (int)level;

    public void SetPlantLevel(float level) => activePlantLevel = (int)level;

    public void SetApplyPlantLevel(bool toggle) => applyPlantLevel = toggle;

    /// <summary>Sets special index. Pass -1 to disable applying specials.</summary>
    public void SetSpecialIndex(int index)
    {
        if (index == -1) { applySpecialIndex = false; return; }
        applySpecialIndex = true;
        activeSpecialIndex = index;
    }

    public void SetTerrainTypeIndex(int index) => activeTerrainTypeIndex = index;

    /// <summary>Sets country id. Pass -1 to stop applying country paint.</summary>
    public void SetCountry(int index)
    {
        if (index == -1) { applyCountry = false; return; }
        applyCountry = true;
        activeCountry = index;
    }

    /// <summary>Requests a nation assignment for the current player. To be moved elsewhere in future development</summary>
    public void RequestNation(int nation)
    {
        player = Player.CurrentPlayer;
        if (player == null) Debug.Log("Player not found in HexMapEditor.RequestNation");
        player.RequestNation((ushort)nation);
    }

    // ===== Map IO (authoring) =====

    /// <summary>Saves the current map to persistent data path as test.map (format version 0).</summary>
    public void Save()
    {
        Debug.Log(Application.persistentDataPath);
        string path = Path.Combine(Application.persistentDataPath, "test.map");
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create)))
        {
            writer.Write(0); // header/version
            hexGrid.Save(writer);
        }
    }

    /// <summary>Loads test.map from ./Map/ and refreshes the grid.</summary>
    public void Load()
    {
        string path = Path.Combine("./Map", "test.map");
        Debug.Log(path);
        using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
        {
            int header = reader.ReadInt32();
            if (header == 0) hexGrid.Load(reader);
            else Debug.LogWarning("Unknown map format " + header);
        }
    }

    /// <summary>Wire the target grid (called by StartNetwork on game init).</summary>
    public void AssignHexGrid(HexGrid hexGrid) => this.hexGrid = hexGrid;

    /// <summary>Starts grid generation (proxy to HexGrid.StartGame()).</summary>
    public void StartGame() => hexGrid.StartGame();

    /// <summary>
    /// Computes the tile improvement that would be placed for given staged values.
    /// If no special is applied, derives improvement from Urban/Plant tiers.
    /// </summary>
    public static TileImprovement getImprovement(int SpecialIndex, int UrbanLevel, int PlantLevel)
    {
        switch (SpecialIndex)
        {
            case 1: return TileImprovement.SolarPark;
            case 2: return UrbanLevel == 1 ? TileImprovement.ForrestAndVillage : TileImprovement.Forrest;
            case 3: return TileImprovement.WindPark;
            case 4: return TileImprovement.Mountain;
            case 5: return TileImprovement.Farm;
            case 6: return TileImprovement.City;
            case 7: return TileImprovement.Village;
            case 8: return TileImprovement.CoalPowerPlant;
            case 9: return TileImprovement.GasPowerPlant;
            case 10: return TileImprovement.NuclearPowerPlant;
            case 11: return TileImprovement.HydroPowerPlant;
            default:
                switch (UrbanLevel)
                {
                    case 1: return TileImprovement.Village;
                    case 2: return TileImprovement.Village;
                    case 3: return PlantLevel == 3 ? TileImprovement.GreenCity : TileImprovement.City;
                    default: return TileImprovement.None;
                }
        }
    }
}
