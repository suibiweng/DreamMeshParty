using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Realtime Refresh")]
    public float listRefreshInterval = 0.5f;      // seconds
    public float statusRefreshInterval = 0.25f;   // seconds

    private enum Tab { All, Generated, RealObject }
    private Tab currentTab = Tab.All;

    [Serializable]
    public class SceneObjectData
    {
        public string id;
        public string name;
        public Vector3 position;
        public Vector3 rotation;
    }

    [Serializable] public class PhysicsData { public float timeScale; public Vector3 gravity; }
    [Serializable] public class GenerateSpotData { public string id; public string prompt; public string gameObjectName; }
    [Serializable] public class UserData { public string userId; public string role; public string deviceId; public string joinedTime; }

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

    // UI row registry
    private readonly Dictionary<string, UIControlRow> _rows = new Dictionary<string, UIControlRow>(128);
    private Coroutine _listWatcher;

    void Start()
    {
        manager = FindObjectOfType<RealityEditorManager>();
        serverUrl = manager.ServerURL + ":" + manager.uploadPort + "/submit_session";
        SceneObjectsList = new List<SceneObjectData>();
        SceneDataSync = GetComponent<SceneDataSync>();

        EnsureRaycastSystems();
        InitTabs();

        BuildUIControlMenu();

        if (_listWatcher != null) StopCoroutine(_listWatcher);
        _listWatcher = StartCoroutine(LiveListWatcher());
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

    public void addSceneObject(SceneObjectData objData) => SceneObjectsList.Add(objData);

    public void SubmmiSession()
    {
        TheSessionPremise = TheSessionPremiseText.text;
        MorePrompt = MorePromptText.text;

        if (sessionURLID == "") sessionURLID = TimestampGenerator.GetTimestamp();
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
        var list = new List<SceneObjectData>();
        var all = GameObject.FindGameObjectsWithTag("RealObject");
        foreach (var obj in all)
        {
            var gs = obj.GetComponent<GenerateSpot>();
            list.Add(new SceneObjectData
            {
                id = gs != null ? gs.URLID : obj.GetInstanceID().ToString(),
                name = obj.name,
                position = obj.transform.position,
                rotation = obj.transform.eulerAngles
            });
        }
        SceneObjectsList = list;
        return list;
    }

    private PhysicsData CapturePhysicsData() => new PhysicsData { timeScale = Time.timeScale, gravity = Physics.gravity };

    public List<GenerateSpotData> GatherGenerateSpots()
    {
        var spots = new List<GenerateSpotData>();
        var allSpots = GameObject.FindObjectsOfType<GenerateSpot>();
        foreach (var spot in allSpots)
        {
            if (spot.gameObject.tag == "RealObject") continue;
            spots.Add(new GenerateSpotData { id = spot.URLID, prompt = spot.Prompt, gameObjectName = spot.gameObject.name });
        }
        return spots;
    }

    private List<UserData> GetUsersInSession()
    {
        return new List<UserData>
        {
            new UserData{ userId = "suibi", role = "host", deviceId = SystemInfo.deviceUniqueIdentifier, joinedTime = DateTime.UtcNow.ToString("s") },
            new UserData{ userId = "anika", role = "guest", deviceId = "meta-quest-123", joinedTime = DateTime.UtcNow.ToString("s") }
        };
    }

    IEnumerator PostSessionData(string json)
    {
        var request = new UnityWebRequest(serverUrl, "POST");
        var bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) Debug.Log("Session submitted successfully.");
        else Debug.LogError("Submission failed: " + request.error);
    }

    public void FetchSession(string sessionURLID)
    {
        var url = $"http://localhost:5000/get_session/{sessionURLID}";
        StartCoroutine(FetchSessionCoroutine(url));
    }

    IEnumerator FetchSessionCoroutine(string url)
    {
        var request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var json = request.downloadHandler.text;
            Debug.Log("Session fetched:\n" + json);
            var session = JsonUtility.FromJson<SessionData>(json);
            Debug.Log($"Session: {session.premise} / {session.prompt}");
            BuildUIControlMenu();
        }
        else
        {
            Debug.LogError("Failed to fetch session: " + request.error);
        }
    }


    public bool isPlaying = false;
    public void PlayALL() => SetAllRunning(true);
    public void StopALL() => SetAllRunning(false);

