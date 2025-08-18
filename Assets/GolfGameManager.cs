using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class GolfGameManager : MonoBehaviour
{
    private NetworkRunner _runner;

    // Start is called before the first frame update
    void Start()
    {
        _runner = FindObjectOfType<NetworkRunner>();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public GameObject SpawnNetworkObject(Vector3 position, Quaternion rotation, GameObject PhotonObject)
    {
        if (_runner == null || !_runner.IsRunning)
        {
            Debug.LogError("NetworkRunner is not running. Cannot spawn network object.");
            return null;
        }
    
        // Spawn the network object
        NetworkObject networkObject = _runner.Spawn(PhotonObject, position, rotation, inputAuthority: _runner.LocalPlayer);
            
        if (networkObject == null)
        {
            Debug.LogError("Failed to spawn the network object.");
            return null; 
        }
            
 
        return networkObject.gameObject; 
    }
}
