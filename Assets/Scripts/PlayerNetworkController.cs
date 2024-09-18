using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
public class PlayerNetworkController : NetworkBehaviour
{
    public OVRManager oVRManager;
    public GameObject head;

    void Start()
    {
        if (isLocalPlayer)
        {
            RemoveHeadFromCameraRenderableLayer();

            if(!isServer) 
            {
                ClientOnItsSceneOperations();
            }
        }
    }

    private void RemoveHeadFromCameraRenderableLayer()
    {
        int layerIndex = LayerMask.NameToLayer("Monke");
        head = GameObject.Find("CenterEyeAnchor");
        head.GetComponent<Camera>().cullingMask &= ~(1 << layerIndex);
    }

    private void ClientOnItsSceneOperations()
    {

    }
}