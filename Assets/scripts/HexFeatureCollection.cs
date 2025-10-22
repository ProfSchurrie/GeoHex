/*
 * Project: GeoHex
 * File: HexFeatureCollection.cs
 * Author: Sören Coremans (based on original work by Jasper Flick)
 * Source: Adapted from the "Hex Map" tutorial by Jasper Flick
 *         https://catlikecoding.com/unity/tutorials/hex-map/
 * Description:
 * Lightweight container for a group of related feature prefabs (e.g., houses for
 * a village tier, trees for a plant tier). Provides a simple picker that maps a
 * normalized random value to an index.
 *
 * Usage:
 *  • Pass a deterministic random in [0, 1) (e.g., from HexMetrics.SampleHashGrid)
 *    to pick a prefab for consistent decoration across clients/seeds.
 *
 * Note:
 *  • The picker assumes 0 ≤ choice < 1. If there’s any chance you’ll pass 1.0,
 *    clamp first to avoid indexing past the end.
 */

using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection {

	public Transform[] prefabs;

	public Transform Pick (float choice) {
		return prefabs[(int)(choice * prefabs.Length)];
	}
}