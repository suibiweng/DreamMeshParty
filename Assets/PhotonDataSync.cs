using System;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class PhotonDataSync : NetworkBehaviour
{
    private GenerateSpot _generateSpot;
    
    [Networked, OnChangedRender(nameof(OnUrlIDChanged))]
    public string NetworkedUrlID { get; set; }
    
    [Networked, OnChangedRender(nameof(OnPromptChanged))]
    public string NetworkedPrompt { get; set; }
    
    private void Start()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        _generateSpot.URLID = NetworkedUrlID; 

    }
    // Method to detect changes to the networked string
    void OnUrlIDChanged()
    {
        Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        _generateSpot.URLID = NetworkedUrlID; 
    }
    void OnPromptChanged()
    {
        Debug.Log("Networked prompt changed to: " + NetworkedPrompt);
        _generateSpot.Prompt = NetworkedPrompt; 
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