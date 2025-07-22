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




    [Networked, OnChangedRender(nameof(OnPremiseChanged))]
    public string NetworkedPremise { get; set; }

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
    void OnPremiseChanged()
    {
        _sceneSessionManager = GetComponent<SceneSessionManager>();
        Debug.Log("Networked premise changed to: " + NetworkedPremise);
        _sceneSessionManager.TheSessionPremise = NetworkedPremise;
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

    public void UpdatePremise(string newPremise)
    {
        if (HasStateAuthority)
        {
            // Change the string value here, which will then be synchronized across all clients
            NetworkedPremise = newPremise;
        }


    }

}
    

