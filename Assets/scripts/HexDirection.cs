/*
 * Project: GeoHex
 * File: HexDirection.cs
 * Author: Sören Coremans (based on original work by Jasper Flick)
 * Source: Adapted from the "Hex Map" tutorial by Jasper Flick
 *         https://catlikecoding.com/unity/tutorials/hex-map/
 * Description:
 * Enum for the six hex directions (pointy-top layout) and convenience
 * extension methods for neighbor traversal with wrap-around (previous/next,
 * +/-2 steps, and opposite).
 */

/// <summary>
/// Six directions around a hex cell in pointy-top orientation:
/// NE → E → SE → SW → W → NW (clockwise).
/// </summary>
public enum HexDirection
{
    NE, E, SE, SW, W, NW
}

/// <summary>
/// Helper extensions for cyclic direction arithmetic (wrap-around).
/// </summary>
public static class HexDirectionExtensions
{
    /// <summary>
    /// Returns the opposite direction (three steps away).
    /// </summary>
    public static HexDirection Opposite(this HexDirection direction)
    {
        return (int)direction < 3 ? (direction + 3) : (direction - 3);
    }

    /// <summary>
    /// Returns the previous direction (one step counter-clockwise), wrapping from NE→NW.
    /// </summary>
    public static HexDirection Previous(this HexDirection direction)
    {
        return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
    }

    /// <summary>
    /// Returns the next direction (one step clockwise), wrapping from NW→NE.
    /// </summary>
    public static HexDirection Next(this HexDirection direction)
    {
        return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
    }

    /// <summary>
    /// Returns the direction two steps counter-clockwise, with wrap-around.
    /// </summary>
    public static HexDirection Previous2(this HexDirection direction)
    {
        direction -= 2;
        return direction >= HexDirection.NE ? direction : (direction + 6);
    }

    /// <summary>
    /// Returns the direction two steps clockwise, with wrap-around.
    /// </summary>
    public static HexDirection Next2(this HexDirection direction)
    {
        direction += 2;
        return direction <= HexDirection.NW ? direction : (direction - 6);
    }
}