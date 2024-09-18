using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using Oculus.Platform;
using UnityEngine;
using UnityEngine.Device;

/// <summary>
/// AnchorManager is responsible for managing spatial anchors in an Oculus VR environment.
/// It allows for creating, saving, sharing, and loading spatial anchors between users.
/// The class also handles the interaction between the user's right hand and the anchor placement system.
/// </summary>
public class AnchorManager : NetworkBehaviour
{
    // The GameObject representing the user's right hand.
    public GameObject rightHand;

    // Prefab for creating an anchor object.
    public GameObject anchorPrefab;

    // Reference to the OVRSpatialAnchor component.
    public OVRSpatialAnchor anchor;

    // Placeholder for the anchor, used before placing it.
    public GameObject anchorPlaceholder;

    // Boolean to check if the anchor has been placed.
    bool isPlaced = false;

    // Oculus App ID for platform initialization.
    public string appId;

    // Oculus user ID used for sharing anchors.
    public ulong userId;

    /// <summary>
    /// Unity's Awake method.
    /// Initializes the Oculus platform with the provided App ID.
    /// </summary>
    private void Awake()
    {
        Core.Initialize(appId);
    }

    /// <summary>
    /// Mirror's method to handle local player initialization.
    /// Sets up anchor placement and starts the coroutine to wait for network loading.
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (!isLocalPlayer) return;
        if (isServer) return;

        // Start coroutine to wait for anchor data from the network
        StartCoroutine(WaitForNetworkToLoadAnchor());

