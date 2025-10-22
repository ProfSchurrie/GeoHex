/*
 * Project: GeoHex
 * File: SpinnBlades.cs
 * Author: Alexander Kautz
 * Description:
 * Rotates the blades of wind turbines in wind farm structures.
 * Each turbine starts at a random initial rotation for visual variation,
 * then continuously spins at a defined speed.
 */

using UnityEngine;

/// <summary>
/// Handles continuous rotation of wind turbine blades.
/// Adds random initial rotation for visual diversity across turbines.
/// </summary>
public class SpinnBlades : MonoBehaviour
{
    /// <summary>
    /// Reference to the turbine blades GameObject to be rotated.
    /// </summary>
    [SerializeField]
    private GameObject wings;

    /// <summary>
    /// Rotation speed in degrees per second.
    /// </summary>
    [SerializeField]
    private float rotationSpeed = 100.0f;

    private void Start()
    {
        // Randomize initial rotation for visual diversity
        Random.InitState(Time.realtimeSinceStartupAsDouble.GetHashCode());
        wings.transform.Rotate(Vector3.forward, Random.value * 360.0f);
    }

    private void Update()
    {
        // Rotate continuously around the Z-axis
        wings.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }
}