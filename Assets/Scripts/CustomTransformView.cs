using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/// <summary>
/// CustomTransformView synchronizes the position and rotation of objects relative to a flag pole.
/// Handles sending and receiving relative transforms to ensure all clients are properly synced.
/// </summary>
public class CustomTransformView : NetworkBehaviour
{
    // Reference to the flag pole GameObject
    public GameObject flagPole;

    // Latest relative position and rotation of the object
    private Vector3 latestRelativePos;
    private Quaternion latestRelativeRot;

    // Rate of interpolation for smooth movement
    private float lerpRate = 1f; // Adjust this value as needed

    // Cached relative position and rotation received from other clients
    private Vector3 receivedRelativePos;
    private Quaternion receivedRelativeRot;

    /// <summary>
    /// Unity's Update method.
    /// Handles the synchronization of position and rotation relative to the flag pole.
    /// The local player sends updated transforms to the server, while non-local players interpolate.
    /// </summary>
    void Update()
    {
        // If this is the server, no need to perform client-side transform updates
        if (isServer) return;

        // If the flag pole is not found, try to find it
        if (flagPole == null)
        {
            Debug.Log("Flag pole is null");
            flagPole = GameObject.FindAnyObjectByType<OVRSpatialAnchor>().gameObject;
            return;
        }

        // Local player sends transform data to the server
        if (isLocalPlayer)
        {
            Debug.Log("Sending transform");

            // Calculate the position and rotation relative to the flag pole
            latestRelativePos = flagPole.transform.InverseTransformPoint(transform.position);
            latestRelativeRot = Quaternion.Inverse(flagPole.transform.rotation) * transform.rotation;

            // Send the relative position and rotation to the server
            CmdSendRelativeTransform(latestRelativePos, latestRelativeRot);
        }
        else
        {
            // Non-local players smoothly interpolate to the received relative transform
            transform.position = flagPole.transform.TransformPoint(receivedRelativePos);
            transform.rotation = flagPole.transform.rotation * receivedRelativeRot;
        }
    }

    /// <summary>
    /// Command method called on the client to send relative position and rotation to the server.
    /// The server then propagates this data to all other clients.
    /// </summary>
    /// <param name="relativePos">The position relative to the flag pole.</param>
    /// <param name="relativeRot">The rotation relative to the flag pole.</param>
    [Command]
    private void CmdSendRelativeTransform(Vector3 relativePos, Quaternion relativeRot)
    {
        // Call the RPC method to update all clients with the new relative position and rotation
        RpcUpdateRelativeTransform(relativePos, relativeRot);
    }

    /// <summary>
    /// ClientRpc method that is called on all clients to update the relative position and rotation.
    /// Only non-local players will update their transform.
    /// </summary>
    /// <param name="relativePos">The position relative to the flag pole.</param>
    /// <param name="relativeRot">The rotation relative to the flag pole.</param>
    [ClientRpc]
    private void RpcUpdateRelativeTransform(Vector3 relativePos, Quaternion relativeRot)
    {
        // Only non-local players should update their transform using the received data
        if (!isLocalPlayer)
        {
            Debug.Log("Receiving transform");
            receivedRelativePos = relativePos;
            receivedRelativeRot = relativeRot;
        }
    }
}
