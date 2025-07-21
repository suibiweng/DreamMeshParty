using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RealityEditor;
using UnityEngine.UI;
using UnityEngine.UIElements;
using OVR.OpenVR;
using TMPro;
public class SceneSessionManager : MonoBehaviour
{

    public string sessionURLID = "";

    public TMP_InputField TheSessionPremiseText;
    public TMP_InputField MorePromptText;


    public string TheSessionPremise = "";
    public string MorePrompt = "";

    private SceneDataSync SceneDataSync;

    // --- Serializable Classes ---

    [Serializable]
    public class SceneObjectData
    {
        public string id;
        public string name;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable]
    public class PhysicsData
    {
        public float timeScale;
        public Vector3 gravity;
    }

    [Serializable]
    public class GenerateSpotData
    {
        public string id;
        public string prompt;
    }

    [Serializable]
    public class UserData
    {
        public string userId;
        public string role;
        public string deviceId;
        public string joinedTime;
    }

    [Serializable]
    public class SessionData
    {
        public string sessionURLID;
        public string premise;
        public string prompt;
        public string created_by = "Suibi";
        public string timestamp;
        public List<SceneObjectData> SceneObjects;
        public PhysicsData physics;
        public List<GenerateSpotData> generateSpots;
        public List<UserData> users;
    }
    public List<SceneObjectData> SceneObjectsList;

    // --- Settings ---
    private string serverUrl = "http://localhost:5000/submit_session";

    RealityEditorManager manager;

    void Start()
    {
        manager = FindObjectOfType<RealityEditorManager>();

        serverUrl = manager.ServerURL + ":" + manager.uploadPort + "/submit_session";
        SceneObjectsList = new List<SceneObjectData>();
        SceneDataSync = GetComponent<SceneDataSync>();



    }

    public void addSceneObject(SceneObjectData objData)
    {
        SceneObjectsList.Add(objData);

    }




    // --- Entry Point ---
    public void SubmmiSession()
    {
        Debug.Log("📦 Preparing session data...");

        TheSessionPremise = TheSessionPremiseText.text;
        MorePrompt = MorePromptText.text;

        if (sessionURLID == "")
            sessionURLID = TimestampGenerator.GetTimestamp();

        SceneDataSync.UpdateURLID(sessionURLID);

        SessionData data = new SessionData
        {
            sessionURLID = sessionURLID,
            premise = TheSessionPremise,
            prompt = MorePrompt,
            timestamp = DateTime.UtcNow.ToString("s"),
            generateSpots = GatherGenerateSpots(),
            SceneObjects = SceneObjectsList
            // physics = CapturePhysicsData(),
            // 
            // users = GetUsersInSession()
        };





        string json = JsonUtility.ToJson(data, true);  // pretty print for debug
        Debug.Log(json);  // log it for inspection
        StartCoroutine(PostSessionData(json));
    }

    // --- Gather Furniture ---
    private List<SceneObjectData> GatherFurnitureData()
    {
        List<SceneObjectData> furnitureList = new List<SceneObjectData>();
        GameObject[] allFurniture = GameObject.FindGameObjectsWithTag("Furniture");

        foreach (GameObject obj in allFurniture)
        {
            furnitureList.Add(new SceneObjectData
            {
                name = obj.name,
                position = obj.transform.position,
                rotation = obj.transform.eulerAngles
            });
        }

        return furnitureList;
    }

    // --- Gather Physics ---
    private PhysicsData CapturePhysicsData()
    {
        return new PhysicsData
        {
            timeScale = Time.timeScale,
            gravity = Physics.gravity
        };
    }

    // --- Gather GenerateSpots ---
    public List<GenerateSpotData> GatherGenerateSpots()
    {
        List<GenerateSpotData> spots = new List<GenerateSpotData>();
        GenerateSpot[] allSpots = GameObject.FindObjectsOfType<GenerateSpot>();

        foreach (GenerateSpot spot in allSpots)
        {
            if (spot.gameObject.tag != "RealObject")
            {
                spots.Add(new GenerateSpotData
                {
                    id = spot.URLID,
                    prompt = spot.Prompt
                });


            }

        }

        return spots;
    }

    // --- Gather Users ---
    private List<UserData> GetUsersInSession()
    {
        return new List<UserData>
        {
            new UserData
            {
                userId = "suibi",
                role = "host",
                deviceId = SystemInfo.deviceUniqueIdentifier,
                joinedTime = DateTime.UtcNow.ToString("s")
            },
            new UserData
            {
                userId = "anika",
                role = "guest",
                deviceId = "meta-quest-123",
                joinedTime = DateTime.UtcNow.ToString("s")
            }
        };
    }

    // --- Send to Server ---
    IEnumerator PostSessionData(string json)
    {
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("✅ Session submitted successfully.");
        else
            Debug.LogError("❌ Submission failed: " + request.error);
    }



    public void FetchSession(string sessionURLID)
    {
        string url = $"http://localhost:5000/get_session/{sessionURLID}";
        StartCoroutine(FetchSessionCoroutine(url));
    }

    IEnumerator FetchSessionCoroutine(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("✅ Session fetched:\n" + json);

            SessionData session = JsonUtility.FromJson<SessionData>(json);
            Debug.Log($"Session: {session.premise} / {session.prompt}");
        }
        else
        {
            Debug.LogError("❌ Failed to fetch session: " + request.error);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SubmmiSession();
            


        }
    }

}
