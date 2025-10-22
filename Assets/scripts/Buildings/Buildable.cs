/*
 * Project: GeoHex
 * File: Buildable.cs
 * Author: Alexander Kautz
 * Description:
 * Component attached to every buildable structure prefab (except cities).
 * Defines metadata such as name, placement restrictions, and special index
 * used by the HexFeatureManager to render the correct model on a cell.
 *
 * Note:
 *  • Cities use UrbanLevel instead of SpecialIndex.
 *  • Gold cost handling has been moved elsewhere and should be removed here.
 */

using UnityEngine;

/// <summary>
/// Metadata for a buildable structure prefab.
/// Defines where it can be placed and how it is referenced in the feature manager.
/// </summary>
public class Buildable : MonoBehaviour
{
    /// <summary>
    /// Display name of the building.
    /// </summary>
    public string buildingName;

    /// <summary>
    /// (Deprecated) Cost of constructing this building in gold.
    /// TODO: Remove this field once cost logic is fully managed externally.
    /// </summary>
    public float goldCost;

    /// <summary>
    /// Index corresponding to the feature in the FeatureManager prefab.
    /// Used by HexFeatureManager.AddFeature() to pick the right mesh.
    /// </summary>
    public int specialIndex;

    /// <summary>
    /// Whether the building can be placed on land tiles.
    /// </summary>
    public bool OnLand;

    /// <summary>
    /// Whether the building can be placed adjacent to or on a river.
    /// </summary>
    public bool OnRiver;

    /// <summary>
    /// Whether the building can be placed on water tiles.
    /// </summary>
    public bool OnWater;
}