using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugTool : MonoBehaviour
{
    public static DebugTool Instance;
    public Text text;
    public GameObject panel;
    public bool openOnStart;
    public Image toggleImage1;
    public Image toggleImage2;
    private void Awake()
    {
        Instance = this;
    }
    public void Clear()
    {
        text.text = " ";
    }
    void OnEnable()
    {
        Application.logMessageReceived += LogCallback;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= LogCallback;
    }
    private string lastText = " ";
    void LogCallback(string logString, string stackTrace, LogType type)
    {
        if (text.text.Length > 16000) { text.text = string.Empty; }
        if (logString != lastText)
        {
            switch (type)
            {
                case LogType.Error:
                    logString = "<color=red>" + logString + "</color>";
                    break;
                case LogType.Assert:
                    return;
                case LogType.Warning:
                    logString = "<color=yellow>" + logString + "</color>";
                    return;
                case LogType.Log:
                    logString = "<color=black>" + logString + "</color>";
                    break;
                case LogType.Exception:
                    logString = "<color=cyan>" + logString + "</color>";
                    return;
                default:
                    break;
            }
            text.text += logString + "\n";
            text.text += "\n--------------------------\n";
            lastText = logString;
        }

    }
}