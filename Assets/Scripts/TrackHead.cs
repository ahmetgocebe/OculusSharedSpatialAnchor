using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class TrackHead : NetworkBehaviour 
{
    private GameObject head;
    // Update is called once per frame
    void Update()
    {
        if (isOwned && isLocalPlayer)
        {
            if (head == null)
            {
                head = GameObject.Find("CenterEyeAnchor");
            }
            transform.position =Vector3.MoveTowards(transform.position,head.transform.position,0.8f);
            transform.rotation =Quaternion.Slerp(transform.rotation,head.transform.rotation,0.8f);
        }
    }
}
