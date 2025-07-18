using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;


public class SessionDataSync : NetworkBehaviour
{
    private SceneSessionManager SceneSessionManager;
    
    [Networked, OnChangedRender(nameof(OnSessionUrlIDChanged))]
    public string NetworkedUrlID { get; set; }
    
    [Networked, OnChangedRender(nameof(OnSessionUrlIDChanged))]
    public string ScenePremise { get; set; }
    
    
    private void Start()
    {
        SceneSessionManager = GetComponent<SceneSessionManager>();
        SceneSessionManager.sessionURLID = NetworkedUrlID; 
        

    }
    // Method to detect changes to the networked string
    void OnSessionUrlIDChanged()
    {
        SceneSessionManager = GetComponent<SceneSessionManager>();
        Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        SceneSessionManager.sessionURLID = NetworkedUrlID; 
    }
    void OnScenePremiseChanged()
    {
        SceneSessionManager = GetComponent<SceneSessionManager>();
        Debug.Log("ScenePremise changed to: " + ScenePremise);
        SceneSessionManager.TheSessionPremiseText.text = ScenePremise; 
    }
    
    public void UpdateSessionURLID(string newUrlID)
    {
        if (HasStateAuthority)
        {
            // Change the string value here, which will then be synchronized across all clients
            NetworkedUrlID = newUrlID;
        }
    }
    
    public void UpdateScenePremise(string newPremise)
    {
        if (HasStateAuthority)
        {
            // Change the string value here, which will then be synchronized across all clients
            ScenePremise = newPremise;
        }
    }

}
