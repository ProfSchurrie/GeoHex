/*
 * Project: GeoHex
 * File: SolarField.cs
 * Author: Alexander Kautz
 * Description:
 * Specialized class for solar field structures.
 * Rotates the panel model to face a fixed "sun direction" point,
 * giving the impression of solar alignment. Intended for visual
 * realism rather than gameplay effect.
 *
 * Future Work:
 *  • Extend to dynamically follow a moving sun vector for a day–night cycle.
 */

using UnityEngine;

/// <summary>
/// Rotates the solar field to face a fixed point representing the sun.
/// </summary>
public class SolarField : MonoBehaviour
{
    /// <summary>
    /// Static direction vector of the sun (world-space).
    /// </summary>
    private static readonly Vector3 solarDirection = new Vector3(0.3f, 0.0f, -0.7f);

    private void Start()
    {
        // Move the solar direction back by 1000 units to simulate a distant sun
        Vector3 solarPoint = solarDirection * -1000.0f;

        // Rotate the solar field to face the sun's position
        transform.LookAt(solarPoint);
    }
}