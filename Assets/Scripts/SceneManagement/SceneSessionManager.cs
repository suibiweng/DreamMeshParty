using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using RealityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class SceneSessionManager : MonoBehaviour
{
    public string sessionURLID = "";
    public TMP_InputField TheSessionPremiseText;
    public TMP_InputField MorePromptText;
    public string TheSessionPremise = "";
    public string MorePrompt = "";

    private SceneDataSync SceneDataSync;
    private RealityEditorManager manager;

    // UI list
    public Transform uiPanelRoot;                 // VerticalLayoutGroup under a ScrollView
    public GameObject controlItemPrefab;          // Row prefab: NameText, StatusText, PlayButton, StopButton, ParamUIButton

    // Tabs (ToggleGroup)
    public ToggleGroup tabGroup;
    public Toggle tabAllToggle;
    public Toggle tabGeneratedToggle;
    public Toggle tabRealObjectToggle;

    [Header("Tab Visuals")]
    public Color activeTabColor = new Color(0.18f, 0.5f, 0.95f, 1f);
    public Color inactiveTabColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public bool boldActiveTabLabel = true;

    private enum Tab { All, Generated, RealObject }
    private Tab currentTab = Tab.All;

    // --- Serializable Data Classes ---
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
        public string gameObjectName;
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
    private string serverUrl = "http://localhost:5000/submit_session";

    void Start()
    {
        manager = FindObjectOfType<RealityEditorManager>();
        serverUrl = manager.ServerURL + ":" + manager.uploadPort + "/submit_session";
        SceneObjectsList = new List<SceneObjectData>();
        SceneDataSync = GetComponent<SceneDataSync>();

        EnsureRaycastSystems();
        InitTabs();             // robust toggle wiring

        BuildUIControlMenu();
    }

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;
        UpdateTabVisuals();
        BuildUIControlMenu();
    }

    private void UpdateTabVisuals()
    {
        void StyleToggle(Toggle t, bool active)
        {
            if (t == null) return;

            var bg = t.GetComponent<Image>();
            if (bg != null) bg.color = active ? activeTabColor : inactiveTabColor;

            var label = t.GetComponentInChildren<TMP_Text>();
            if (label != null && boldActiveTabLabel)
                label.fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        }

        StyleToggle(tabAllToggle,        currentTab == Tab.All);
        StyleToggle(tabGeneratedToggle,  currentTab == Tab.Generated);
        StyleToggle(tabRealObjectToggle, currentTab == Tab.RealObject);
    }

    public void addSceneObject(SceneObjectData objData)
    {
        SceneObjectsList.Add(objData);
    }

    public void SubmmiSession()
    {
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
            SceneObjects = GatherRealObjectData()
        };

        string json = JsonUtility.ToJson(data, true);
        Debug.Log(json);
        StartCoroutine(PostSessionData(json));

        BuildUIControlMenu();
    }

    private List<SceneObjectData> GatherRealObjectData()
    {
        List<SceneObjectData> list = new List<SceneObjectData>();
        GameObject[] all = GameObject.FindGameObjectsWithTag("RealObject");

        foreach (GameObject obj in all)
        {
            list.Add(new SceneObjectData
            {
                id = obj.GetComponent<GenerateSpot>().URLID,
                name = obj.name,
                position = obj.transform.position,
                rotation = obj.transform.eulerAngles
            });
        }

        // Keep list synced for RealObject tab
        SceneObjectsList = list;
        return list;
    }

    private PhysicsData CapturePhysicsData()
    {
        return new PhysicsData
        {
            timeScale = Time.timeScale,
            gravity = Physics.gravity
        };
    }

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
                    prompt = spot.Prompt,
                    gameObjectName = spot.gameObject.name
                });
            }
        }

        return spots;
    }

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

    IEnumerator PostSessionData(string json)
    {
        UnityWebRequest request = new UnityWebRequest(serverUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            Debug.Log("Session submitted successfully.");
        else
            Debug.LogError("Submission failed: " + request.error);
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
            Debug.Log("Session fetched:\n" + json);

            SessionData session = JsonUtility.FromJson<SessionData>(json);
            Debug.Log($"Session: {session.premise} / {session.prompt}");

            BuildUIControlMenu();
        }
        else
        {
            Debug.LogError("Failed to fetch session: " + request.error);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            SubmmiSession();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            BuildUIControlMenu();
        }
    }

    public bool isPlaying;

    public void PlayAll()
    {
        if (isPlaying) return;
        isPlaying = true;

        foreach (GameObject child in manager.GenCubesDic.Values)
        {
            LuaMonoBehavior lua = child.GetComponent<LuaMonoBehavior>();
            if (lua != null)
            {
                lua.Play();
            }
        }
    }

    public void StopAll()
    {
        if (!isPlaying) return;
        isPlaying = false;

        foreach (GameObject child in manager.GenCubesDic.Values)
        {
            LuaMonoBehavior lua = child.GetComponent<LuaMonoBehavior>();
            if (lua != null)
            {
                lua.Stop();
            }
        }
    }

    // === UI CONTROL PANEL WITH TABS ===

    public void BuildUIControlMenu()
    {
        // Refresh sources before building
        GatherRealObjectData();
        var generated = GatherGenerateSpots();

        foreach (Transform child in uiPanelRoot)
        {
            Destroy(child.gameObject);
        }

        if (currentTab == Tab.All || currentTab == Tab.RealObject)
        {
            foreach (var objData in SceneObjectsList)
            {
                CreateUIItem(objData.name);
            }
        }

        if (currentTab == Tab.All || currentTab == Tab.Generated)
        {
            foreach (var spot in generated)
            {
                CreateUIItem(spot.gameObjectName);
            }
        }
    }

    private void CreateUIItem(string objectName)
    {
        GameObject uiItem = Instantiate(controlItemPrefab, uiPanelRoot);

        TMP_Text label = uiItem.transform.Find("NameText").GetComponent<TMP_Text>();
        TMP_Text statusText = uiItem.transform.Find("StatusText").GetComponent<TMP_Text>();

        Button playBtn = uiItem.transform.Find("PlayButton").GetComponent<Button>();
        Button stopBtn = uiItem.transform.Find("StopButton").GetComponent<Button>();
        Button paramBtn = uiItem.transform.Find("ParamUIButton").GetComponent<Button>();

        label.text = objectName;
        statusText.text = "Idle";

        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            playBtn.interactable = false;
            stopBtn.interactable = false;
            paramBtn.interactable = false;
            statusText.text = "Not Found";
            return;
        }

        LuaMonoBehavior lua = target.GetComponent<LuaMonoBehavior>();
        if (lua == null)
        {
            playBtn.interactable = false;
            stopBtn.interactable = false;
            paramBtn.interactable = false;
            statusText.text = "No Lua";
            return;
        }

        playBtn.onClick.AddListener(() =>
        {
            lua.Play();
            statusText.text = "Playing";
        });

        stopBtn.onClick.AddListener(() =>
        {
            lua.Stop();
            statusText.text = "Stopped";
        });

        // paramBtn.onClick.AddListener(() => lua.ShowParameterUI());
    }

    // === Robust tab setup helpers ===

    private void EnsureRaycastSystems()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void InitTabs()
    {
        // Ensure a ToggleGroup exists
        if (tabGroup == null)
        {
            Transform parent = null;
            if (tabAllToggle != null) parent = tabAllToggle.transform.parent;
            else if (tabGeneratedToggle != null) parent = tabGeneratedToggle.transform.parent;
            else if (tabRealObjectToggle != null) parent = tabRealObjectToggle.transform.parent;

            if (parent != null)
                tabGroup = parent.GetComponent<ToggleGroup>() ?? parent.gameObject.AddComponent<ToggleGroup>();
        }

        // Force all toggles into the same group
        AssignToGroup(tabAllToggle);
        AssignToGroup(tabGeneratedToggle);
        AssignToGroup(tabRealObjectToggle);

        // Rewire listeners
        if (tabAllToggle != null)
        {
            tabAllToggle.onValueChanged.RemoveAllListeners();
            tabAllToggle.onValueChanged.AddListener(on => { if (on) SwitchTab(Tab.All); });
        }
        if (tabGeneratedToggle != null)
        {
            tabGeneratedToggle.onValueChanged.RemoveAllListeners();
            tabGeneratedToggle.onValueChanged.AddListener(on => { if (on) SwitchTab(Tab.Generated); });
        }
        if (tabRealObjectToggle != null)
        {
            tabRealObjectToggle.onValueChanged.RemoveAllListeners();
            tabRealObjectToggle.onValueChanged.AddListener(on => { if (on) SwitchTab(Tab.RealObject); });
        }

        // Initial selection: respect existing On state; else default to All
        if (tabAllToggle != null && tabAllToggle.isOn) currentTab = Tab.All;
        else if (tabGeneratedToggle != null && tabGeneratedToggle.isOn) currentTab = Tab.Generated;
        else if (tabRealObjectToggle != null && tabRealObjectToggle.isOn) currentTab = Tab.RealObject;
        else if (tabAllToggle != null) { tabAllToggle.isOn = true; currentTab = Tab.All; }

        UpdateTabVisuals();
    }

    private void AssignToGroup(Toggle t)
    {
        if (t == null) return;
        t.group = tabGroup;
    }
}
