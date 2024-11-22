using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;  

using UnityEngine.Networking;

[System.Serializable]
public class CropBoxContainer
{
    public List<FutnitureData> cropBoxes;
}

// Define a class structure matching the JSON file format
[System.Serializable]
public class RoomData
{
    public FlammableObject[] flammableObjects;
}

[System.Serializable]
public class FlammableObject
{
    public string URID { get; set; }       // Unique identifier for the object
    public string Reason { get; set; }    // Reason why the object is flammable
    public string FireType { get; set; }  // Type of fire (e.g., Type A, Type B)
    public string Description { get; set; } // Detailed description of the fire type
}





public class FiresceneManager : MonoBehaviour
{  
    
    public bool isServer;
    
     public RealityEditorManager manager;
    public OSC osc;
   public string url = "http://192.168.0.139:12000/Room.json";

    string RoomID;


    public FireSpot [] fireSpots;
    // Start is called before the first frame update
    void Start()
    {
        manager = GetComponent<RealityEditorManager> ();    
        osc=GetComponent<OSC>();
        // osc.SetAddressHandler("/setFire",SetFire);

        RoomID=IDGenerator.GenerateID();

        url="http://192.168.0.139:12000/"+RoomID+"_ROOM.json" ;



        
    }

    public FlammableObject[] flammableObjects; // Array to store parsed flammable objects

    IEnumerator FetchRoomJson()
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching file: {request.error}");
        }
        else
        {
            string jsonData = request.downloadHandler.text;
            Debug.Log($"Received JSON: {jsonData}");

            // Parse the JSON to extract flammableObjects
            JObject parsedData = JObject.Parse(jsonData);

            // Check if the "flammableObjects" key exists and is not null
            if (parsedData["flammableObjects"] != null)
            {
                JArray flammableObjectsArray = (JArray)parsedData["flammableObjects"];
                if (flammableObjectsArray != null && flammableObjectsArray.Count > 0)
                {
                    // Convert JArray to FlammableObject[]
                    flammableObjects = flammableObjectsArray.ToObject<FlammableObject[]>();

                    // Log each FlammableObject
                    foreach (var obj in flammableObjects)
                    {
                        Debug.Log($"Flammable object - URID: {obj.URID}, Reason: {obj.Reason}");
                            var fire =findTheSpotinthelist(obj.URID);

                            fire.setDescription(obj.Reason);
                            
                            if(obj.FireType == "Type A") fire.fireType=ExtinguisherType.CO2;
                            if(obj.FireType == "Type B") fire.fireType=ExtinguisherType.Foam;
                            if(obj.FireType == "Type C") fire.fireType=ExtinguisherType.DryPowder;
                            if(obj.FireType == "Type D") fire.fireType=ExtinguisherType.Water;






                    }

                    // Optionally call a method after processing
                    // setFire();
                }
                else
                {
                    Debug.LogError("The flammableObjects array is empty or null.");
                }
            }
            else
            {
                Debug.LogError("flammableObjects key not found in the JSON.");
            }
        }
    }





    public List<FutnitureData> cropBoxes = new List<FutnitureData>();



    public void ServerStart(){

        StartCoroutine(getallCropBoxes());





    }

    public void startAnlyze(){
         StartCoroutine(getallCropBoxes());

         StartCoroutine(FetchRoomJson());





    }




    public void StarttoSimulation(){

       

        setFire();

      


    }


string[] FlamableObject; 



  


    RoomData roomData ;

        // Function to process the JSON data
    void ProcessRoomJson(string json)
    {
        roomData= JsonUtility.FromJson<RoomData>(json);
        // Example: You can parse the JSON and use it in your Unity application
        
        // foreach (var obj in roomData.flammableObjects)
        // {
        //     Debug.Log($"Flammable object URID: {obj.URID}");
        // }
    }



    IEnumerator getallCropBoxes()
    {
        isServer=true;
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("CropBox"))
        {
            FutnitureData data = new FutnitureData
            {
                name = g.transform.parent.name,
              // Parent is set to the object's own name
                position = g.transform.position,
                scale = g.transform.localScale,
                rotationEulerAngles = g.transform.rotation.eulerAngles,
            

            };

            
            yield return new WaitForSeconds(0.2f);

            GameObject gc= manager.createFireSpot(g.transform.position);

           
        //    yield return new WaitForSeconds(1f);
            data.URID=gc.GetComponent<GenerateSpot>().URLID;
             gc.GetComponent<FireSpot>().setObjectname(g.transform.parent.name);
            print(data.URID);
            cropBoxes.Add(data);
        }



        fireSpots=FindObjectsOfType<FireSpot>(); 
        
         
        CropBoxContainer container = new CropBoxContainer { cropBoxes = this.cropBoxes };         

        string json = JsonUtility.ToJson(container, true);



        print("here is:"+json);

        // Send the JSON data to the server
        StartCoroutine(SendJsonToServer(json,RoomID+"_ROOM.json"));
    }




private IEnumerator SendJsonToServer(string jsonData, string filename)
{
    string url = "http://192.168.0.139:5000/receiveRoom";  // Replace with your local server URL and endpoint
    
    // Modify the JSON data to include the filename
    string modifiedJsonData = $"{{\"filename\":\"{filename}\",\"data\":{jsonData}}}";

    UnityWebRequest request = new UnityWebRequest(url, "POST");

    // Create a byte array from the modified JSON string
    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(modifiedJsonData);
    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
    request.downloadHandler = new DownloadHandlerBuffer();

    // Set the request content type to application/json
    request.SetRequestHeader("Content-Type", "application/json");

    // Send the request and wait for the response
    yield return request.SendWebRequest();

    // Check for errors in the request
    if (request.result != UnityWebRequest.Result.Success)
    {
        Debug.LogError("Error sending JSON to server: " + request.error);
    }
    else
    {
        Debug.Log("Successfully sent JSON to server");
        Debug.Log("Response: " + request.downloadHandler.text);
    }
}


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Z)){


            ServerStart();
        }


        if(Input.GetKeyDown(KeyCode.C)){


            StarttoSimulation();
        }


        
    }

    int currentFire=0;

    

    public void setFire(){


       

        print(flammableObjects[currentFire].URID);
    

         findTheSpotinthelist(flammableObjects[currentFire].URID).setFire();
         //findTheSpotinthelist(FlamableObject[currentFire]).fireSync.CallSetFireRPC();

         print("setThefire at"+flammableObjects[currentFire].URID);


        
      

        

    }

    public void SetFireFromOSC(OscMessage oscMessage){

        if(isServer)
        findTheSpotinthelist(oscMessage.values[0].ToString()).setFire();


    }


    public void putOutFire(string urid){



        currentFire++;
        findTheSpotinthelist(urid).putOutFire();
       // findTheSpotinthelist(urid).fireSync.CallputFireoutRPC();



        setFire();


    }




    public FireSpot findTheSpotinthelist(string id){
        foreach (FireSpot sp in fireSpots){

            if (sp.gameObject.GetComponent<GenerateSpot>().URLID == id){
                return sp;
            }


        }
        print("Can't find the object");
        return null;

    }


    }
