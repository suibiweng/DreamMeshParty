using System;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class SceneDataSync : NetworkBehaviour
{
    private SceneSessionManager _sceneSessionManager;
    
    [Networked, OnChangedRender(nameof(OnUrlIDChanged))]
    public string NetworkedUrlID { get; set; }
    
    [Networked, OnChangedRender(nameof(OnPromptChanged))]
    public string NetworkedPrompt { get; set; }
    
    private void Start()
    {
        _sceneSessionManager = GetComponent<SceneSessionManager>();
        _sceneSessionManager.sessionURLID = NetworkedUrlID;

    }
    // Method to detect changes to the networked string
    void OnUrlIDChanged()
    {
        _sceneSessionManager = GetComponent<SceneSessionManager>();
        Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        _sceneSessionManager.sessionURLID = NetworkedUrlID; 
    }
    void OnPromptChanged()
    {
        _sceneSessionManager = GetComponent<SceneSessionManager>();
        Debug.Log("Networked prompt changed to: " + NetworkedPrompt);
        _sceneSessionManager.MorePrompt = NetworkedPrompt;
    }
    
    public void UpdateURLID(string newUrlID)
    {
        if (HasStateAuthority)
        {
            // Change the string value here, which will then be synchronized across all clients
            NetworkedUrlID = newUrlID;
        }
    }
    public void UpdatePrompt(string newUrlID)
    {
        if (HasStateAuthority)
        {
            // Change the string value here, which will then be synchronized across all clients
            NetworkedPrompt = newUrlID;
        }
    }
}
    

