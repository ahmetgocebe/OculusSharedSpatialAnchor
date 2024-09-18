using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Platform;
using System;
using System.Threading.Tasks;
/*
 colocation_multiplayer
-app id 7870468413080520
-user id 8247117388714542
 */
public class AnchorNoNetwork : MonoBehaviour
{
    public GameObject rightHand;
    public GameObject anchorPrefab;
    [HideInInspector] public OVRSpatialAnchor anchor;
    public GameObject anchorPlaceholder;
    bool isPlaced = false;

    private void Start()
    {
        Core.Initialize("8084137635032380");
        rightHand = GameObject.Find("RightControllerAnchor");
        anchorPlaceholder = GameObject.Find("AnchorPlaceholder");
    }
    private void Update()
    {
        if (rightHand == null)
        {
            rightHand = GameObject.Find("RightControllerAnchor");
        }
        AlignPlaceHolder();


        if (isPlaced == false)// only place new if there is nothing placed
        {
            // Place the anchor when the button is pressed
            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                Debug.Log("begin the anchor");
                isPlaced = true;
                StartCoroutine(CreateSpatialAnchor());
            }
        }
    }

    IEnumerator CreateSpatialAnchor()
    {
        yield return new WaitUntil(Core.IsInitialized);
        Debug.Log("Core Initialized");
        var go = Instantiate(anchorPrefab);
        go.transform.SetParent(null, true);
        go.transform.position = rightHand.transform.position; //- new Vector3(0, pos.y, 0);
        go.transform.rotation = rightHand.transform.rotation;
        var anchor = go.AddComponent<OVRSpatialAnchor>();

        // Wait for the async creation
        yield return new WaitUntil(() => anchor.Created);
        Debug.Log("anchor created");

        Debug.Log($"Created anchor {anchor.Uuid}");
        SaveAnchor(anchor);
    }

    private async void SaveAnchor(OVRSpatialAnchor anchor)
    {
        var result = await anchor.SaveAnchorAsync();
        if (result.Success)
        {
            Debug.Log($"-Anchor {anchor.Uuid} saved successfully.");
            ShareAnchor(anchor);
        }
        else
        {
            Debug.LogError($"Anchor {anchor.Uuid} failed to save with error {result.Status}");
        }
    }
    //7912813695494047
    public ulong userId = 7912813695494047;
    private async void ShareAnchor(OVRSpatialAnchor anchor)
    {
        Debug.Log("Sharing");
        OVRSpaceUser us = new OVRSpaceUser();
        try
        {
            us = new OVRSpaceUser(userId);
        }
        catch (Exception e)
        {
            Debug.Log("User could not created " + e.Message);
        }
        try
        {
            OVRSpatialAnchor.OperationResult result;
            result = await anchor.ShareAsync(us);

            if (result == OVRSpatialAnchor.OperationResult.Success)
            {
                Debug.Log("Anchor shared successfully.");
                // CmdSaveAnchor(anchor.Uuid.ToString());
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure)
            {
                Debug.LogError("Anchor sharing failed due to a general error.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure_DataIsInvalid)
            {
                Debug.LogError("Anchor sharing failed: The anchor is invalid.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure_SpaceNetworkRequestFailed)
            {
                Debug.LogError("Anchor sharing failed due to a network error.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure_SpaceCloudStorageDisabled)
            {
                Debug.LogError("Anchor sharing failed: Cloud map service is unavailable.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure_SpaceNetworkTimeout)
            {
                Debug.LogError("Anchor sharing failed due to a cloud map upload timeout.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure_SpaceLocalizationFailed)
            {
                Debug.LogError("Anchor sharing failed: localization.");
            }
            else if (result == OVRSpatialAnchor.OperationResult.Failure)
            {
                Debug.LogError("Anchor sharing failed: Unknown error.");
            }
            else
            {
                Debug.LogError("Anchor could not be shared. Result: " + result);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"An error occurred while sharing the anchor: {ex.Message}");
        }
    }
    List<OVRSpatialAnchor.UnboundAnchor> _unboundAnchors = new();
    async void LoadAnchorsByUuid(IEnumerable<Guid> uuids)
    {
        // Log the UUIDs you're attempting to load
        foreach (var uuid in uuids)
        {
            Debug.Log($"Attempting to load anchor with UUID: {uuid}");
        }

        // Step 1: Load
        var result = await OVRSpatialAnchor.LoadUnboundSharedAnchorsAsync(uuids, _unboundAnchors);
        Debug.Log("status of loading unbound " + result.Status);
        if (result.Success)
        {
            Debug.Log("Anchors loaded successfully.");
            Debug.Log($"LoadUnboundAnchorsAsync returned {_unboundAnchors.Count} unbound anchors");

            // Note: result.Value is the same as _unboundAnchors
            foreach (var unboundAnchor in result.Value)
            {
                Debug.Log($"Unbound anchor UUID: {unboundAnchor.Uuid}");

                // Step 2: Localize
                unboundAnchor.LocalizeAsync().ContinueWith((success, anchor) =>
                {
                    if (success)
                    {
                        // Create a new game object with an OVRSpatialAnchor component
                        var spatialAnchor = Instantiate(anchorPrefab).AddComponent<OVRSpatialAnchor>();
                        Debug.Log($"Anchor localized successfully with UUID: {unboundAnchor.Uuid}");
                        SaveAnchor(spatialAnchor);
                        // Step 3: Bind
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


    private void AlignPlaceHolder()
    {
        // Move anchorPlaceholder to the same position as the right hand but without changing its Y-position.
        anchorPlaceholder.transform.position = rightHand.transform.position - new Vector3(0, rightHand.transform.position.y, 0);

        // Sync only the Y-axis rotation from rightHand to anchorPlaceholder
        Vector3 rightHandRotation = rightHand.transform.eulerAngles;
        Vector3 anchorPlaceholderRotation = anchorPlaceholder.transform.eulerAngles;

        // Apply only the Y rotation of the right hand to the anchor placeholder
        anchorPlaceholderRotation.y = rightHandRotation.y;
        anchorPlaceholder.transform.eulerAngles = anchorPlaceholderRotation;
    }
}
