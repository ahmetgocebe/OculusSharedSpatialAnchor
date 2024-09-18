using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlagPole : MonoBehaviour
{
    public static FlagPole Instance;

    private void Awake()
    {
        Instance = this;   
    }


}
