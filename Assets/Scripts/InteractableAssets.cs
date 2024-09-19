using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;    
using UnityEngine.Networking;
using UnityEngine.UI;

using Oculus.Interaction;



public class InteractableDreamMesh{
    public string prompt;
    public int iswearable; //0 not a wearable object 1:head ,2:body ,3:hand ,4:foot, 5:low body
    public bool interactable;
    public Vector2 output_position;
    public int output_style; //0: particle ,1: Sound,   2: Light 
    public int input_style; // 0:trigger 1:switch
    public int trigger_style;//0:no trigger  , 1:contunue ,2:one shot

}

public class InteractableAssets : MonoBehaviour
{
    public Camera targetCamera;
    public RealityEditorManager manager;
    public GenerateSpot generateSpot;

    public InteractableDreamMesh interactableDreamMesh;

    public Toggle interactableToggle;
    // Start is called before the first frame update

    public TriggerSync triggerSync;


    public outputComponent outputspot;
    public GameObject interactiveCamera;
     public RenderTexture renderTexture; // Assign the render texture in the Inspector
    public string uploadUrl = "http://localhost:5000/"; // Change this to your server URL

    public Transform Left,Right;


    void Start()
    {
       
        
        manager = FindObjectOfType<RealityEditorManager>();
        generateSpot=gameObject.GetComponent<GenerateSpot>();
        triggerSync=gameObject.GetComponent<TriggerSync>();

        Left=manager.LeftHand;
        Right=manager.RightHand;




    
        checkCoroutine = StartCoroutine(CheckForJsonOnServer( generateSpot.downloadURL+generateSpot.URLID+"_interactable.json",3f));

     //   StartCoroutine(UploadTexture());
    }













 private Coroutine checkCoroutine;
    public void SetInteractable(){

        //interactiveCamera.SetActive(true);      
        //  StartCoroutine(UploadSnapShotTexture());

         OscMessage msg = new OscMessage{
            address="/SetInteractive"
         };

         msg.values.Add(generateSpot.URLID);
         msg.values.Add(generateSpot.DremmeshPrompt);
         manager.osc.Send(msg);




         // looking for json File;
       checkCoroutine = StartCoroutine(CheckForJsonOnServer( uploadUrl+generateSpot.URLID+"_interactable.json",3f));



    }


    private void AlignWithController(Transform TargetTransform)
    {
        // Get the controller's forward direction
        Vector3 controllerForward = TargetTransform.transform.forward;

        // // Align the object's forward direction with the controller's forward direction
        
         outputspot.transform.forward = controllerForward;

        // // (Optional) Keep object position relative to the controller
        // transform.position = grabber.transform.position + grabber.transform.forward * someDistance;
    }







    void Update() {


        generateSpot.toLockthePosition(!interactableToggle.isOn);

        if(OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger ) ){

                AlignWithController(Left);

        }
        if(OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger)){

             AlignWithController(Right);

        }









        if(interactableDreamMesh!=null ){
            if(interactableDreamMesh.input_style==0){
                if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger )  ||  OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) ){
                
                
                 //output.BroadcastMessage("triggerOutPut");
                if(generateSpot.isGrabing){
                    outputspot.triggerOutPut();
                    triggerSync.CallTriggerRPC();
                }
                 
                
                }
        
            }

              if(interactableDreamMesh.input_style==1){


                  if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||  OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) ){


            if(generateSpot.isGrabing){
                outputspot.triggerOutPut();
                triggerSync.CallTriggerRPC();



            }
                 
                
                
                    //output.BroadcastMessage("triggerOutPut");
                
                }

              }




        }


        
    }


    IEnumerator ReadJsonFromServer(string jsonURL)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(jsonURL))
        {
            // Send the request and wait for the response
            yield return request.SendWebRequest();

            // Check for errors
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching JSON: " + request.error);
            }
            else
            {
                // Get the JSON data
                string json = request.downloadHandler.text;

                // Parse the JSON into an object
                // MyJsonData jsonData = JsonUtility.FromJson<MyJsonData>(json);

                // // Now you can access jsonData fields
                // Debug.Log("Key1: " + jsonData.key1);
                // Debug.Log("Key2: " + jsonData.key2);
                // Debug.Log("Key3: " + jsonData.key3);
            }
        }
    }



        IEnumerator CheckForJsonOnServer(string jsonURL,float checkInterval)
    {
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(jsonURL))
            {
                // Send the request and wait for the response
                yield return request.SendWebRequest();

                // Check if the file exists or there are any errors
                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning("JSON not found or error fetching JSON: " + request.error);
                }
                else
                {
                    // If the file exists, read and parse the JSON
                    string json = request.downloadHandler.text;
                  InteractableDreamMesh jsonData = JsonUtility.FromJson<InteractableDreamMesh>(json);

                    interactableDreamMesh= jsonData;

                    // Stop checking after successful reading
                    StopCheckingForJson();
                    yield break;
                }
            }

            // Wait for the specified interval before checking again
            yield return new WaitForSeconds(checkInterval);
        }
    }


        public void StopCheckingForJson()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
            Debug.Log("Coroutine manually stopped.");
        }
    }

    IEnumerator UploadSnapShotTexture()
    {
        // Convert RenderTexture to Texture2D
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        // Encode Texture2D to JPG format in memory
        byte[] textureBytes = texture.EncodeToJPG();

        // Create a WWWForm and add the byte array as binary data
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", textureBytes, "image.jpg", "image/jpeg");

        // Create and send the request
        UnityWebRequest www = UnityWebRequest.Post(uploadUrl, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("Error: " + www.error);
        }
        else
        {
            Debug.Log("Texture uploaded successfully!");
        }


        // interactiveCamera.SetActive(false);      
    }
}
