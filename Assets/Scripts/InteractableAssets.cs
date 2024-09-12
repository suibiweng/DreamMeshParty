using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;    
using UnityEngine.Networking;
using UnityEngine.UI;
using AmplifyShaderEditor;



class InteractableDreamMesh{
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

    InteractableDreamMesh interactableDreamMesh;

    public Toggle interactableToggle;
    // Start is called before the first frame update

    public GameObject interactiveCamera;
     public RenderTexture renderTexture; // Assign the render texture in the Inspector
    public string uploadUrl = "http://localhost:5000/upload"; // Change this to your server URL

    void Start()
    {
        
        manager = FindObjectOfType<RealityEditorManager>();
        generateSpot=gameObject.GetComponent<GenerateSpot>();


        uploadUrl = manager.ServerURL+":5000/upload/"; 

     //   StartCoroutine(UploadTexture());
    }

 private Coroutine checkCoroutine;
    public void SetInteracteable(){
        interactiveCamera.SetActive(true);      


         StartCoroutine(UploadSnapShotTexture());

         OscMessage msg = new OscMessage{
            address="/SetInteractive"
         };

         msg.values.Add(generateSpot.URLID);

         manager.osc.Send(msg);

         // looking for json File;

         checkCoroutine = StartCoroutine(CheckForJsonOnServer( uploadUrl+generateSpot.URLID+"_interactable.json",3f));


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
