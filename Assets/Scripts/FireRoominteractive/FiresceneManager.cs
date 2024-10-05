using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;
//using OpenCover.Framework.Model;
using UnityEngine.Networking;




public class FiresceneManager : MonoBehaviour
{  
    
    public bool isServer;
    
     public RealityEditorManager manager;
    public OSC osc;



    public FireSpot [] fireSpots;
    // Start is called before the first frame update
    void Start()
    {
        manager = GetComponent<RealityEditorManager> ();    
        osc=GetComponent<OSC>();
        osc.SetAddressHandler("",SetFire);
        
    }
    public List<FutnitureData> cropBoxes = new List<FutnitureData>();

    public void getallCropBoxes()
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

            cropBoxes.Add(data);

            GameObject gc= manager.createFireSpot(g.transform.position);
            data.URID=gc.GetComponent<GenerateSpot>().URLID;
        }



        fireSpots=FindObjectsOfType<FireSpot>();           

        string json = JsonUtility.ToJson(new { cropBoxes = this.cropBoxes }, true);

        // Send the JSON data to the server
        StartCoroutine(SendJsonToServer(json));
    }




        private IEnumerator SendJsonToServer(string jsonData)
    {
        string url = "http://localhost:5000/receiveRoom";  // Replace with your local server URL and endpoint
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
        
    }

    public void SetFire(OscMessage oscMessage){

        if(isServer)
        findTheSpotinthelist(oscMessage.values[0].ToString()).setFire();


    }


    public void putOutFire(string urid){


        OscMessage oscMessage=new OscMessage();
        oscMessage.address="/PutOutFire";
        oscMessage.values.Add(urid);
        
        if(isServer)
        osc.Send(oscMessage);



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
