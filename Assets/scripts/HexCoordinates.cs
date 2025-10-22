/*
 * Project: GeoHex
 * File: HexCoordinates.cs
 * Author: Sören Coremans (based on original work by Jasper Flick)
 * Source: Adapted from the "Hex Map" tutorial by Jasper Flick
 *         https://catlikecoding.com/unity/tutorials/hex-map/
 *
 * Description:
 * Hex grid coordinate helper supporting axial/cube-style coordinates.
 * Stores axial (x, z) with derived cube y = -x - z, provides conversions
 * from offset coordinates and world-space positions, and exposes readable
 * string representations for debugging.
 *
 * Notes:
 *  • FromOffsetCoordinates() converts from row-offset coordinates
 *    (every other row shifted; matches the tutorial’s layout).
 *  • FromPosition() converts Unity world positions to hex grid coordinates,
 *    including proper cube rounding to the nearest valid hex.
 */

using UnityEngine;

[System.Serializable]
public struct HexCoordinates {

	[SerializeField]
	private int x, z;

	public int X {
		get {
			return x;
		}
	}

	public int Z {
		get {
			return z;
		}
	}

	public int Y {
		get {
			return -X - Z;
		}
	}

	public HexCoordinates (int x, int z) {
		this.x = x;
		this.z = z;
	}

	public static HexCoordinates FromOffsetCoordinates (int x, int z) {
		return new HexCoordinates(x - z / 2, z);
	}

	public static HexCoordinates FromPosition (Vector3 position) {
		float x = position.x / (HexMetrics.innerRadius * 2f);
		float y = -x;

		float offset = position.z / (HexMetrics.outerRadius * 3f);
		x -= offset;
		y -= offset;

		int iX = Mathf.RoundToInt(x);
		int iY = Mathf.RoundToInt(y);
		int iZ = Mathf.RoundToInt(-x -y);

		if (iX + iY + iZ != 0) {
			float dX = Mathf.Abs(x - iX);
			float dY = Mathf.Abs(y - iY);
			float dZ = Mathf.Abs(-x -y - iZ);

			if (dX > dY && dX > dZ) {
				iX = -iY - iZ;
			}
			else if (dZ > dY) {
				iZ = -iX - iY;
			}
		}

		return new HexCoordinates(iX, iZ);
	}

	public override string ToString () {
		return "(" +
			X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
	}

	public string ToStringOnSeparateLines () {
		return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
	}
}