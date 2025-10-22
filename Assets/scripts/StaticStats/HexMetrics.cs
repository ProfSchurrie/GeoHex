/*
 * Project: GeoHex
 * File: HexMetrics.cs
 * Author: Sören Coremans (based on original work by Jasper Flick)
 * Source: Adapted from the "Hex Map" tutorial by Jasper Flick
 *         https://catlikecoding.com/unity/tutorials/hex-map/
 *
 * Description:
 * Central repository for all geometric constants and utility functions
 * used throughout the hexagonal map system. Handles coordinate geometry,
 * elevation steps, noise perturbation, and corner/edge calculations for
 * rendering hex tiles with smooth transitions and natural irregularities.
 *
 * Notes:
 *  • Most geometric parameters (corner ratios, solid/blend factors, 
 *    perturbation noise) are based on Jasper Flick’s reference math.
 */

using UnityEngine;

public static class HexMetrics
{
    // --- Hex Geometry Ratios ---
    // Relationship between outer and inner radii (defines hex proportions)
    public const float outerToInner = 0.866025404f;   // ≈ sqrt(3)/2
    public const float innerToOuter = 1f / outerToInner;
    public const float outerRadius = 10f;
    public const float innerRadius = outerRadius * outerToInner;

    // --- Color Blending and Cell Borders ---
    public const float solidFactor = 0.9f;            // Area within cell border
    public const float blendFactor = 1f - solidFactor; // Fraction used for blending

    // --- Elevation and Terracing ---
    public const float elevationStep = 3f;            // Height difference per elevation level
    public const int terracesPerSlope = 2;            // Steps between elevation levels
    public const int terraceSteps = terracesPerSlope * 2 + 1;
    public const float horizontalTerraceStepSize = 1f / terraceSteps;
    public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

    // --- Terrain Perturbation & Noise ---
    // Introduces randomness to break grid uniformity
    public const float cellPerturbStrength = 4f;
    public const float elevationPerturbStrength = 1.5f;
    public const float noiseScale = 0.003f;
    public static Texture2D noiseSource;

    // --- Chunk Configuration ---
    public const int chunkSizeX = 5, chunkSizeZ = 5;

    // --- Water and Rivers ---
    public const float streamBedElevationOffset = -1.75f; // Depth of rivers below terrain
    public const float waterElevationOffset = -0.5f;      // Height offset for water surface
    public const float waterFactor = 0.6f;                // Scaling of hex corners for water areas
    public const float waterBlendFactor = 1f - waterFactor;

    // --- Hash Grid ---
    // Used to ensure consistent pseudo-random placement (e.g. buildings/features)
    public const int hashGridSize = 256;
    public const float hashGridScale = 0.25f;
    static HexHash[] hashGrid;

    // --- Misc ---
    public const float bridgeDesignLength = 7f;           // Default bridge length
    public static Color[] terrainColors;
    public static Color[] countryColors;
    public static int mapMode = 0;                        // 0 = terrain, 1 = nations

    // --- Hash Grid Initialization ---
    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];
        Random.State currentState = Random.state;
        Random.InitState(seed);
        for (int i = 0; i < hashGrid.Length; i++)
            hashGrid[i] = HexHash.Create();
        Random.state = currentState;
    }

    // --- Feature Thresholds ---
    // Used by HexFeatureManager to decide which features appear on terrain.
    // Likely represents probability cutoffs for small, medium, and large features.
    static float[][] featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    public static float[] GetFeatureThresholds(int level) => featureThresholds[level];

    // --- Corner Geometry ---
    static Vector3[] corners = {
        new Vector3(0f, 0f, outerRadius),
        new Vector3(innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(0f, 0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
        new Vector3(0f, 0f, outerRadius)
    };

    // --- Noise Sampling ---
    public static Vector4 SampleNoise(Vector3 position)
        => noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);

    // --- Geometric Utility Methods ---
    public static Vector3 GetFirstCorner(HexDirection direction) => corners[(int)direction];
    public static Vector3 GetSecondCorner(HexDirection direction) => corners[(int)direction + 1];
    public static Vector3 GetFirstSolidCorner(HexDirection direction) => corners[(int)direction] * solidFactor;
    public static Vector3 GetSecondSolidCorner(HexDirection direction) => corners[(int)direction + 1] * solidFactor;
    public static Vector3 GetBridge(HexDirection direction)
        => (corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;
    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
        => (corners[(int)direction] + corners[(int)direction + 1]) * (0.5f * solidFactor);

    // --- Water Geometry ---
    public static Vector3 GetFirstWaterCorner(HexDirection direction) => corners[(int)direction] * waterFactor;
    public static Vector3 GetSecondWaterCorner(HexDirection direction) => corners[(int)direction + 1] * waterFactor;
    public static Vector3 GetWaterBridge(HexDirection direction)
        => (corners[(int)direction] + corners[(int)direction + 1]) * waterBlendFactor;

    // --- Terracing ---
    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        float v = ((step + 1) / 2) * verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * horizontalTerraceStepSize;
        return Color.Lerp(a, b, h);
    }

    // --- Edge Type Determination ---
    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2) return HexEdgeType.Flat;
        int delta = elevation2 - elevation1;
        return (delta == 1 || delta == -1) ? HexEdgeType.Slope : HexEdgeType.Cliff;
    }

    // --- Position Perturbation ---
    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
        return position;
    }

    // --- Deterministic Random Sampling ---
    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int)(position.x * hashGridScale) % hashGridSize;
        if (x < 0) x += hashGridSize;
        int z = (int)(position.z * hashGridScale) % hashGridSize;
        if (z < 0) z += hashGridSize;
        return hashGrid[x + z * hashGridSize];
    }
}
