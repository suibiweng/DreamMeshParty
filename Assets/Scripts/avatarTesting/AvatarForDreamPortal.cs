using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using RealityEditor;
using UnityEngine.Networking;
using Oculus.Platform.Models;
public class AvatarForDreamPortal : MonoBehaviour
{
    GameObject UserHead;

    public Transform LeftHand, RightHand;
    public Transform Head; 

    RealityEditorManager realityEditorManager;


     private NetworkRunner _runner;
    



    public GameObject AvatarHead;

    

    // Start is called before the first frame update
    void Start()
    {   
        realityEditorManager=GetComponent<RealityEditorManager>();

        _runner = FindObjectOfType<NetworkRunner>(); 

        LeftHand=realityEditorManager.LeftHand;
        RightHand=realityEditorManager.RightHand;
        Head=realityEditorManager.PlayerCamera;


        StartCoroutine(delaySpwanBody());


        
    


            
        
           //GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab); 
    }

    // Update is called once per frame
    void Update()
    {
        if(UserHead!=null)
        {
            print("UserHead is not null");
            UserHead.transform.position=Head.position;
            UserHead.transform.rotation=Head.rotation;
        }   

      
        
    }



    IEnumerator delaySpwanBody()
    {
        yield return new WaitForSeconds(3);


        UserHead = SpawnNetworkObject(Head.position, Quaternion.identity, AvatarHead); 
        BodyPartSyc bodyPartSyc=UserHead.GetComponent<BodyPartSyc>();
        bodyPartSyc.SetTarget(Head);
        

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
