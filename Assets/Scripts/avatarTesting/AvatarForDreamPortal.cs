using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using RealityEditor;
using UnityEngine.Networking;
using Oculus.Platform.Models;
using Unity.VisualScripting;
public class AvatarForDreamPortal : MonoBehaviour
{
    GameObject UserHead;
    GameObject UserLeftHand;
    GameObject UserRightHand;

    public Transform LeftHand, RightHand;
    public Transform Head; 

    RealityEditorManager realityEditorManager;


    public NetworkRunner _runner;
    
    public GameObject AvatarHead, AvatarHand;


    private bool PlayerSetup = false;

    // Start is called before the first frame update
    void Start()
    {   
        realityEditorManager=GetComponent<RealityEditorManager>();

        _runner = FindObjectOfType<NetworkRunner>(); 

        LeftHand=realityEditorManager.LeftHand;
        RightHand=realityEditorManager.RightHand;
        Head=realityEditorManager.PlayerCamera;


        // StartCoroutine(delaySpwanBody());


        
    


            
        
           //GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab); 
    }

    // Update is called once per frame
    void Update()
    {
        if(_runner.IsRunning && !PlayerSetup)
        {
            SpawnBody();
            print("Player is setup");
            PlayerSetup=true;
        }
        if(UserHead!=null)
        {
            print("UserHead is not null");
            UserHead.transform.position=Head.position;
            UserHead.transform.rotation=Head.rotation;
            UserLeftHand.transform.position=LeftHand.position;
            UserLeftHand.transform.rotation=LeftHand.rotation;
            UserRightHand.transform.position=RightHand.position;
            UserRightHand.transform.rotation=RightHand.rotation;

        }   

      
        
    }



    void SpawnBody()
    {
        // yield return new WaitForSeconds(8);


        UserHead = SpawnNetworkObject(Head.position, Quaternion.identity, AvatarHead); 
        UserLeftHand = SpawnNetworkObject(LeftHand.position, Quaternion.identity, AvatarHand);
        UserRightHand = SpawnNetworkObject(RightHand.position, Quaternion.identity, AvatarHand);        
        BodyPartSync HeadSync=UserHead.GetComponent<BodyPartSync>();
        BodyPartSync LeftHandSync=UserLeftHand.GetComponent<BodyPartSync>();
        BodyPartSync RightHandSync=UserRightHand.GetComponent<BodyPartSync>();
           
        
        HeadSync.SetTarget(Head);
        LeftHandSync.SetTarget(LeftHand);
        RightHandSync.SetTarget(RightHand);
        

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
