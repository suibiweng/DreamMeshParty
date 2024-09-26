using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using RealityEditor;
using UnityEngine.Networking;

public class RoomScanManager : MonoBehaviour
{

    public ParticleSystem hintParticle;

    public MeshExporter meshExporter;
    public GameObject Roommesh;

    public List<GameObject> Cropboxes;


    public GameObject BoundingBox;


    public OSC osc;

    public RealityEditorManager manager;


    
    int imgCount;
    string ID;
    public bool recording;


    public class FutnitureData
{
    public string name;
    
    public Vector3 position;
    public Vector3 scale;
    public Vector3 rotationEulerAngles;
}


    // Start is called before the first frame update
    void Start()
    {
        Cropboxes=new List<GameObject>();
    //    StartCoroutine(searchRoomMesh(1.0f));

    osc =FindAnyObjectByType<OSC>();      


    StartCoroutine(DelayTurnOffMesh());

    manager=FindAnyObjectByType<RealityEditorManager>();
    
        //Search room mesh
        StartCoroutine(GettheBoxes());
    }

 
    MeshFilter TargetMesh;
    Vector3 RoomPosition;

    public void getMeshObj (MeshFilter meshFilter)
    {

        Roommesh=meshFilter.gameObject;
        TargetMesh=meshFilter;
        meshExporter.objectToExport=meshFilter.gameObject;

        // RoomPosition=Roommesh.transform.position;
        //  Debug.Log("Room:"+RoomPosition+Roommesh.transform.localScale+Roommesh.transform.rotation.eulerAngles);



    



     



    }
    IEnumerator DelayTurnOffMesh(){
        yield return new WaitForSeconds(1);


           Roommesh.SetActive(false);


    }

      IEnumerator GettheBoxes(){
        yield return new WaitForSeconds(10);

            getallCropBoxes();
           


    }


    public void uploadRoomMesh(){

        meshExporter.UploadMeshDirectly(Roommesh);


    }



    public void uploadCropRoomMesh(){

        //meshExporter.CropAndUploadMesh(targetObj.GetComponent<MeshFilter>(),BoundingBox);


    }

public List<FutnitureData> cropBoxes = new List<FutnitureData>();

    public void getallCropBoxes()
    {
        foreach (GameObject g in GameObject.FindGameObjectsWithTag("CropBox"))
        {
            FutnitureData data = new FutnitureData
            {
                name = g.transform.parent.name,
              // Parent is set to the object's own name
                position = g.transform.position,
                scale = g.transform.localScale,
                rotationEulerAngles = g.transform.rotation.eulerAngles
            };

            cropBoxes.Add(data);
        }

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


         if(OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger)){
            if(recording){


                UpdateRoomScanning();

                hintParticle.Play();


            }


        }

        
    }


    public void StartRoomScanning(){

        recording = true;
        imgCount=0;

        ID =TimestampGenerator.GetTimestamp();
       // startRecordingTime = Time.time;
        OscMessage message;
        message = new OscMessage();
        message.address = "/RoomScanStart";
        message.values.Add(ID);
        osc.Send(message);
        imgCount=0;

           Roommesh.SetActive(true);








    }



    public void UpdateRoomScanning(){

        if(!capturing)
            StartCoroutine( DelayCapture(0.2f));

   



        
    }

    bool   capturing=false;
    IEnumerator DelayCapture(float delaysecod){
        capturing=true;

        Capture();

         yield return new WaitForSeconds(delaysecod);
         
        capturing=false;


    }


    void  Capture()
{

             OscMessage message;

                message = new OscMessage();
                message.address = "/Roomscan";
                message.values.Add(ID+"_RoomScan_"+imgCount+"");
                //message.values.Add(this.transform.localToWorldMatrix.ToString());
             
                // osc.Send(message);
                
    
                imgCount++;
               // instruction="Captured pics :"+imgCount;


               osc.Send(message);





}


    public void EndRoomScanning(){

        recording = false;
        OscMessage message;
        message = new OscMessage();
        message.address = "/RoomscanEnd";
        osc.Send(message);

        hintParticle.Stop();
        hintParticle.Clear();



        Roommesh.SetActive(false);



        // foreach(var c in cameraPoses){
        //     Destroy((GameObject)c);
        // }

        // campoints.Seton(false);




    }

}
