using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using RealityEditor;
using UnityEngine.Networking;
using Oculus.Platform.Models;
public class AvatarForDreamPortal : MonoBehaviour
{
    //these need to be the trackers that are put in the robot. 
    public Transform RightUpperArmTracker, RightLowerArmTracker, RightHandTracker;
    public Transform LeftUpperArmTracker, LeftLowerArmTracker, LeftHandTracker;
    public Transform HeadTracker, ChestTracker; 
    public Transform RightUpperLegTracker, RightLowerLegTracker;
    public Transform LeftUpperLegTracker, LeftLowerLegTracker;


    
    RealityEditorManager realityEditorManager;
    public GameObject BodyPartPiece;

    
    // Start is called before the first frame update
    void Start()
    {
        realityEditorManager = GetComponent<RealityEditorManager>();
        StartCoroutine(delaySpawnBody());
    }

    IEnumerator delaySpawnBody()
    {
        yield return new WaitForSeconds(5);


        GameObject UserHead = realityEditorManager.SpawnNetworkObject(Vector3.one, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc headBodyPartSyc=UserHead.GetComponent<BodyPartSyc>();
        headBodyPartSyc.SetTarget(HeadTracker);
        
        //sent the transform of the actual head cube. 
        
        GameObject UserChest = realityEditorManager.SpawnNetworkObject(Vector3.one, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc ChestBodyPartSyc=UserChest.GetComponent<BodyPartSyc>();
        ChestBodyPartSyc.SetTarget(ChestTracker);
        
        //arms
        GameObject UserRightUpperArm = realityEditorManager.SpawnNetworkObject(Vector3.one, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc RightUpperArmSync = UserRightUpperArm.GetComponent<BodyPartSyc>();
        RightUpperArmSync.SetTarget(RightUpperArmTracker);
        
        GameObject UserRightLowerArm = realityEditorManager.SpawnNetworkObject(Vector3.one, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc RightLowerArmSync = UserRightLowerArm.GetComponent<BodyPartSyc>();
        RightLowerArmSync.SetTarget(RightLowerArmTracker);
        
        GameObject UserRightHand = realityEditorManager.SpawnNetworkObject(Vector3.one * 3, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc rhandBodyPartSyc=UserRightHand.GetComponent<BodyPartSyc>();
        rhandBodyPartSyc.SetTarget(RightHandTracker);
        
        GameObject UserLeftUpperArm = realityEditorManager.SpawnNetworkObject(Vector3.one * 2, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc LeftUpperArmSync = UserLeftUpperArm.GetComponent<BodyPartSyc>();
        LeftUpperArmSync.SetTarget(LeftUpperArmTracker);
        
        GameObject UserLeftLowerArm = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc LeftLowerArmSync = UserLeftLowerArm.GetComponent<BodyPartSyc>();
        LeftLowerArmSync.SetTarget(LeftLowerArmTracker);
        
        GameObject UserLeftHand = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece);   
        BodyPartSyc lhandBodyPartSyc=UserLeftHand.GetComponent<BodyPartSyc>();
        lhandBodyPartSyc.SetTarget(LeftHandTracker);
        
        
        //legs
        GameObject UserRightUpperLeg = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc RightUpperLegSync = UserRightUpperLeg.GetComponent<BodyPartSyc>();
        RightUpperLegSync.SetTarget(RightUpperLegTracker);
        
        GameObject UserRightLowerLeg = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc RightLowerLegSync = UserRightLowerLeg.GetComponent<BodyPartSyc>();
        RightLowerLegSync.SetTarget(RightLowerLegTracker);
        
        GameObject UserLeftUpperLeg = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc LeftUpperLegSync = UserLeftUpperLeg.GetComponent<BodyPartSyc>();
        LeftUpperLegSync.SetTarget(LeftUpperLegTracker);
        
        GameObject UserLeftLowerLeg = realityEditorManager.SpawnNetworkObject(Vector3.zero, Quaternion.identity, BodyPartPiece); 
        BodyPartSyc LeftLowerLegSync = UserLeftLowerLeg.GetComponent<BodyPartSyc>();
        LeftLowerLegSync.SetTarget(LeftLowerLegTracker);
        
        

    }   






        // public GameObject SpawnNetworkObject(Vector3 position, Quaternion rotation, GameObject PhotonObject)
        // {
        //     if (_runner == null || !_runner.IsRunning)
        //     {
        //         Debug.LogError("NetworkRunner is not running. Cannot spawn network object.");
        //         return null;
        //     }
        //
        //     // Spawn the network object
        //     NetworkObject networkObject = _runner.Spawn(PhotonObject, position, rotation, inputAuthority: _runner.LocalPlayer);
        //     
        //     if (networkObject == null)
        //     {
        //         Debug.LogError("Failed to spawn the network object.");
        //         return null; 
        //     }
        //
        //     return networkObject.gameObject; 
        // }




}
