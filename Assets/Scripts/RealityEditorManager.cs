using System.Collections;
using System.Collections.Generic;
using Fusion;
using Normal.Realtime;
using Oculus.Interaction;
using UnityEngine;
using RealityEditor;
// using Normal.Realtime; 
using TMPro;
using TriLibCore.Dae.Schema;
using Unity.VisualScripting;


public class RealityEditorManager : MonoBehaviour
{
    public bool isFireScene;
    public GameObject GenerateSpotPrefab;
    private NetworkRunner _runner;
    private Realtime _realtime;
    public bool isOnline; 
    public Transform LeftHand, RightHand;
    public Transform PlayerCamera; 
    public string uploadPort,downloadPort;
    public string ServerURL;
    
    public Dictionary<string,GameObject> GenCubesDic;

    public SceneSaverTest SceneSaverTest; 
   // public List<GameObject> GenCubes;
   
    public string selectedIDUrl;
    public int IDs = 0;
    TextMeshPro debugText;

    public Transform Cursor;
    
    public GameObject sculptingMenu,scuptingBrush;
    public OSC osc;
    private int colorcubeMover; 
    void Start()
    {

        osc = FindObjectOfType<OSC>();
        _runner = FindObjectOfType<NetworkRunner>(); 
        ServerURL+=":"+downloadPort+"/";
        //GenCubes= new List<GameObject>();
        GenCubesDic=new Dictionary<string,GameObject>();
        // IDs=GenCubes.Count;
         // osc.SetAllMessageHandler(ReciveFromOSC);
         
         //tested adding all the cubes already in the scene.
         // GameObject InitialGenCube = FindObjectOfType<GenerateSpot>().GameObject();
         // Debug.Log("Found the initial cube");
         // GenCubesDic.Add(InitialGenCube.GetComponent<GenerateSpot>().URLID, InitialGenCube); //think about this: Are we adding the cube to the other players dictionaries? 
         // Debug.Log("The Initial Cubes URLID is: " + InitialGenCube.GetComponent<GenerateSpot>().URLID);

    } 


    public void updateSelected(int id,string IDurl)
    {
        Debug.Log("Using a dictionary in The manager, The key you are looking for is: " + IDurl); 
        GenCubesDic[selectedIDUrl].GetComponent<GenerateSpot>().isselsected=false;
        // GenCubesDic[selectedIDUrl].GetComponent<RealtimeTransform>().ClearOwnership(); 
        GenCubesDic[IDurl].GetComponent<GenerateSpot>().isselsected=true;
        // GenCubesDic[IDurl].GetComponent<RealtimeView>().RequestOwnershipOfSelfAndChildren();
        // GenCubesDic[IDurl].GetComponent<RealtimeTransform>().RequestOwnership();
        // selectedID=id;
        selectedIDUrl=IDurl; 
    }

    public void turnSculptingMenu(bool on){

        sculptingMenu.SetActive(on);
        scuptingBrush.SetActive(on);
    }

    public GameObject getSelectSpot(){
        return GenCubesDic[selectedIDUrl];
    }


    // Update is called once per frame
    void Update()
    {

        if(isFireScene) return;
        
        //OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
        if(OVRInput.GetUp(OVRInput.RawButton.A)){
            createSpot(RightHand.position);
        }
        if(OVRInput.GetUp(OVRInput.RawButton.X)){
          
            createSpot(LeftHand.position);
            
        }
        if(Input.GetKeyDown(KeyCode.Space)){
            createSpot(new Vector3(0,0,0));
            
        }
        if(Input.GetKeyDown(KeyCode.S)){
            SceneSaverTest.SaveSceneToServer();
        }
        if(Input.GetKeyDown(KeyCode.L)){
            SceneSaverTest.LoadSceneFromServer();
        }
    }

