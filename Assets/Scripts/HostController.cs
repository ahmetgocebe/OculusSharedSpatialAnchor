using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// HostController is responsible for managing players and spawning specific GameObjects on the server.
/// It tracks player connections and spawns objects at the start of the game.
/// </summary>
public class HostController : NetworkBehaviour
{
    // List of players connected to the server
    public List<NetworkConnectionToServer> players = new List<NetworkConnectionToServer>();

    // List of GameObjects to spawn when the server starts
    public List<GameObject> SpawnOnStart = new List<GameObject>();

    /// <summary>
    /// Called when the server starts. 
    /// If this object is owned by the local player, it starts the spawning coroutine.
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();

        // Check if this object is owned by the local player and start the spawning process
        if (isLocalPlayer && isOwned)
        {
            StartCoroutine(Spawn());
        }
    }

    /// <summary>
    /// Coroutine that spawns objects listed in SpawnOnStart.
    /// Waits for a short delay between each spawn to prevent spawning all at once.
    /// </summary>
    IEnumerator Spawn()
    {
        foreach (var item in SpawnOnStart)
        {
            // Instantiate and spawn each GameObject on the server
            GameObject g = Instantiate(item);
            NetworkServer.Spawn(g, this.gameObject);

            // Wait for 0.5 seconds before spawning the next object
            yield return new WaitForSeconds(0.5f);
        }
    }
}