        // Find the right hand and anchor placeholder objects
        rightHand = GameObject.Find("RightControllerAnchor");
        anchorPlaceholder = GameObject.Find("AnchorPlaceholder");
    }

    /// <summary>
    /// Coroutine that waits until the anchor file system is loaded, then loads anchors by UUID if available.
    /// </summary>
    IEnumerator WaitForNetworkToLoadAnchor()
    {
        yield return new WaitUntil(() => AnchorFileSystem.Instance != null);

        // If an anchor exists in the AnchorFileSystem, load it using its UUID
        if (String.IsNullOrEmpty(AnchorFileSystem.Instance.Anchor) == false)
        {
            LoadAnchorsByUuid(new List<Guid>() { Guid.Parse(AnchorFileSystem.Instance.Anchor) });
        }
    }

    /// <summary>
    /// Unity's Update method.
    /// Handles positioning the anchor placeholder and placing the anchor when the user presses the button.
    /// </summary>
    private void Update()
    {
        if (isServer) return;
        if (!isLocalPlayer) return;
        if (isPlaced == true) return;

        // Move the anchor placeholder to the same position as the right hand, but maintain the original Y-position
        anchorPlaceholder.transform.position = rightHand.transform.position - new Vector3(0, rightHand.transform.position.y, 0);

        // Sync the Y-axis rotation of the right hand to the anchor placeholder
        Vector3 rightHandRotation = rightHand.transform.eulerAngles;
        Vector3 anchorPlaceholderRotation = anchorPlaceholder.transform.eulerAngles;
        anchorPlaceholderRotation.y = rightHandRotation.y;
        anchorPlaceholder.transform.eulerAngles = anchorPlaceholderRotation;

        // Find the right hand object if it is null
        if (rightHand == null)
        {
            rightHand = GameObject.Find("RightControllerAnchor");
        }

        // Place the anchor if the button is pressed
        if (isPlaced == false && OVRInput.GetDown(OVRInput.Button.One))
        {
            Debug.Log("begin the anchor");
            isPlaced = true;
            PlaceAnchor();
        }
    }

    /// <summary>
    /// Initiates the process of creating and placing the spatial anchor.
    /// </summary>
    private void PlaceAnchor()
    {
        StartCoroutine(CreateSpatialAnchor());
    }

    /// <summary>
    /// Coroutine that creates a spatial anchor at the right hand's position and rotation.
    /// </summary>
    IEnumerator CreateSpatialAnchor()
    {
        // Instantiate the anchor prefab and position it at the right hand
        var go = Instantiate(anchorPrefab);
        go.transform.position = rightHand.transform.position - new Vector3(0, rightHand.transform.position.y, 0);
        go.transform.rotation = rightHand.transform.rotation;

        // Add the OVRSpatialAnchor component and wait until the anchor is created
        var anchor = go.AddComponent<OVRSpatialAnchor>();
        yield return new WaitUntil(() => anchor.Created);

        Debug.Log("anchor created");
        Debug.Log($"Created anchor {anchor.Uuid}");

        // Save the anchor once it is created
        SaveAnchor(anchor);
    }

    /// <summary>
    /// Saves the spatial anchor and stores its UUID in PlayerPrefs for future use.
    /// </summary>
    /// <param name="anchor">The OVRSpatialAnchor to be saved.</param>
    public async void SaveAnchor(OVRSpatialAnchor anchor)
    {
        var result = await anchor.SaveAnchorAsync();
        if (result.Success)
        {
            PlayerPrefs.SetString("Anchor", anchor.Uuid.ToString());
            Debug.Log($"-Anchor {anchor.Uuid} saved successfully.");
            ShareAnchor(anchor);
        }
        else
        {
            Debug.LogError($"Anchor {anchor.Uuid} failed to save with error {result.Status}");
        }
    }

    /// <summary>
    /// Shares the spatial anchor with another user via Oculus Platform.
    /// </summary>
    /// <param name="anchor">The OVRSpatialAnchor to be shared.</param>
    private async void ShareAnchor(OVRSpatialAnchor anchor)
    {
        var result = await anchor.ShareAsync(new OVRSpaceUser(userId));
        if (result == OVRSpatialAnchor.OperationResult.Success)
        {
            Debug.Log("Anchor shared successfully.");
            CmdSaveAnchor(anchor.Uuid.ToString());
        }
        else
        {
            Debug.LogError("Anchor could not be shared.");
        }
    }

    /// <summary>
    /// Command to save the anchor's UUID on the server and sync it across clients.
    /// </summary>
    /// <param name="anchorId">The UUID of the anchor to save.</param>
    [Command]
    private void CmdSaveAnchor(string anchorId)
    {
        Debug.Log("CmdSaveAnchor");
        AnchorFileSystem.Instance.SaveAnchorToXml(anchorId);

        // Call the client RPC to notify clients about the saved anchor
        RpcSaveAnchor(anchorId);
    }

    /// <summary>
    /// Client RPC to load the anchor by its UUID on all clients.
    /// </summary>
    /// <param name="anchorId">The UUID of the anchor to load.</param>
    [ClientRpc]
    private void RpcSaveAnchor(string anchorId)
    {
        if (isServer) return;
        if (isPlaced) return;
        Debug.Log("RpcSaveAnchor on client");

        Guid g = new Guid(PlayerPrefs.GetString("anchor"));
        Debug.Log("Parsed guid " + g);
        if (g != Guid.Empty)
        {
            LoadAnchorsByUuid(new List<Guid>() { g });
        }
    }

    // List to store unbound anchors loaded from the Oculus Platform
    List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();

    /// <summary>
    /// Loads spatial anchors by UUIDs from the Oculus Platform and attempts to localize them.
    /// </summary>
    /// <param name="uuids">A collection of UUIDs representing the anchors to be loaded.</param>
    async void LoadAnchorsByUuid(IEnumerable<Guid> uuids)
    {
        // Log the UUIDs being loaded
        foreach (var uuid in uuids)
        {
            Debug.Log($"Attempting to load anchor with UUID: {uuid}");
        }

        // Load the unbound anchors
        var result = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(uuids, _unboundAnchors);
        Debug.Log("status of loading unbound " + result.Status);
        if (result.Success)
        {
            Debug.Log("Anchors loaded successfully.");
            Debug.Log($"LoadUnboundAnchorsAsync returned {_unboundAnchors.Count} unbound anchors");

            // Localize and bind each loaded anchor
            foreach (var unboundAnchor in result.Value)
            {
                Debug.Log($"Unbound anchor UUID: {unboundAnchor.Uuid}");

                unboundAnchor.LocalizeAsync().ContinueWith((success, anchor) =>
                {
                    if (success)
                    {
                        var spatialAnchor = Instantiate(anchorPrefab).AddComponent<OVRSpatialAnchor>();
                        Debug.Log($"Anchor localized successfully with UUID: {unboundAnchor.Uuid}");

                        unboundAnchor.BindTo(spatialAnchor);
                    }
                    else
                    {
                        Debug.LogError($"Localization failed for anchor {unboundAnchor.Uuid}");
                    }
                }, unboundAnchor);
            }
            isPlaced = true;
        }
        else
        {
            Debug.LogError($"Load failed with error: {result.Status}. No anchors loaded.");
            Debug.LogError($"Unbound anchors count after failure: {_unboundAnchors.Count}");
        }
    }
}