    public void createReconstructionSpot(Vector3 pos,Vector3 scale){
        
        GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity);
        gcube.GetComponent<GenerateSpot>().id=IDs;
        string urlid=TimestampGenerator.GetTimestamp(); 
        gcube.GetComponent<GenerateSpot>().URLID=urlid;
        gcube.transform.localScale = scale;
        Debug.Log("The new Cube's URLID is: " + urlid);
        // gcube.GetComponent<DataSync2>().SetURLID(urlid); //setting the network urlid once right after we make the spot. But this dont work
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid,gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl=urlid;  
        IDs++;
        

    }

    
    public void createSpot(Vector3 pos)
    {
        // GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity ); 
        GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab); 
        gcube.GetComponent<GenerateSpot>().id=IDs;
        string urlid=TimestampGenerator.GetTimestamp(); 
        gcube.GetComponent<GenerateSpot>().URLID=urlid;
        Debug.Log("The new Cube's URLID is: " + urlid);
        gcube.GetComponent<PhotonDataSync>().UpdateURLID(urlid);  //setting the network urlid once right after we make the spot.
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid, gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl=urlid;  
        IDs++;
    }



    
    public GameObject createFireSpot(Vector3 pos)
    {
        // GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity ); 
        GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab); 
        gcube.GetComponent<GenerateSpot>().id=IDs;
        string urlid=TimestampGenerator.GetTimestamp(); 
        gcube.GetComponent<GenerateSpot>().URLID=urlid;
        Debug.Log("The new Cube's URLID is: " + urlid);
        gcube.GetComponent<PhotonDataSync>().UpdateURLID(urlid);  //setting the network urlid once right after we make the spot.
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid, gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl=urlid;  
        IDs++;

        return gcube;
    }







    public GameObject createSavedSpot(Vector3 pos, Quaternion rot, Vector3 scale, string urlid) // same as create spot function but includes scaling and rotating
    {
        Debug.Log("Creating Saved spot at " + pos);
        GameObject gcube = SpawnNetworkObject(pos, rot, GenerateSpotPrefab); 
        gcube.transform.localScale = scale;
        gcube.GetComponent<GenerateSpot>().id=IDs;
        gcube.GetComponent<GenerateSpot>().URLID=urlid;
        gcube.GetComponent<PhotonDataSync>().UpdateURLID(urlid); //setting the network urlid once right after we make the spot. But this dont work
        if (!GenCubesDic.ContainsKey(urlid))
        {
            GenCubesDic.Add(urlid,gcube);
        }
        selectedIDUrl=urlid;
        IDs++;
        return gcube; 

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

   public void RemoveSpot(string urlid){
       Destroy(GenCubesDic[urlid].gameObject);
       GenCubesDic.Remove(urlid);
     
    }
   
    public void InstructModify(int id,string promt,string urlid){
        OscMessage message = new OscMessage()
        {
            address = "/InstructModify"
        };
        message.values.Add(id);
        message.values.Add(promt);
        message.values.Add(urlid);
        osc.Send(message);

    }
    
    public void ScanObj(int id){

        OscMessage message = new OscMessage()
        {
            address = "/ScanModel"
        };
        message.values.Add(id);
        //message.values.Add(promt);

    }
    
    public void setPrompt(string txt)
    {
        //GenCubes[selectedID].GetComponent<GenerateSpot>().Prompt=txt;
        GenCubesDic[selectedIDUrl].GetComponent<GenerateSpot>().Prompt=txt;
    }
    
    public void ChangeID(string PreviousKey,string Modifiedkey,GameObject v){
        GenCubesDic.Add(Modifiedkey,v);
       // GenCubesDic.Remove(PreviousKey);
       // GenCubesDic[Modifiedkey] = value;
       
    }
    
    public void promtGenerateModel(int id,string promt,string URLID){
        Debug.Log("Checkpoint 2");

        OscMessage message = new OscMessage()
        {
            address = "/PromtGenerateModel"
        };
        Debug.Log("Checkpoint 3");

        message.values.Add(id);
        message.values.Add(promt);
        message.values.Add(URLID);
        message.values.Add("genrated");
        Debug.Log("Checkpoint 4");
        osc.Send(message);
        Debug.Log("Checkpoint 5");


    }
    public void sendStop(){
        
        OscMessage message = new OscMessage()
        {
            address = "/stopProcess"
        };
    

        osc.Send(message);

    }
    
    
    
    
    
}
