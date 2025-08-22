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

    // --- NEW SYNCED VARIABLES ---
    [Networked, OnChangedRender(nameof(OnIsRealObjectChanged))]
    public bool NetIsRealObject { get; set; }

    [Networked, OnChangedRender(nameof(OnOutlineChanged))]
    public bool NetOutlineEnabled { get; set; }

    [Networked, OnChangedRender(nameof(OnMenuActiveChanged))]
    public bool NetSelectMenuActive { get; set; }
    

    private void Start()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        _generateSpot.URLID = NetworkedUrlID; 
        _generateSpot.isRealObject = NetIsRealObject;

        if (_generateSpot.Outlinebox != null)
            _generateSpot.Outlinebox.enabled = NetOutlineEnabled;

        if (_generateSpot.selectMenu != null)
            _generateSpot.selectMenu.SetActive(NetSelectMenuActive);
    }

    // --- URLID / Prompt ---
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

    // --- Toggles ---
    void OnMenu1Changed() => EditBehaviorToggle.isOn = EditBehavior;
    void OnMenu2Changed() => ShowCode.isOn = ShowLua;
    void OnMenu3Changed() => Physics.isOn = EnablePhysics;

    // --- NEW HANDLERS ---
    void OnIsRealObjectChanged()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        _generateSpot.isRealObject = NetIsRealObject;
    }

    void OnOutlineChanged()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        if (_generateSpot.Outlinebox != null)
            _generateSpot.Outlinebox.enabled = NetOutlineEnabled;
    }

    void OnMenuActiveChanged()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        if (_generateSpot.selectMenu != null)
            _generateSpot.selectMenu.SetActive(NetSelectMenuActive);
    }

    // --- UPDATE METHODS ---
    public void UpdateURLID(string newUrlID)
    {
        if (HasStateAuthority)
            NetworkedUrlID = newUrlID;
    }

    public void UpdatePrompt(string newPrompt)
    {
        if (HasStateAuthority)
            NetworkedPrompt = newPrompt;
    }

    public void UpdateMenu1(bool val)
    {
        if (HasStateAuthority)
            EditBehavior = val;
    }

    public void UpdateMenu2(bool val)
    {
        if (HasStateAuthority)
            ShowLua = val;
    }

    public void UpdateMenu3(bool val)
    {
        if (HasStateAuthority)
            EnablePhysics = val;
    }

    // --- NEW UPDATE METHODS ---
    public void UpdateIsRealObject(bool val)
    {
        if (HasStateAuthority)
            NetIsRealObject = val;
    }

    public void UpdateOutline(bool val)
    {
        if (HasStateAuthority)
            NetOutlineEnabled = val;
    }

    public void UpdateSelectMenu(bool val)
    {
        if (HasStateAuthority)
            NetSelectMenuActive = val;
    }
}
