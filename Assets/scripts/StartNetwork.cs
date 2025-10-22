/*
 * Project: GeoHex
 * File: StartNetwork.cs
 * Author: Sören Coremans
 * Description:
 * Entry point for initializing multiplayer sessions in Geohex.
 * Handles starting the game in Host, Client, or Server mode,
 * instantiates the shared HexGrid world, and links the map camera and editor.
 */

using Unity.Netcode;
using UnityEngine;

public class StartNetwork : MonoBehaviour
{
    /// <summary>
    /// Prefab reference to the <see cref="HexGrid"/> object representing the world.
    /// This is instantiated locally for each connected instance when the game begins.
    /// </summary>
    public HexGrid hexGridPrefab;

    /// <summary>
    /// Prefab reference to the <see cref="HexServer"/> component responsible
    /// for managing multiplayer synchronization on the host.
    /// </summary>
    public HexServer serverPrefab;

    /// <summary>
    /// Reference to the world camera used for map navigation and player view.
    /// </summary>
    public HexMapCamera mapCamera;

    /// <summary>
    /// Starts the game in <b>Server-only</b> mode.
    /// </summary>
    /// <remarks>
    /// This function is used exclusively for testing and debugging.
    /// Regular gameplay never runs in pure server mode — players can only
    /// join as Hosts or Clients.
    /// </remarks>
    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        StartGame();
        Instantiate(serverPrefab);
    }

    /// <summary>
    /// Starts the game in <b>Client</b> mode, connecting to an existing host.
    /// </summary>
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        StartGame();
    }

    /// <summary>
    /// Starts the game in <b>Host</b> mode, acting as both server and client.
    /// </summary>
    /// <remarks>
    /// This is the standard mode used by the teacher in a classroom session.
    /// </remarks>
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        StartGame();
        Instantiate(serverPrefab);
    }

    /// <summary>
    /// Handles shared game initialization after the network has been started.
    /// Instantiates the HexGrid, starts the world logic, and connects it to
    /// the map camera and editor.
    /// </summary>
    private void StartGame()
    {
        HexGrid hexgrid = Instantiate(hexGridPrefab);
        hexgrid.StartGame();

        // Assign the new grid to camera and editor systems
        mapCamera.AssignHexGrid(hexgrid);
        HexMapEditor.Instance.AssignHexGrid(hexgrid);
    }
}
