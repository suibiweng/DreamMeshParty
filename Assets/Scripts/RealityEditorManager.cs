using System.Collections;
using System.Collections.Generic;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using RealityEditor;
using TMPro;
using TriLibCore.Dae.Schema;
using Unity.VisualScripting;
using UnityEngine.Networking;

using UnityEngine.SceneManagement;
using Klak.Ndi.Interop;


public class RealityEditorManager : MonoBehaviour
{
   
    public bool isFireScene;

    public bool isPhysics;
    public GameObject GenerateSpotPrefab;
    private NetworkRunner _runner;
    public bool isOnline; 
    public Transform LeftHand, RightHand;
    public Transform PlayerCamera; 
    public string uploadPort,downloadPort;
    public string ServerURL;
    private string comandURL;

    public SceneSessionManager sceneSessionManager;
    
    public Dictionary<string, GameObject> GenCubesDic;

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
        comandURL = ServerURL + ":" + uploadPort + "/";
        ServerURL += ":" + downloadPort + "/";
        //comandURL+=":"+uploadPort+"/";
        //GenCubes= new List<GameObject>();
        GenCubesDic = new Dictionary<string, GameObject>();
        sceneSessionManager = FindObjectOfType<SceneSessionManager>();
        
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
        GenCubesDic[IDurl].GetComponent<GenerateSpot>().isselsected=true;
   
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


        // if(Input.GetKeyDown(KeyCode.F1)){
        //    createSpotOnMenu();
        // }

        
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

    public void createReconstructionSpot(Vector3 pos, Vector3 scale)
    {

        GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity);
        gcube.GetComponent<GenerateSpot>().id = IDs;
        string urlid = TimestampGenerator.GetTimestamp();
        gcube.GetComponent<GenerateSpot>().URLID = urlid;
        gcube.transform.localScale = scale;
        Debug.Log("The new Cube's URLID is: " + urlid);
        // gcube.GetComponent<DataSync2>().SetURLID(urlid); //setting the network urlid once right after we make the spot. But this dont work
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid, gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl = urlid;
        IDs++;
        gcube.name =""+ urlid;
        
        

    }



    public void createSpotOnMenu()
    {
        // GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity ); 
        GameObject gcube = SpawnNetworkObject(LeftHand.position, Quaternion.identity, GenerateSpotPrefab);
        // gcube.GetComponent<GenerateSpot>().id=IDs;
        string urlid = IDGenerator.GenerateID();
        gcube.GetComponent<GenerateSpot>().URLID = urlid;
        Debug.Log("The new Cube's URLID is: " + urlid);
        gcube.GetComponent<PhotonDataSync>().UpdateURLID(urlid);  //setting the network urlid once right after we make the spot.
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid, gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl = urlid;
        IDs++;
        gcube.name = "" + urlid;
        sceneSessionManager.SubmmiSession();
        


    }
    


public GameObject createRealobjectSpot(Vector3 pos, Vector3 scale)
{
    // Spawn the networked prefab
    GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab);

    // Generate new URLID
    string urlid = IDGenerator.GenerateID();

    var spot = gcube.GetComponent<GenerateSpot>();
    spot.id = IDs;
    spot.isRealObject = true; // mark as real
    spot.URLID = urlid;
    //gcube.transform.localScale = scale;

    // --- Apply "real object" rules locally ---
    if (spot.selectMenu != null)
        spot.selectMenu.SetActive(false);

    if (spot.boxCollider != null)
        spot.boxCollider.enabled = false;

    // --- Sync across the network ---
    // var sync = gcube.GetComponent<PhotonDataSync>();
    // if (sync != null && sync.HasStateAuthority)
    // {
    //     sync.UpdateURLID(urlid);
    //     sync.UpdatePrompt(spot.Prompt);
    //     sync.UpdateIsRealObject(true);
    //     sync.UpdateSelectMenu(false); // menu closed
    //     sync.UpdateOutline(false);    // outlines off by default
    // }

    GenCubesDic.Add(urlid, gcube);

    selectedIDUrl = urlid;
    IDs++;

    Debug.Log("The new Cube's URLID is: " + urlid);

    return gcube;
}




