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
   public string TestLoadSceneName;

   public TMP_Dropdown ScenesDropDown;
   public TMP_Text ScenePromptTMP; 
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
   
   [System.Serializable]
   public class StringArrayWrapper
   {
       public List<string> fileNames;
   }


   private void Start()
   {
       uploadURL = RealityEditorManager.ServerURL+":8000";
       PopulateDropdown();
   }

   void Update()
   {
       
       if(OVRInput.GetUp(OVRInput.RawButton.LThumbstick))
       {
           source.PlayOneShot(savingSound);
           Debug.Log("Start Button Has Been Pressed. Saving Scene...");
           SaveSceneToServer();
       }
       if(OVRInput.GetUp(OVRInput.RawButton.RThumbstick)){
           source.PlayOneShot(loadingSound);
           Debug.Log("Load Button Has Been Pressed. Loading Scene...");
           LoadSceneFromServer();
       }
       if(Input.GetKeyDown(KeyCode.D)){
           PopulateDropdown();
       }
   }
   
   public void SaveSceneToServer()
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
       allData.sceneName = ScenePromptTMP.text; 
       string json = JsonUtility.ToJson(allData);
       // Save to PlayerPrefs
       Debug.Log("Saving the JsonString: " + json);
       
       PlayerPrefs.SetString("GenerateSpotData", json);
      StartCoroutine( UploadJsonFile(json)); 
       PlayerPrefs.Save();
   }

   public void LoadSceneFromServer()
   {
       // Request to load the selected scene from the server
       if (TestLoadSceneName.Length == 0)
       {
           string selectedSceneName = ScenesDropDown.options[ScenesDropDown.value].text;
           StartCoroutine(DownloadSceneData(selectedSceneName));
       }
       else
       {
           StartCoroutine(DownloadSceneData(TestLoadSceneName));
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
   IEnumerator DownloadSceneData(string fileName)
   {
       Debug.Log("Trying to load a specific scene:" + fileName);
       UnityWebRequest www = UnityWebRequest.Get($"{uploadURL}/download?filename={fileName}");
       yield return www.SendWebRequest();

       if (www.result == UnityWebRequest.Result.Success)
       {
           string json = www.downloadHandler.text;
           SavedSceneData allData = JsonUtility.FromJson<SavedSceneData>(json);
           Debug.Log("Loading Scene: " + allData.sceneName);
           Debug.Log("Loading scene with " + allData.generateSpotDataList.Count + " Cubes");

           foreach (var data in allData.generateSpotDataList)
           {
               GameObject newObject = RealityEditorManager.createSavedSpot(data.position, data.rotation, data.scale, data.urlid);
               Debug.Log("loading urlid: " + data.urlid);
               newObject.GetComponent<GenerateSpot>().initAdd();
           }
       }
       else
       {
           Debug.LogError("Error downloading scene data: " + www.error);
       }
   }
   
   void PopulateDropdown()
   {
       Debug.Log("Should be populating the dropdown");
       // Clear existing options
       // ScenesDropDown.ClearOptions();

       // Start coroutine to get filenames from the server
       StartCoroutine(FetchSceneFileNames());
   }
   
   public List<string> ParseJsonArray(string jsonArray)
   {
       // Modify the JSON string to match the format expected by the wrapper class
       string wrappedJson = "{\"fileNames\":" + jsonArray + "}";

       // Deserialize using the wrapper class
       StringArrayWrapper wrapper = JsonUtility.FromJson<StringArrayWrapper>(wrappedJson);
       return wrapper.fileNames;
   }
   
   IEnumerator FetchSceneFileNames()
   {
       Debug.Log("dropdown asking for all the scene names");
       UnityWebRequest www = UnityWebRequest.Get($"{uploadURL}/list-files");
       yield return www.SendWebRequest();
        
       if (www.result == UnityWebRequest.Result.Success)
       {
           Debug.Log("dropdown Successful Request");

           // Assume the server returns a JSON array of filenames
           string jsonResponse = www.downloadHandler.text;
           Debug.Log("DROPDOWN GOT A RESPONSE OF ALL THE SCENES: " + jsonResponse);
           List<string> fileNames = ParseJsonArray(jsonResponse); 
           Debug.Log("DROPDOWN broke the json into individual names" + fileNames);

           // Populate the dropdown with the filenames
           foreach (var name in fileNames)
           {
               Debug.Log("Adding Dropdown Value" + name);
               ScenesDropDown.options.Add(new TMP_Dropdown.OptionData(name, null));
           }
           
           ScenesDropDown.RefreshShownValue();
           Debug.Log("Dropdown populated with scene filenames." + fileNames); 
       }
       else
       {
           Debug.LogError("Error fetching filenames: " + www.error);
       }
   }
   
}

