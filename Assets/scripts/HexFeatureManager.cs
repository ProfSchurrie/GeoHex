/*
 * Project: GeoHex
 * File: HexFeatureManager.cs
 * Author: Sören Coremans (based on Catlike Coding's Hex Map tutorial)
 * Source: https://catlikecoding.com/unity/tutorials/hex-map/
 *
 * Description:
 * Places visual features (urban/plant decorations, bridges, specials) on hex cells.
 * Uses a deterministic hash (via HexMetrics.SampleHashGrid) to pick and orient prefabs,
 * ensuring identical decoration layouts across clients/seeds.
 *
 * Responsibilities:
 *  • Maintain a per-chunk "Features Container" transform for instantiated props.
 *  • Choose urban/plant prefabs based on cell levels and per-level thresholds.
 *  • Add special features via SpecialIndex (1-based) and bridges across roads/rivers.
 *
 * Notes:
 *  • Thresholds come from HexMetrics.GetFeatureThresholds(level - 1).
 *  • SpecialIndex is 1-based; array access uses [SpecialIndex - 1].
 *  • Position Y is lifted by half the prefab height; XZ is perturbed by noise.
 */

using Unity.Netcode;
using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    
    Transform container;

    /// <summary>
    /// Collections of urban prefabs per tier; indexed by threshold tier (not directly by level).
    /// </summary>
    public HexFeatureCollection[] urbanCollections;

    /// <summary>
    /// Collections of plant prefabs per tier; indexed by threshold tier.
    /// </summary>
    public HexFeatureCollection[] plantCollection;

    /// <summary>
    /// Array of "special" prefabs addressed by (cell.SpecialIndex - 1).
    /// </summary>
    public Transform[] special;

    /// <summary>
    /// Bridge prefab (scaled along Z to span between two road centers).
    /// </summary>
    public Transform bridge;

    public void Apply() {}

    /// <summary>
    /// Clears and recreates the features container. Call before re-triangulation or re-population.
    /// </summary>
    public void Clear()
    {
        if (container)
        {
            Destroy(container.gameObject);
        }
        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    /// <summary>
    /// Adds a single decoration feature at <paramref name="position"/> for the given <paramref name="cell"/>.
    /// Chooses between urban and plant collections using deterministic hash thresholds, then places it with
    /// perturbed position and random yaw from the hash.
    /// </summary>
    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexHash hash = HexMetrics.SampleHashGrid(position);

        // Pick urban by cell.UrbanLevel, fall back to plant by PlantLevel
        Transform prefab = PickPrefab(urbanCollections, cell.UrbanLevel, hash.a, hash.d);
        Transform otherPrefab = PickPrefab(plantCollection, cell.PlantLevel, hash.b, hash.d);

        if (prefab)
        {
            if (otherPrefab && hash.b < hash.a)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }

        Transform instance = Instantiate(prefab);

        // Lift by half height so meshes sit on the ground nicely
        position.y += instance.localScale.y * 0.5f;

        // Perturb XZ for natural variation and give a deterministic yaw
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);

        instance.SetParent(container, false);
    }

    /// <summary>
    /// Picks a prefab from a collection set based on the level and hash thresholds.
    /// </summary>
    Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Spawns and scales a bridge between two road centers. Scale Z is set from the distance and
    /// normalized by HexMetrics.bridgeDesignLength. Orientation is aligned along the span vector.
    /// </summary>
    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
    {
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);

        Transform instance = Instantiate(bridge);
        instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
        instance.forward = roadCenter2 - roadCenter1;

        float length = Vector3.Distance(roadCenter1, roadCenter2);
        instance.localScale = new Vector3(1f, 1f, length * (1f / HexMetrics.bridgeDesignLength));

        instance.SetParent(container, false);
    }

    /// <summary>
    /// Adds a "special" feature for the cell at the given position using SpecialIndex (1-based).
    /// Orientation uses deterministic yaw from the hash.
    /// </summary>
    public void AddSpecialFeature(HexCell cell, Vector3 position)
    {
        Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
        instance.localPosition = HexMetrics.Perturb(position);

        HexHash hash = HexMetrics.SampleHashGrid(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.e, 0f);

        instance.SetParent(container, false);
    }
}
