using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;

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



public class FiresceneManager : MonoBehaviour
{  
    
    public bool isServer;
    
     public RealityEditorManager manager;
    public OSC osc;
    private string url = "http://192.168.1.139:8000/Room.json";


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



        setFire();






    }





    IEnumerator FetchRoomJson()
    {
        // Create a UnityWebRequest to get the file from the server
        UnityWebRequest request = UnityWebRequest.Get(url);

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        // Check if there was an error
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error fetching file: {request.error}");
        }
        else
        {
            // Get the downloaded JSON as a string
            string jsonData = request.downloadHandler.text;

            // Log the received JSON data
            Debug.Log($"Received JSON: {jsonData}");

            // Optionally, process the JSON data further if necessary
            ProcessRoomJson(jsonData);
        }
    }

    RoomData roomData ;

        // Function to process the JSON data
    void ProcessRoomJson(string json)
    {
        roomData= JsonUtility.FromJson<RoomData>(json);
        // Example: You can parse the JSON and use it in your Unity application
        
        foreach (var obj in roomData.flammableObjects)
        {
            Debug.Log($"Flammable object URID: {obj.URID}");
        }
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


        
    }

    int currentFire=0;

    

    public void setFire(){



        if(currentFire<roomData.flammableObjects.Length ){




        

         findTheSpotinthelist(roomData.flammableObjects[currentFire].URID).setFire();
        }

        else{


            //finish

        }

    }

    public void SetFireFromOSC(OscMessage oscMessage){

        if(isServer)
        findTheSpotinthelist(oscMessage.values[0].ToString()).setFire();


    }


    public void putOutFire(string urid){



        currentFire++;
        setFire();

/*
        OscMessage oscMessage=new OscMessage();
        oscMessage.address="/PutOutFire";
        oscMessage.values.Add(urid);
        
        if(isServer)
        osc.Send(oscMessage);
*/


    }




    public FireSpot findTheSpotinthelist(string id){
        foreach (FireSpot sp in fireSpots){

            if (sp.gameObject.GetComponent<GenerateSpot>().URLID == id){
                return sp;
            }


        }
        return null;

    }


    }
