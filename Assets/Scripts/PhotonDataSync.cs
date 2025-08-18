using System;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class PhotonDataSync : NetworkBehaviour
{
    private GenerateSpot _generateSpot;
    public Toggle EditBehaviorToggle;
    public Toggle ShowCode;
    public Toggle Physics; 
    
    [Networked, OnChangedRender(nameof(OnUrlIDChanged))]
    public string NetworkedUrlID { get; set; }
    
    [Networked, OnChangedRender(nameof(OnPromptChanged))]
    public string NetworkedPrompt { get; set; }
    
    [Networked, OnChangedRender(nameof(OnMenu1Changed))]
    public bool EditBehavior { get; set; }
    
    [Networked, OnChangedRender(nameof(OnMenu2Changed))]
    public bool ShowLua { get; set; }
    
    [Networked, OnChangedRender(nameof(OnMenu3Changed))]
    public bool EnablePhysics { get; set; }
    
    
    private void Start()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        _generateSpot.URLID = NetworkedUrlID; 

    }
    // Method to detect changes to the networked string
    void OnUrlIDChanged()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        _generateSpot.URLID = NetworkedUrlID; 
    }
    void OnPromptChanged()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        Debug.Log("Networked prompt changed to: " + NetworkedPrompt);
        _generateSpot.Prompt = NetworkedPrompt; 
    }
    void OnMenu1Changed()
    {
        //grab the toggle and set it to the value
        EditBehaviorToggle.isOn = EditBehavior;
    }
    void OnMenu2Changed()
    {
        //grab the toggle and set it to the value
        ShowCode.isOn = ShowLua;
    }
    void OnMenu3Changed()
    {
        //grab the toggle and set it to the value
        Physics.isOn = EnablePhysics;
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
    
    public void UpdateMenu1(bool val)
    {
        if (HasStateAuthority)
        {
            EditBehavior = val;
        }
    }
    public void UpdateMenu2(bool val)
    {
        if (HasStateAuthority)
        {
            ShowLua = val;
        }
    }
    public void UpdateMenu3(bool val)
    {
        if (HasStateAuthority)
        {
            EnablePhysics = val;
        }
    }
    
    
}