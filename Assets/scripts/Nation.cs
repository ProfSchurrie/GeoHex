/*
 * Project: GeoHex
 * File: Nation.cs
 * Author: Sören Coremans
 * Description:
 * Networked simulation entity representing a player's nation. Tracks territory (cells),
 * per-round economic/ecological outputs, and ongoing trade effects. The owning client
 * computes round updates locally while key values (e.g., cumulative CO2) are synchronized
 * via NetworkVariables for all clients.
 *
 * Responsibilities:
 *  • Maintain owned cells and apply per-cell improvement outputs.
 *  • Aggregate resource flow (Gold), effectiveness (Food/Energy), and CO₂ output.
 *  • Apply time-based ticks (“rounds”) and ongoing TradeStats.
 *  • Coordinate with HexServer for cell assignment and game time visibility.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Networked nation owned by a player. Aggregates tile improvements, trade effects,
/// and produces per-round economic & ecological stats. Authoritatively exposes
/// long-lived values (e.g., cumulative CO₂) via <see cref="NetworkVariable{T}"/>.
/// </summary>
public class Nation : NetworkBehaviour
{
    /// <summary>
    /// Convenience pointer to the local player's active nation (set on the owning client).
    /// </summary>
    public static Nation CurrentNation { get; private set; }

    /// <summary>
    /// Locally stored current stats snapshot (gold/effectiveness/CO₂ delta).
    /// Note: the CO₂ field here is not networked; cumulative CO₂ is tracked in <see cref="CO2"/>.
    /// </summary>
    private NationStats nationStats;

    /// <summary>
    /// Ongoing per-round trade effects applied each tick until their rounds expire.
    /// </summary>
    private List<TradeStats> tradeDeals = new List<TradeStats>();

    /// <summary>
    /// Owning player (assigned when the Nation is attached to a Player).
    /// </summary>
    private Player player;

    /// <summary>
    /// Cumulative CO₂ produced by this nation over its lifetime (network-synchronized).
    /// </summary>
    public NetworkVariable<float> CO2 =
        new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>
    /// All cells currently assigned to this nation (ownership/territory).
    /// </summary>
    public List<HexCell> cells;

    /// <summary>
    /// Time accumulator since the last round tick.
    /// </summary>
    private float timeElapsed = 0f;

    /// <summary>
    /// Duration of a round in seconds. (Game balance lever; 5s here.)
    /// </summary>
    private float timeInterval = 5f;

    /// <summary>
    /// Whether the nation’s tick processing is paused.
    /// </summary>
    private bool paused = false;

    /// <summary>
    /// Local round counter for this nation. HexServer reads from <see cref="Nation.CurrentNation.time"/>.
    /// </summary>
    public int time = 0;

    /// <summary>
    /// Nation identifier (server-assigned), used to map territories & ownership.
    /// </summary>
    public NetworkVariable<ushort> id =
        new NetworkVariable<ushort>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>
    /// Internal guard so cells are only assigned once when the id becomes valid.
    /// </summary>
    private bool cellsAssigned = false;

    private void Awake()
    {
        cellsAssigned = false;
        cells = new List<HexCell>();

        // Initialize starting stats (e.g., starting gold).
        nationStats = new NationStats(5000f, 1f, 1f, 0f);

        // Register with the authoritative server and spawn the network object server-side.
        HexServer.Instance.AddNation(this);
        Spawn();
    }

    private void Start()
    {
        // Link back to the owning player component.
        transform.parent.GetComponent<Player>().nation = this;
    }

    private void Update()
    {
        // Only the owning client advances simulation for this nation.
        if (!IsOwner)
        {
            return;
        }

        // Initialize the convenience pointer on the local client.
        if (CurrentNation == null)
        {
            CurrentNation = this;
        }

        // Lazily bind owning player and (optionally) cell assignment trigger.
        if (player == null)
        {
            AssignPlayerAndCells();
        }

        if (!paused)
        {
            timeElapsed += Time.deltaTime;
        }

        // Assign cells once the server has set a valid id.
        if (id.Value != 0 && !cellsAssigned)
        {
            cellsAssigned = true;
            AssignCells();
        }

        // Process a round tick.
        if (timeElapsed >= timeInterval)
        {
            NationStats calcedStats = calc();

            // Persist calculated deltas into our live snapshot and networked totals.
            nationStats.FoodEffectiveness = calcedStats.FoodEffectiveness;
            CO2.Value += calcedStats.CO2;                       // cumulative CO₂ is networked
            nationStats.Gold += calcedStats.Gold;
            nationStats.EnergyEffectiveness = calcedStats.EnergyEffectiveness;

            // Advance time and carry over any extra elapsed time.
            time++;
            timeElapsed -= timeInterval;
        }
    }

    /// <summary>
    /// Server-only: spawns this Nation’s network object.
    /// </summary>
    public void Spawn()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    /// <summary>
    /// Adds a cell to this nation’s territory list.
    /// </summary>
    public void AddCell(HexCell cell)
    {
        cells.Add(cell);
    }

    /// <summary>
    /// Compact container of round-based nation outputs.
    /// </summary>
    public struct NationStats
    {
        public float Gold;
        public float FoodEffectiveness;
        public float EnergyEffectiveness;
        public float CO2; // Per-round CO₂ delta (not networked); applied into CO2 NetworkVariable each tick.

        public NationStats(float gold, float foodEffectiveness, float energyEffectiveness, float co2)
        {
            Gold = gold;
            FoodEffectiveness = foodEffectiveness;
            EnergyEffectiveness = energyEffectiveness;
            CO2 = co2;
        }
    }

    /// <summary>
    /// Calculates one round of production/consumption and effectiveness values,
    /// based on owned cells’ improvements and active trade deals.
    /// </summary>
    /// <remarks>
    /// The calculation pattern:
    ///  • Iterate cells → look up improvement stats and a per-output effectiveness map.
    ///  • Accumulate positive/negative Energy & Food flows to derive effectiveness (0..1).
    ///  • Apply effectiveness to Gold, and accumulate CO₂ outputs.
    ///  • Apply ongoing trades (Gold/Food/Energy), decrement rounds, and remove expired deals.
    /// </remarks>
    public NationStats calc()
    {
        Debug.Log("Calc");

        float positiveEnergie = 0f;
        float negativeEnergie = 0f;
        float positiveFood = 0f;
        float negativeFood = 0f;

        NationStats stats = new NationStats(0, 0, 0, 0);

        foreach (HexCell cell in cells)
        {
            Stats cellStats;
            int[] effectivenessMap;
            (cellStats, effectivenessMap) = GetCellStats(cell.getImprovement());

            float tileEffectiveness = cellStats.Effectiveness[cell.EffectivenessIndex];

            // Build a per-output effectiveness array for [Gold, Food, Energy, CO2].
            // 0 => unaffected by terrain effectiveness; 1 => multiplied by tileEffectiveness.
            float[] effectivenessArray = new float[effectivenessMap.Length];
            for (int i = 0; i < effectivenessMap.Length; i++)
            {
                effectivenessArray[i] = (effectivenessMap[i] == 0) ? 1f : tileEffectiveness;
            }

            // Gold output is affected by both national effectiveness (Food/Energy) and any per-tile effectiveness.
            stats.Gold += cellStats.GoldOutput
                          * effectivenessArray[0]
                          * nationStats.EnergyEffectiveness
                          * nationStats.FoodEffectiveness;

            // Food & Energy flows contribute to positive/negative totals for effectiveness computation.
            if (cellStats.FoodOutput > 0)
            {
                positiveFood += cellStats.FoodOutput * effectivenessArray[1] * nationStats.EnergyEffectiveness;
            }
            else
            {
                negativeFood -= cellStats.FoodOutput * effectivenessArray[1];
            }

            if (cellStats.EnergyOutput > 0)
            {
                positiveEnergie += cellStats.EnergyOutput * effectivenessArray[2];
            }
            else
            {
                negativeEnergie -= cellStats.EnergyOutput * effectivenessArray[2];
            }

            if (cellStats.EnergyOutput != 0) Debug.Log("Engergy " + cellStats.EnergyOutput);

            // CO₂ output respects the per-tile effectiveness.
            stats.CO2 += cellStats.CO2Output * effectivenessArray[3];
        }

        // Apply ongoing trade deals (per-round flows) and age them out.
        for (int i = tradeDeals.Count - 1; i >= 0; i--)
        {
            TradeStats tradeDeal = tradeDeals[i];

            stats.Gold += tradeDeal.Gold;

            if (tradeDeal.Food > 0) positiveFood += tradeDeal.Food;
            else                    negativeFood -= tradeDeal.Food;

            if (tradeDeal.Energy > 0) positiveEnergie += tradeDeal.Energy;
            else                       negativeEnergie -= tradeDeal.Energy;

            tradeDeal.Rounds -= 1;
            if (tradeDeal.Rounds == 0)
            {
                tradeDeals.RemoveAt(i);
            }
        }

        // Derive effectiveness in [0..1] from supply vs. demand (avoid div-by-zero).
        stats.EnergyEffectiveness = (negativeEnergie == 0) ? 1f : Math.Min(1f, positiveEnergie / negativeEnergie);
        stats.FoodEffectiveness   = (negativeFood   == 0) ? 1f : Math.Min(1f, positiveFood   / negativeFood);

        return stats;
    }

    /// <summary>
    /// Attempts to bind the owning player and (optionally) trigger cell assignment.
    /// </summary>
    public void AssignPlayerAndCells()
    {
        if (transform.parent != null)
        {
            player = transform.parent.GetComponent<Player>();
            player.nation = this;

            if (HexServer.Instance == null)
            {
                Debug.Log($"AssignPlayerAndCells called without a HexServer");
            }
            // Cell linking is performed via HexServer.AssignNationCells when id is set.
            // HexServer.Instance.AssignNationCells(this, player.GetCountry());
        }
    }

    /// <summary>
    /// Requests HexServer to link all cells belonging to this nation id and
    /// add them to this nation’s cell list.
    /// </summary>
    public void AssignCells()
    {
        if (transform.parent != null)
        {
            if (HexServer.Instance == null)
            {
                Debug.Log($"AssignPlayerAndCells called without a HexServer");
            }
            HexServer.Instance.AssignNationCells(this, player.GetCountry());
        }
    }

    /// <summary>
    /// Returns the most recently applied stats snapshot.
    /// </summary>
    public NationStats GetStats()
    {
        return nationStats;
    }

    /// <summary>
    /// Returns the authoritative game time (round) from <see cref="HexServer"/>.
    /// </summary>
    public int GetTime()
    {
        return HexServer.Instance.time.Value;
    }

    /// <summary>
    /// Server-only: transfers ownership of this nation to another client.
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

    /// <summary>
    /// Applies an immediate gold change (e.g., one-time trade transfer).
    /// </summary>
    public void changeGold(float gold)
    {
        nationStats.Gold += gold;
    }

    /// <summary>
    /// Adds a new per-round trade effect that lasts for the given number of rounds.
    /// </summary>
    public void AddTradeDeal(float gold, float food, float energy, int rounds)
    {
        tradeDeals.Add(new TradeStats(gold, food, energy, rounds));
    }

    /// <summary>
    /// Returns the balancing stats for the given <see cref="TileImprovement"/> and
    /// a per-output effectiveness map indicating which outputs are affected by the
    /// tile’s effectiveness curve.
    /// 
    /// Effectiveness map layout: [Gold, Food, Energy, CO2]
    ///   • 0 => output is unaffected by tile effectiveness (multiplier = 1)
    ///   • 1 => output is scaled by tile effectiveness (multiplier = tileEffectiveness)
    /// </summary>
    public static (Stats cellstats, int[] effectivenessMap) GetCellStats(TileImprovement improvement)
    {
        Stats cellStats;
        int[] effectivenessMap = new int[4] { 0, 0, 0, 0 };

        switch (improvement)
        {
            case TileImprovement.Forrest:
                cellStats = BalancingStats.Forrest;
                break;

            case TileImprovement.City:
                cellStats = BalancingStats.City;
                effectivenessMap[0] = 1; // Gold affected
                break;

            case TileImprovement.Village:
                cellStats = BalancingStats.Village;
                effectivenessMap[0] = 1; // Gold affected
                break;

            case TileImprovement.GreenCity:
                cellStats = BalancingStats.GreenCity;
                effectivenessMap[0] = 1; // Gold affected
                break;

            case TileImprovement.SolarPark:
                cellStats = BalancingStats.SolarPark;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.WindPark:
                cellStats = BalancingStats.WindPark;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.Farm:
                cellStats = BalancingStats.Farm;
                effectivenessMap[1] = 1; // Food affected
                break;

            case TileImprovement.CoalPowerPlant:
                cellStats = BalancingStats.CoalPowerPlant;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.GasPowerPlant:
                cellStats = BalancingStats.GasPowerPlant;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.NuclearPowerPlant:
                cellStats = BalancingStats.NuclearPlant;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.HydroPowerPlant:
                cellStats = BalancingStats.HydroPlant;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.ImprovedFarm:
                cellStats = BalancingStats.ImprovedFarm;
                effectivenessMap[1] = 1; // Food affected
                break;

            case TileImprovement.ImprovedSolarPark:
                cellStats = BalancingStats.ImprovedSolarPark;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.ImprovedWindPark:
                cellStats = BalancingStats.ImprovedWindPark;
                effectivenessMap[2] = 1; // Energy affected
                break;

            case TileImprovement.ForrestAndVillage:
                // Composite: a village in a forested tile.
                cellStats = BalancingStats.Village;
                effectivenessMap[0] = 1; // Gold affected (via village)
                cellStats.CO2Output += BalancingStats.Forrest.CO2Output; // add forest’s CO₂ modifier
                break;

            default:
                cellStats = BalancingStats.None;
                break;
        }

        return (cellStats, effectivenessMap);
    }
}