private void SetAllRunning(bool run)
{
        isPlaying = run;
    // Iterate every Lua-driven object in the scene
        var allLua = FindObjectsOfType<LuaMonoBehavior>();

    foreach (var lua in allLua)
    {
        if (lua == null) continue;

        var rpc = lua.GetComponent<LuaNetRPC>();

        if (rpc != null)
        {
            // Optimistic local action for instant feedback (avoids double-run using IsRunning)
            if (run && !rpc.IsRunning)        lua.Play();
            else if (!run && rpc.IsRunning)   lua.Stop();

            // Network fan-out (will also execute on this client)
            rpc.PlaySynced(run);
        }
        else
        {
            // No RPC on this object -> local only
            if (run) lua.Play();
            else     lua.Stop();
        }
    }
}


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) SubmmiSession();
        if (Input.GetKeyDown(KeyCode.F6)) BuildUIControlMenu();
    }

    // === UI CONTROL PANEL WITH TABS ===

    public void BuildUIControlMenu()
    {
        foreach (Transform child in uiPanelRoot) Destroy(child.gameObject);
        _rows.Clear();

        foreach (var name in EnumerateDisplayNamesForCurrentTab())
        {
            var row = CreateUIItem(name);
            _rows[name] = row;
        }
    }

    private IEnumerable<string> EnumerateDisplayNamesForCurrentTab()
    {
        GatherRealObjectData();
        var generated = GatherGenerateSpots();

        if (currentTab == Tab.All || currentTab == Tab.RealObject)
            foreach (var obj in SceneObjectsList) yield return obj.name;

        if (currentTab == Tab.All || currentTab == Tab.Generated)
            foreach (var gs in generated) yield return gs.gameObjectName;
    }

    private IEnumerator LiveListWatcher()
    {
        var statusTick = 0f;
        while (true)
        {
            yield return new WaitForSeconds(listRefreshInterval);

            var currentNames = new HashSet<string>(EnumerateDisplayNamesForCurrentTab());

            var stale = _rows.Keys.Where(n => !currentNames.Contains(n)).ToList();
            foreach (var n in stale)
            {
                if (_rows.TryGetValue(n, out var row) && row != null) Destroy(row.gameObject);
                _rows.Remove(n);
            }

            foreach (var n in currentNames)
            {
                if (!_rows.ContainsKey(n))
                {
                    var row = CreateUIItem(n);
                    _rows[n] = row;
                }
            }

            statusTick += listRefreshInterval;
            if (statusTick >= statusRefreshInterval)
            {
                foreach (var kv in _rows) kv.Value.RefreshStatus();
                statusTick = 0f;
            }
        }
    }

    private UIControlRow CreateUIItem(string objectName)
    {
        var uiItem = Instantiate(controlItemPrefab, uiPanelRoot);
        var row = uiItem.GetComponent<UIControlRow>();
        if (row == null) row = uiItem.AddComponent<UIControlRow>();
        row.Bind(objectName);
        return row;
    }

    // Tabs setup/raycast -----------------------------------------------------

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
            canvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private void InitTabs()
    {
        if (tabGroup == null)
        {
            Transform parent = null;
            if (tabAllToggle != null) parent = tabAllToggle.transform.parent;
            else if (tabGeneratedToggle != null) parent = tabGeneratedToggle.transform.parent;
            else if (tabRealObjectToggle != null) parent = tabRealObjectToggle.transform.parent;

            if (parent != null)
                tabGroup = parent.GetComponent<ToggleGroup>() ?? parent.gameObject.AddComponent<ToggleGroup>();
        }

        AssignToGroup(tabAllToggle);
        AssignToGroup(tabGeneratedToggle);
        AssignToGroup(tabRealObjectToggle);

        if (tabAllToggle != null)
        {
            tabAllToggle.onValueChanged.RemoveAllListeners();
            tabAllToggle.onValueChanged.AddListener(on => { if (on) SwitchTab(Tab.All); });
            if (!tabGeneratedToggle && !tabRealObjectToggle) tabAllToggle.isOn = true;
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

/// <summary>
/// UI logic per row. Finds child widgets by name:
/// NameText, StatusText, PlayButton, StopButton, ParamUIButton.
/// Uses LuaNetRPC if present, else falls back to LuaMonoBehavior.
/// </summary>
public class UIControlRow : MonoBehaviour
{
    private TMP_Text _name;
    private TMP_Text _status;
    private Button _playBtn;
    private Button _stopBtn;
    private Button _paramBtn;

    private string _targetName;
    private GameObject _go;
    private LuaMonoBehavior _lua;
    private LuaNetRPC _rpc;

    public void Bind(string objectName)
    {
        _name    = transform.Find("NameText")?.GetComponent<TMP_Text>();
        _status  = transform.Find("StatusText")?.GetComponent<TMP_Text>();
        _playBtn = transform.Find("PlayButton")?.GetComponent<Button>();
        _stopBtn = transform.Find("StopButton")?.GetComponent<Button>();
        _paramBtn= transform.Find("ParamUIButton")?.GetComponent<Button>();

        _targetName = objectName;
        if (_name != null) _name.text = objectName;
        if (_status != null) _status.text = "Idle";

        ResolveTarget();

        if (_playBtn != null) _playBtn.onClick.AddListener(OnPlay);
        if (_stopBtn != null) _stopBtn.onClick.AddListener(OnStop);
        // if (_paramBtn != null) _paramBtn.onClick.AddListener(() => _lua?.ShowParameterUI());

        UpdateInteractable();
    }

    public void RefreshStatus()
    {
        if (_go == null) ResolveTarget();
        if (_status == null) return;

        if (_go == null)
        {
            _status.text = "Not Found";
            UpdateInteractable();
            return;
        }

        if (_rpc != null)
        {
            _status.text = _rpc.IsRunning ? "Playing" : "Stopped";
        }
        else if (_lua != null)
        {
            try { _status.text = _lua.isRunning ? "Playing" : "Stopped"; }
            catch { /* if not exposed, keep last text */ }
        }
        else
        {
            _status.text = "No Lua";
        }

        UpdateInteractable();
    }

    private void OnPlay()
    {
        ResolveTarget();
        if (_rpc != null) _rpc.PlaySynced(true);
        else if (_lua != null) _lua.Play();
        if (_status != null) _status.text = "Playing";
    }

    private void OnStop()
    {
        ResolveTarget();
        if (_rpc != null) _rpc.PlaySynced(false);
        else if (_lua != null) _lua.Stop();
        if (_status != null) _status.text = "Stopped";
    }

    private void ResolveTarget()
    {
        if (_go != null) return;
        _go  = GameObject.Find(_targetName);
        _lua = _go ? _go.GetComponent<LuaMonoBehavior>() : null;
        _rpc = _go ? _go.GetComponent<LuaNetRPC>() : null;
    }

    private void UpdateInteractable()
    {
        bool ok = _go != null && (_rpc != null || _lua != null);
        if (_playBtn != null) _playBtn.interactable = ok;
        if (_stopBtn != null) _stopBtn.interactable = ok;
        if (_paramBtn != null) _paramBtn.interactable = ok;
    }
}
