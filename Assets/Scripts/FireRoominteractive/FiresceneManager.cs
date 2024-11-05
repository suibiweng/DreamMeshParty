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
    public string URID;
}


[System.Serializable]
public class FlammableItemsData
{
    public List<string> flammableItems;  // Simple list to hold the array of strings
}



public class FiresceneManager : MonoBehaviour
{  
    
    public bool isServer;
    
     public RealityEditorManager manager;
    public OSC osc;
    private string url = "http://192.168.1.139:12000/Room.json";


    public FireSpot [] fireSpots;
    // Start is called before the first frame update
    void Start()
    {
        manager = GetComponent<RealityEditorManager> ();    
        osc=GetComponent<OSC>();
        // osc.SetAddressHandler("/setFire",SetFire);
        
    }
    public List<FutnitureData> cropBoxes = new List<FutnitureData>();



    public void ServerStart(){

        StartCoroutine(getallCropBoxes());





    }


    public void StartTheFireScene(){

        StartCoroutine(FetchRoomJson());



      


    }


string[] FlamableObject; 



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

            // Parse the JSON to extract flammableObjects (an array of strings)
            JObject parsedData = JObject.Parse(jsonData);

            // Check if the "flammableObjects" key exists and is not null
            if (parsedData["flammableObjects"] != null)
            {
                JArray flammableObjectsArray = (JArray)parsedData["flammableObjects"];
                if (flammableObjectsArray != null && flammableObjectsArray.Count > 0)
                {
                    // Create a string array to store the flammable object URIDs
                    string[] uridArray = flammableObjectsArray.ToObject<string[]>();  // Convert JArray directly to string[]

                    // Log each URID
                    foreach (var urid in uridArray)
                    {
                        Debug.Log($"Flammable object URID: {urid}");
                    }
                    FlamableObject=uridArray;

                    // Call your setFire function after processing the items
                    setFire();
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

            
            yield return new WaitForSeconds(1);

            GameObject gc= manager.createFireSpot(g.transform.position);
            data.URID=gc.GetComponent<GenerateSpot>().URLID;
            cropBoxes.Add(data);
        }



        fireSpots=FindObjectsOfType<FireSpot>();  
        CropBoxContainer container = new CropBoxContainer { cropBoxes = this.cropBoxes };         

        string json = JsonUtility.ToJson(container, true);



        print("here is:"+json);

        // Send the JSON data to the server
        StartCoroutine(SendJsonToServer(json));
    }




        private IEnumerator SendJsonToServer(string jsonData)
    {
        string url = "http://192.168.1.139:5000/receiveRoom";  // Replace with your local server URL and endpoint
        UnityWebRequest request = new UnityWebRequest(url, "POST");

        // Create a byte array from the JSON string
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
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


            StartTheFireScene();
        }


        
    }

    int currentFire=0;

    

    public void setFire(){


       

        print(FlamableObject[currentFire]);
    

         findTheSpotinthelist(FlamableObject[currentFire]).setFire();
         findTheSpotinthelist(FlamableObject[currentFire]).fireSync.CallSetFireRPC();

         print("setThefire at"+FlamableObject[currentFire]);


        
      

        

    }

    public void SetFireFromOSC(OscMessage oscMessage){

        if(isServer)
        findTheSpotinthelist(oscMessage.values[0].ToString()).setFire();


    }


    public void putOutFire(string urid){



        currentFire++;
        findTheSpotinthelist(urid).putOutFire();
        findTheSpotinthelist(urid).fireSync.CallputFireoutRPC();



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
