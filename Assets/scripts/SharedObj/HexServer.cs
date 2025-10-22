/*
 * Project: GeoHex
 * File: HexServer.cs
 * Author: Sören Coremans, Alexander Kautz
 * Description:
 * Authoritative multiplayer game server for GeoHex. Orchestrates:
 *  • Session bootstrap (spawning shared chunks/tiles and linking to the HexGrid)
 *  • Nation & player registration and territory ownership transfer
 *  • Global simulation state (round time, CO₂ escalation thresholds, sea-level rise)
 *  • Trade pipeline (TradeOffer → server-minted TradeDeal → client UI → processing)
 *
 * Notes:
 *  • Water level never decreases: CO₂ thresholds can fall back to a lower bucket,
 *    but sea level remains at the highest reached value by design.
 *  • offeredFood in trades is legacy from an older food model;
 *  • This class runs server-only logic inside guarded branches; clients still observe
 *    spawned TradeDeals and present/resolve offers locally as needed.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Singleton, authoritative server brain. Manages shared world objects,
/// global time & sea-level, nation/player registration, and the trade flow.
/// </summary>
public class HexServer : NetworkBehaviour
{
    /// <summary>
    /// Simple counter of connected players (currently unused; handy for debugging/future features).
    /// </summary>
    private int playerCount = 0;

    /// <summary>
    /// Server-tracked availability map for nation slots (index = nation tag).
    /// </summary>
    public NetworkVariable<List<bool>> nationsAssigned = new NetworkVariable<List<bool>>(
        new List<bool> { false, false, false, false, false, false , false},
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// Global round counter mirrored from the owning nation’s local tick (read by all clients).
    /// </summary>
    public NetworkVariable<int> time = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// CO₂ thresholds that trigger sea-level rise when the total exceeds the current bucket.
    /// </summary>
    public int[] CO2Levels = new int[] { 150000, 300000, 500000, 700000 };

    /// <summary>
    /// Difficulty modifier applied to total CO₂ before comparing against <see cref="CO2Levels"/>.
    /// </summary>
    public int CO2mod = 3;

    /// <summary>
    /// Current bucket index in <see cref="CO2Levels"/>. Set to -1 when the final threshold is surpassed.
    /// </summary>
    private int currentLevel;

    /// <summary>
    /// Global sea level. Increases on threshold crossings; does not decrease if CO₂ later drops.
    /// </summary>
    public NetworkVariable<int> waterLevel = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    /// <summary>
    /// All nations registered with the server.
    /// </summary>
    List<Nation> nations = new List<Nation>();

    /// <summary>
    /// All players registered with the server.
    /// </summary>
    List<Player> players = new List<Player>();

    /// <summary>
    /// Shared (networked) chunk managers, parallel to local <see cref="HexGridChunk"/>s.
    /// </summary>
    private SharedHexGridChunk[] chunks;

    /// <summary>
    /// Prefab for creating networked shared chunks at startup.
    /// </summary>
    public SharedHexGridChunk chunkPrefab;

    /// <summary>
    /// Prefab used to mint a <see cref="TradeDeal"/> when a player's <see cref="TradeOffer"/> is confirmed.
    /// </summary>
    public TradeDeal tradeDealPrefab;

    /// <summary>
    /// Reference to the local HexGrid (visual/logic side).
    /// </summary>
    private HexGrid hexGrid;

    /// <summary>
    /// Global singleton instance (server-side authority).
    /// </summary>
    public static HexServer Instance { get; private set; }

    private void Awake()
    {
        currentLevel = 0;

        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple instances of HexServer found! Destroying the duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Spawn this server object (server only)
        Spawn();

        // Prepare shared chunk array sized to the local HexGrid
        hexGrid = HexGrid.Instance;
        chunks = new SharedHexGridChunk[hexGrid.chunkCountX * hexGrid.chunkCountZ];

        // Server creates the networked chunk hierarchy and child shared tiles
        if (NetworkManager.Singleton.IsServer)
        {
            CreateChunks();
        }
    }

    private void Start()
    {
        // All peers (server & clients) discover/link existing shared chunks and tiles
        chunks = GetAllChildChunks();

        // React to sea-level changes: server floods all chunks; clients update owned cells
        waterLevel.OnValueChanged += RaiseWaterLevel;
    }

    /// <summary>
    /// Cleanup singleton on destroy.
    /// </summary>
    public override void OnDestroy()
    {
        base.OnDestroy();

        // Clear the instance when the object is destroyed
        if (Instance == this)
        {
            Instance = null;
        }
        // Optional hygiene:
        // waterLevel.OnValueChanged -= RaiseWaterLevel;
    }

    /// <summary>
    /// Server-only: spawns this network object.
    /// </summary>
    public void Spawn()
    {
        Debug.Log("is Server: " + NetworkManager.Singleton.IsServer);
        Debug.Log("is Client: " + NetworkManager.Singleton.IsClient);
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkObject.Spawn();
        }
    }

    private void Update()
    {
        // --- Server-side simulation & trade orchestration ---
        if (NetworkManager.Singleton.IsServer)
        {
            // Mirror authoritative time from the current nation (owner's local tick)
            if (Nation.CurrentNation != null)
            {
                time.Value = Nation.CurrentNation.time;
            }

            // Sea-level progression: compare total CO₂ (scaled) against current threshold
            if (currentLevel != -1 && TotalCO2() > CO2Levels[currentLevel] / CO2mod)
            {
                waterLevel.Value += 1;

                if (currentLevel == CO2Levels.Length - 1)
                {
                    // Final bucket reached; stop checking
                    currentLevel = -1;
                }
                currentLevel++;
            }

            // Trade pipeline: convert confirmed TradeOffers into TradeDeals
            foreach (Player player in players)
            {
                TradeOffer tradeOffer = player.tradeOffer;

                // Server mints a TradeDeal when the owner confirms and the offer hasn't been processed yet
                if (tradeOffer.confirmOffer.Value == 1 && tradeOffer.offerProcessed.Value == 0)
                {
                    TradeDeal tradeDeal = Instantiate(tradeDealPrefab);

                    // Copy the payload from the offer
                    tradeDeal.offeredMoney.Value = tradeOffer.offeredMoney.Value;

                    // Legacy: one-time food is deprecated
                    tradeDeal.offeredFood.Value = tradeOffer.offeredFood.Value;

                    tradeDeal.offeredFoodRounds.Value = tradeOffer.offeredFoodRounds.Value;
                    tradeDeal.offeredMoneyRounds.Value = tradeOffer.offeredMoneyRounds.Value;
                    tradeDeal.offeredEnergyRounds.Value = tradeOffer.offeredEnergyRounds.Value;
                    tradeDeal.rounds.Value = tradeOffer.rounds.Value;
                    tradeDeal.offeringCountry.Value = tradeOffer.offeringCountry.Value;
                    tradeDeal.otherCountry.Value = tradeOffer.otherCountry.Value;
                    tradeDeal.transform.SetParent(transform);

                    // Transfer deal ownership to the receiving player if online; otherwise discard
                    bool found = false;
                    foreach (Player otherPlayer in players)
                    {
                        if (otherPlayer.nation.id.Value == tradeOffer.otherCountry.Value)
                        {
                            found = true;
                            tradeDeal.ChangeOwnership(otherPlayer.NetworkObject.OwnerClientId);
                        }
                    }
                    if (!found)
                    {
                        Destroy(tradeDeal.gameObject);
                    }

                    // Mark the offer as processed to throttle further deal creation
                    tradeOffer.offerProcessed.Value = 1;
                }
                // Reset processed flag when the offer returns to idle state on the client
                else if (tradeOffer.confirmOffer.Value == 0 && tradeOffer.offerProcessed.Value == 1)
                {
                    tradeOffer.offerProcessed.Value = 0;
                }
            }
        }

        // --- Client- & Server-side: present/resolve outstanding TradeDeals locally ---
        foreach (TradeDeal tradeDeal in GetTradeDealsFromChildren())
        {
            if (!tradeDeal.shown)
            {
                // Receiving player: present the offer UI
                if (tradeDeal.otherCountry.Value == Player.CurrentPlayer.nation.id.Value)
                {
                    tradeDeal.ShowOffer();
                    tradeDeal.shown = true;
                }
                // Offering player: if deal accepted/declined remotely, process local bookkeeping
                else if (tradeDeal.offeringCountry.Value == Player.CurrentPlayer.nation.id.Value)
                {
                    if (tradeDeal.accept.Value != 0 && !tradeDeal.processedLocal)
                    {
                        tradeDeal.Process();
                    }
                }
            }

            // Intended server cleanup of stale TradeDeals (disabled due to prior bugs)
            /*
            if (NetworkManager.Singleton.IsServer)
            {
                if (tradeDeal.accept.Value != 0 && tradeDeal.rounds.Value <= Player.CurrentPlayer.nation.GetTime()-2)
                {
                    Destroy(tradeDeal.gameObject);
                }
            }
            */
        }
    }

    /// <summary>
    /// Collects all <see cref="TradeDeal"/>s parented to this server object.
    /// </summary>
    public List<TradeDeal> GetTradeDealsFromChildren()
    {
        List<TradeDeal> tradeDeals = new List<TradeDeal>();

        foreach (Transform child in transform)
        {
            TradeDeal tradeDeal = child.GetComponent<TradeDeal>();
            if (tradeDeal != null)
            {
                tradeDeals.Add(tradeDeal);
            }
        }
        return tradeDeals;
    }

    /// <summary>
    /// Server: creates all shared chunks, spawns them, and pre-creates their per-tile network objects.
    /// </summary>
    public void CreateChunks()
    {
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i] = Instantiate(chunkPrefab);
            chunks[i].Spawn();
            chunks[i].transform.SetParent(transform);
            chunks[i].CreateTiles();
            chunks[i].uniqueIndex.Value = i;
        }
    }

    /// <summary>
    /// Discovers all <see cref="SharedHexGridChunk"/> children, links them to the local
    /// <see cref="HexGridChunk"/>s via <see cref="HexGrid.LinkChunks(SharedHexGridChunk,int)"/>,
    /// and ensures each chunk links & caches its child tiles.
    /// </summary>
    public SharedHexGridChunk[] GetAllChildChunks()
    {
        Transform[] childTransforms = GetComponentsInChildren<Transform>();
        List<SharedHexGridChunk> chunkList = new List<SharedHexGridChunk>();

        int i = 0;

        foreach (Transform child in childTransforms)
        {
            SharedHexGridChunk chunk = child.GetComponent<SharedHexGridChunk>();
            if (chunk != null)
            {
                chunkList.Add(chunk);
                hexGrid.LinkChunks(chunk, chunk.uniqueIndex.Value);
                chunk.GetAllChildTiles();
                i++;
            }
        }

        return chunkList.ToArray();
    }

    /// <summary>
    /// Assigns the given client to control the specified nation, transfers ownership of
    /// all matching tiles across chunks, and marks the slot as taken. Returns the nation id.
    /// </summary>
    public int getCountry(ulong ClientId, int nationNum)
    {
        playerCount++;

        if (NetworkManager.Singleton.IsServer)
        {
            nationsAssigned.Value[nationNum] = true;
        }

        foreach (SharedHexGridChunk chunk in chunks)
        {
            chunk.TransferOwnership(ClientId, nationNum);
        }

        return nationNum;
    }

    /// <summary>
    /// Adds all cells for the given nation id to the provided <see cref="Nation"/>.
    /// </summary>
    public void AssignNationCells(Nation nation, int nationID)
    {
        foreach (SharedHexGridChunk chunk in chunks)
        {
            chunk.AssignCellToNation(nation, nationID);
        }
    }

    /// <summary>
    /// Responds to <see cref="waterLevel"/> changes.
    /// Server floods all chunks centrally; clients update their owned cells locally
    /// (resetting Urban/Plant/Special where a cell becomes submerged).
    /// </summary>
    public void RaiseWaterLevel(int waterLevelOld, int waterLevelNew)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (SharedHexGridChunk chunk in chunks)
            {
                chunk.chunk.RaiseWaterLevel(waterLevelNew);
            }
        }
        else
        {
            foreach (HexCell cell in Player.CurrentPlayer.nation.cells)
            {
                cell.WaterLevel = waterLevelNew;
                if (cell.IsUnderwater)
                {
                    cell.UrbanLevel = 0;
                    cell.PlantLevel = 0;
                    cell.SpecialIndex = 0;
                }
            }
        }
    }

    /// <summary>
    /// Registers a nation with the server (for CO₂ aggregation, etc.).
    /// </summary>
    public void AddNation(Nation nation)
    {
        nations.Add(nation);
    }

    /// <summary>
    /// Registers a player with the server (for offer processing, ownership transfers).
    /// </summary>
    public void AddPlayer(Player player)
    {
        players.Add(player);
    }

    /// <summary>
    /// Returns all registered players.
    /// </summary>
    public List<Player> GetPlayers()
    {
        return players;
    }

    /// <summary>
    /// Sums cumulative CO₂ across all nations (applies to sea-level progression).
    /// </summary>
    public float TotalCO2()
    {
        float co2 = 0;
        foreach (Nation nation in nations)
        {
            co2 += nation.CO2.Value;
        }
        return co2;
    }
}
