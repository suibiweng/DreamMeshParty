using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.Networking;
using Random = UnityEngine.Random;
using RealityEditor;
using TMPro;
using UnityEngine.UIElements;

public class SceneSaverTest : MonoBehaviour
{
   public AudioSource source;
   public AudioClip savingSound, loadingSound;
   public RealityEditorManager RealityEditorManager; 
   private string uploadURL;
   private string savedSceneFolderPath;

   public TMP_Dropdown ScenesDropDown; 

   [System.Serializable]
   public class GenerateSpotData
   {
       public Vector3 position;
       public Quaternion rotation;
       public Vector3 scale;
       public string urlid; 
   }


   [System.Serializable]
   public class SavedSceneData
   {
       public string sceneName; 
       public List<GenerateSpotData> generateSpotDataList;
   }


   private void Start()
   {
       uploadURL = RealityEditorManager.ServerURL+":8000";
       savedSceneFolderPath = Path.Combine(Application.dataPath, "../SavedScene");
       PopulateDropdown();
   }

   void Update()
   {
       
       if(OVRInput.GetUp(OVRInput.RawButton.LThumbstick))
       {
           source.PlayOneShot(savingSound);
           Debug.Log("Start Button Has Been Pressed. Saving Scene...");
           SaveGenerateSpotsToPlayerPrefs();
       }
       if(OVRInput.GetUp(OVRInput.RawButton.RThumbstick)){
           source.PlayOneShot(loadingSound);
           Debug.Log("Load Button Has Been Pressed. Loading Scene...");
           LoadGenerateSpotsFromPlayerPrefs();
       }
   }


   public void SaveGenerateSpotsToPlayerPrefs()
   {
       // Find all objects of type GenerateSpot
       GameObject[] generateSpots = GameObject.FindGameObjectsWithTag("GenerateSpot");
       List<GenerateSpotData> generateSpotDataList = new List<GenerateSpotData>();
       Debug.Log("found " + generateSpots.Length + " Cubes while saving scene");
       // Extract data
       foreach (var generateSpot in generateSpots)
       {
           GenerateSpotData data = new GenerateSpotData();
           data.position = generateSpot.transform.position;
           data.rotation = generateSpot.transform.rotation;
           data.scale = generateSpot.transform.localScale;
           data.urlid = generateSpot.GetComponent<GenerateSpot>().URLID; 
           Debug.Log("saving the urlid: " + data.urlid);
           generateSpotDataList.Add(data);
       }
       // Serialize data to JSON
       SavedSceneData allData = new SavedSceneData();
       allData.generateSpotDataList = generateSpotDataList;
       allData.sceneName = "SavedScene: " + Random.value; 
       string json = JsonUtility.ToJson(allData);


       // Save to PlayerPrefs
       Debug.Log("Saving the JsonString: " + json);
       
       PlayerPrefs.SetString("GenerateSpotData", json);
      StartCoroutine( UploadJsonFile(json)); 
       PlayerPrefs.Save();
   }


   public void LoadGenerateSpotsFromPlayerPrefs()
   {
       if (PlayerPrefs.HasKey("GenerateSpotData"))
       {
           string json = PlayerPrefs.GetString("GenerateSpotData");

           // Deserialize the JSON string back to the object 
           SavedSceneData allData = JsonUtility.FromJson<SavedSceneData>(json);
           Debug.Log("Loading Scene: " + allData.sceneName);

           Debug.Log("Loading scene with " + allData.generateSpotDataList.Count + " Cubes");
           foreach (var data in allData.generateSpotDataList)
           {
               GameObject newObject = RealityEditorManager.createSavedSpot(data.position, data.rotation, data.scale, data.urlid);
               Debug.Log("loading urlid: " + data.urlid);
               // newObject.GetComponent<GenerateSpot2>().URLID = data.URLID;
               newObject.GetComponent<GenerateSpot>().initAdd();
           }
       }
   }
   
   IEnumerator UploadJsonFile(string jsonData)
   {
       // Convert the string to a byte array
       byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonData);

       // Create a UnityWebRequest for POST
       UnityWebRequest www = new UnityWebRequest(uploadURL, "POST");

       www.uploadHandler = new UploadHandlerRaw(jsonToSend);
       www.downloadHandler = new DownloadHandlerBuffer();

       // Set the content type to JSON
       www.SetRequestHeader("Content-Type", "application/json");

       // Send the request and wait for a response
       yield return www.SendWebRequest();

       if (www.result == UnityWebRequest.Result.Success)
       {
           Debug.Log("Upload complete! Server response: " + www.downloadHandler.text);
       }
       else
       {
           Debug.LogError("Error uploading JSON: " + www.error);
       }

   }
   
   void PopulateDropdown()
   {
       // Clear existing options
       ScenesDropDown.ClearOptions();

       // Check if the folder exists
       if (!Directory.Exists(savedSceneFolderPath))
       {
           Debug.LogError("SavedScene folder does not exist.");
           return;
       }

       // Get all JSON files in the folder
       string[] files = Directory.GetFiles(savedSceneFolderPath, "*.json");

       // Extract scene names from filenames
       List<string> sceneNames = new List<string>();
       foreach (string file in files)
       {
           // Extract the filename without extension
           string fileName = Path.GetFileNameWithoutExtension(file);

           // Add the extracted scene name to the list
           sceneNames.Add(fileName);
       }

       // Add the scene names to the dropdown
       ScenesDropDown.AddOptions(sceneNames);
   }
   
   
   
}