public GameObject createSpot(Vector3 pos)
{
    GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab);

    var spot = gcube.GetComponent<GenerateSpot>();
    spot.id = IDs;
    string urlid = IDGenerator.GenerateID();
    spot.URLID = urlid;
    spot.isRealObject = false; // virtual spot

    Debug.Log("The new Cube's URLID is: " + urlid);

    // Leave menu/collider as prefab default
    if (spot.selectMenu != null)
        spot.selectMenu.SetActive(spot.selectMenu.activeSelf);

    if (spot.boxCollider != null)
        spot.boxCollider.enabled = spot.boxCollider.enabled;

    // --- Sync across network ---
var sync = gcube.GetComponent<PhotonDataSync>();
if (sync != null )
{
    sync.UpdateURLID(urlid);
    sync.UpdatePrompt(spot.Prompt);
    sync.UpdateIsRealObject(false);
    sync.UpdateSelectMenu(spot.selectMenu != null && spot.selectMenu.activeSelf);
    sync.UpdateOutline(false);
    sync.UpdateObjectName(urlid); // 👈 sync name here
}

    GenCubesDic.Add(urlid, gcube);
    selectedIDUrl = urlid;
    IDs++;

    gcube.name = urlid;
    sceneSessionManager.SubmmiSession();

    return gcube;
}

    public void updateSession()
    {
        sceneSessionManager.SubmmiSession();
    }


    public void UpdatealltheCode()
    {
         // this is not needed anymore, we are using the fetch code in the generate spot.
        foreach (var kvp in GenCubesDic)
        {
            print("Updating code for: " + kvp.Key);
            var generateSpot = kvp.Value.GetComponent<GenerateSpot>();
            var luaMonoBehavior = kvp.Value.GetComponent<LuaMonoBehavior>();

            if (generateSpot != null && luaMonoBehavior.hasluaScript)
            {
                generateSpot.FetchCode();
                print("Updating code for: " + kvp.Key);
            }
        }
    }



    
    public GameObject createFireSpot(Vector3 pos)
    {
        // GameObject gcube = Instantiate(GenerateSpotPrefab, pos, Quaternion.identity ); 
        GameObject gcube = SpawnNetworkObject(pos, Quaternion.identity, GenerateSpotPrefab);
        gcube.GetComponent<GenerateSpot>().id = IDs;
        string urlid = IDGenerator.GenerateID();
        gcube.GetComponent<GenerateSpot>().URLID = urlid;
        Debug.Log("The new Cube's URLID is: " + urlid);
        gcube.GetComponent<PhotonDataSync>().UpdateURLID(urlid);  //setting the network urlid once right after we make the spot.
        Debug.Log("Setting the network urlid to be: " + urlid);
        GenCubesDic.Add(urlid, gcube); //think about this: Are we adding the cube to the other players dictionaries? 
        selectedIDUrl = urlid;
        IDs++;
        sceneSessionManager.SubmmiSession();

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
        sceneSessionManager.SubmmiSession();
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




    public void sendCommandwithPrompt(string command,string urlid,string prompt)
    {

        StartCoroutine(SendtheCommand(comandURL + "command", command, urlid, prompt));
    }

public void sendCommand(string command)
    {

        StartCoroutine(SendtheCommand(comandURL + "command", command, selectedIDUrl, GenCubesDic[selectedIDUrl].GetComponent<GenerateSpot>().Prompt));
    }


public IEnumerator SendtheCommand( string url,string command ,string urlid,string Prompt)
{
    
    if (command != "")
    {
        WWWForm form = new WWWForm();

        form.AddField("Command", command);
        form.AddField("URLID",urlid);
        form.AddField("Prompt",Prompt);

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Command sent: " + command);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}
    
    
    
    
    
}
