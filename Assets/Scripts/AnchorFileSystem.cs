using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using System;
using UnityEngine.Events;
using Mirror;

public class AnchorFileSystem : NetworkBehaviour
{
    public static AnchorFileSystem Instance;

    // SyncVar to make _anchor networked with a hook to call when the value is updated
    [SyncVar(hook = nameof(OnAnchorUpdated))]
    [SerializeField] private string _anchor;

    public UnityAction<string> OnLoadAnchorUuId;

    public string Anchor
    {
        get { return _anchor; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (isServer && isOwned)
            UpdateAnchor(LoadAnchorFromXml());
    }
    [Command(requiresAuthority = false)]
    private void UpdateAnchor(string id)
    {
        _anchor = id;
    }
    // This method is called whenever the SyncVar _anchor is updated on clients
    private void OnAnchorUpdated(string oldValue, string newValue)
    {
        Debug.Log($"Anchor updated from {oldValue} to {newValue}");
        OnLoadAnchorUuId?.Invoke(newValue);  // Notify listeners of the updated anchor
    }

    // This method will save the anchor on the server and update the SyncVar
    [Server]
    public void SaveAnchorToXml(string uuid)
    {
        // If the anchor is already set, do nothing
        if (!string.IsNullOrEmpty(Anchor)) return;

        // Define the file path to the XML file
        string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "anchors.xml");

        // Create a new XML document with a single root element "Anchors"
        XDocument xmlDoc = new XDocument(new XElement("Anchors"));

        // Add a new anchor entry with the UUID, clearing previous anchors
        xmlDoc.Root.Add(new XElement("Anchor",
            new XElement("UUID", uuid),
            new XElement("SavedTime", DateTime.Now.ToString())
        ));

        // Save the document to the file, overwriting any existing file
        xmlDoc.Save(filePath);

        Debug.Log($"Anchor UUID {uuid} saved to XML file, previous data cleared.");

        // Set the _anchor value and update it for all clients
        _anchor = uuid;
    }

    private string LoadAnchorFromXml()
    {
        Debug.Log("Loading Anchor From Xml");
        // Define the file path to the XML file
        string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "anchors.xml");

        // Check if the file exists, if not return null or handle accordingly
        if (!File.Exists(filePath))
        {
            Debug.LogError("Anchor XML file not found.");
            return null;
        }

        // Load the XML document from the file
        XDocument xmlDoc = XDocument.Load(filePath);

        // Find the "Anchor" element inside the root element "Anchors"
        XElement anchorElement = xmlDoc.Root.Element("Anchor");

        if (anchorElement != null)
        {
            // Get the UUID value
            string uuid = anchorElement.Element("UUID")?.Value;
            string savedTime = anchorElement.Element("SavedTime")?.Value;

            Debug.Log($"Loaded anchor UUID: {uuid}, saved on: {savedTime}");

            return uuid;
        }
        else
        {
            Debug.LogError("No anchor found in the XML file.");
            return null;
        }
    }

    private string GetFilePath()
    {
        // On Windows, you can use the AppData folder
        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        // Or use a custom path relative to the project directory
        // string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "SaveData");

        // Ensure the directory exists
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        return Path.Combine(folderPath, "anchors.xml");
    }
}
