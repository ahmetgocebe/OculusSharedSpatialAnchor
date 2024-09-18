using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Mirror.Discovery;

/// <summary>
/// Manages network functionality for the application.
/// Handles network discovery, attempts to connect as a host in windows and editor,
/// Broadcasts server for discovery
/// and attemps to connect as client on android(oculus)
/// Also initializes the Oculus Platform if an HMD is present.
/// </summary>
public class NetworkController : MonoBehaviour
{
    // Reference to the NetworkManager component
    public NetworkManager networkManager;
    // Reference to the NetworkDiscovery component
    public NetworkDiscovery networkDiscovery;
    // Timer for internal logic (if needed)
    private float timer;

    /// <summary>
    /// Initializes the Oculus Platform if an HMD is present.
    /// </summary>
    private void Awake()
    {
        if (OVRManager.isHmdPresent)
        {
            // Initialize Oculus Platform
            Oculus.Platform.Core.Initialize();
        }
    }

    /// <summary>
    /// Attempts to start the network as either a client or a host.
    /// </summary>
    void Start()
    {
        TryStartNetwork();
    }

    /// <summary>
    /// Tries to start the network.
    /// It first starts network discovery to look for servers,
    /// and in the Unity Editor, it starts the host and advertises the server.
    /// </summary>
    void TryStartNetwork()
    {
        // Start network discovery to look for servers
        networkDiscovery.StartDiscovery();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Try to connect as a client
        Debug.Log("Attempting to connect as a client...");
        // If no server is found, start as host
        networkManager.StartHost();
        // Advertise the server to make it discoverable
        networkDiscovery.AdvertiseServer();
#endif
    }

    /// <summary>
    /// Called when a server is found through network discovery.
    /// Attempts to connect to the found server as a client.
    /// </summary>
    /// <param name="response">The server response containing the connection information.</param>
    public void OnServerFound(ServerResponse response)
    {
        // Start the client and connect to the server using the provided URI
        networkManager.StartClient(response.uri);
    }
}
