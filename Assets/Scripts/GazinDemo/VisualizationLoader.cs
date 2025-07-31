using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System;
using RealityEditor;
using ExitGames.Client.Photon.StructWrapping;

public static class JsonUtilityWrapper
{
    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }

    public static List<T> FromJson<T>(string json)
    {
        string wrappedJson = "{\"Items\":" + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(wrappedJson).Items;
    }
}

[System.Serializable]
public class InteractionResult
{
    public string text;
    public float confidence;
    public string @class;      // For 2Dto3D
    public float[] box;        // For 2Dto3D
    public float mask_score;   // For 2Dto3D
}

[System.Serializable]
public class InteractionEntry
{
    public string Type;
    public string timestamp;
    public string result_path;
    public List<InteractionResult> result;
}

public class VisualizationLoader : MonoBehaviour
{
    public string jsonURL = "http://localhost:5000/unity_connection.json";
    public GameObject buttonPrefab;
    public Transform buttonParent;

    public GameObject StoryCube;

    public StorySender storySender;

    private List<InteractionEntry> interactions;

    RealityEditorManager realityEditorManager;

    void Start()
    {
        StartCoroutine(LoadAndCreateButtons());
        storySender = FindObjectOfType<StorySender>();
        realityEditorManager = FindObjectOfType<RealityEditorManager>();
        


    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("🔄 Reloading buttons...");
            ReloadButtons();
        }
    }

    IEnumerator LoadAndCreateButtons()
    {
        UnityWebRequest request = UnityWebRequest.Get(jsonURL);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            interactions = JsonUtilityWrapper.FromJson<InteractionEntry>(json);

            foreach (var entry in interactions)
            {
                GameObject buttonObj = Instantiate(buttonPrefab, buttonParent);
                TMP_Text textComponent = buttonObj.GetComponentInChildren<TMP_Text>();
                string label = "";

                label += $"<b>🧩 Type:</b> {entry.Type.Trim()}\n";
                label += $"<b>🕒 Time:</b> {entry.timestamp}\n";

                if (entry.result != null && entry.result.Count > 0)
                {
                var result = entry.result[0];
                if (!string.IsNullOrEmpty(result.text))
                {
                    label += $"<b>📄 Result:</b> {result.text}";
                }
                else if (!string.IsNullOrEmpty(result.@class))
                {
                    label += $"<b>📦 Object:</b> {result.@class}";
                }
                }

                textComponent.enableWordWrapping = true;
                textComponent.text = label;

                buttonObj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    string type = entry.Type.Trim();

                    if (type.Equals("Storytelling", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("🎤 Storytelling Triggered");
                        TriggerStorytelling(entry);
                    }
                    else if (type.Equals("Formula Visualization", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("🔬 Formula Visualization Triggered");
                        TriggerFormulaVisualization(entry);
                    }
                    else if (type.Equals("2Dto3D", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log("🧱 2Dto3D Triggered");
                        Trigger2Dto3D(entry);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Unknown interaction type: " + type);
                    }
                });
            }
        }
        else
        {
            Debug.LogError("Failed to fetch JSON: " + request.error);
        }
    }

    public void ReloadButtons()
    {
        foreach (Transform child in buttonParent)
        {
            Destroy(child.gameObject);
        }

        StartCoroutine(LoadAndCreateButtons());
    }

    void TriggerStorytelling(InteractionEntry entry)
    {
        Debug.Log($"📝 Story Text: {entry.result[0].text} | Confidence: {entry.result[0].confidence}");

        storySender.SendStory(entry.result[0].text);    
        // Visual storytelling logic
    }

    void TriggerFormulaVisualization(InteractionEntry entry)
    {
        Debug.Log($"🧪 Formula Text: {entry.result[0].text} | Confidence: {entry.result[0].confidence}");
        // Formula visualization logic
    }

void Trigger2Dto3D(InteractionEntry entry)
{
    var res = entry.result[0];

    // Build the payload (we'll assume result_path is the source file)
    string filePath = entry.result_path; 
    string urlid = IDGenerator.GenerateID(); // Example unique ID

    StoryCube= Instantiate(StoryCube, gameObject.transform.position, Quaternion.identity);
    var storySpot= StoryCube.GetComponent<StorySpot>();

    storySpot.startDownload(realityEditorManager.ServerURL+":"+ realityEditorManager.downloadPort + "/" + $"{urlid}_2Dto3D.zip");

    Debug.Log($"📦 Sending 2Dto3D request to server for object: {res.@class}");
    


    // Start the request
    StartCoroutine(CallImageTo3D(filePath, urlid));
}

IEnumerator CallImageTo3D(string filePath, string urlid)
{
    string serverURL = "http://127.0.0.1:5000/ImageTo3D"; // Flask server endpoint

    WWWForm form = new WWWForm();
    form.AddField("file", filePath);  // Server expects this as string path
    form.AddField("URLID", urlid);

    using (UnityWebRequest www = UnityWebRequest.Post(serverURL, form))
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ ImageTo3D request success: {www.downloadHandler.text}");
        }
        else
        {
            Debug.LogError($"❌ ImageTo3D request failed: {www.error}");
        }
    }
}

}
