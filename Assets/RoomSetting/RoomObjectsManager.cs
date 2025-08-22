using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using RealityEditor;
using Fusion;
using UnityEngine.SceneManagement;

public class RoomObjectsManager : MonoBehaviour
{
    public MRUKAnchor[] objectsinRoom;
    public RealityEditorManager manager;
    public SceneSessionManager sceneSessionManager;
    public SceneSaverTest SceneSaverTest;   // kept
    public NetworkRunner _runner;

private async void Awake()
{
    manager = GetComponent<RealityEditorManager>();
    SceneSaverTest = FindAnyObjectByType<SceneSaverTest>();
    sceneSessionManager = FindAnyObjectByType<SceneSessionManager>();
    _runner = FindObjectOfType<NetworkRunner>();

    if (_runner == null)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
    }

    // If runner exists but is Shutdown, start it
    if (_runner.State == NetworkRunner.States.Shutdown)
    {
        Debug.Log("Starting NetworkRunner as Host...");
        var sceneMgr = _runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneMgr == null) sceneMgr = _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Host,       // this makes IsServer true
            SessionName = "EditorTest",
            Scene = SceneRef.None,          // stay in current scene
            SceneManager = sceneMgr
        });
    }
}


    

    public void InitRoomSession()
    {
        objectsinRoom = FindObjectsOfType<MRUKAnchor>();
        if (objectsinRoom.Length > 0)
        {
            SetuptheSpots();
        }
        else
        {
            Debug.LogWarning("No MRUKAnchor objects found in the scene.");
        }

        StartCoroutine(DelaytoCreateSpots());
    }

    IEnumerator DelaytoCreateSpots()
    {
        yield return new WaitForSeconds(30f);
        SetuptheSpots();
    }

public void SetuptheSpots()
{
    if (_runner != null && _runner.IsServer)
    {
        foreach (MRUKAnchor anchor in objectsinRoom)
        {
            if (anchor == null)
            {
                Debug.LogWarning("Null anchor in objectsinRoom, skipping.");
                continue;
            }

            Collider col = anchor.GetComponentInChildren<Collider>();
            if (col == null)
            {
                Debug.LogWarning($"Anchor {anchor.name} has no Collider, skipping.");
                continue;
            }

            // Setup each anchor as needed (network spawn handled inside manager)
            GameObject gc = manager.createRealobjectSpot(anchor.transform.position, anchor.transform.localScale);
            if (gc == null)
            {
                Debug.LogError("manager.createRealobjectSpot returned null, skipping.");
                continue;
            }

            var spot = gc.GetComponent<GenerateSpot>();
            if (spot == null)
            {
                Debug.LogError("Generated object missing GenerateSpot component, skipping.");
                continue;
            }

            // Local setup
            spot.Prompt = anchor.gameObject.name;
            gc.tag = "RealObject";
            gc.name = anchor.gameObject.name;

            if (spot.Outlinebox != null)
                spot.Outlinebox.enabled = false;

            if (spot.selectMenu != null)
                spot.selectMenu.SetActive(false);

            spot.isRealObject = true;

            // --- NEW: Sync to all clients via PhotonDataSync ---
            var sync = gc.GetComponent<PhotonDataSync>();
            if (sync != null && sync.HasStateAuthority)
            {
                sync.UpdatePrompt(anchor.gameObject.name);
                sync.UpdateIsRealObject(true);
                sync.UpdateOutline(false);
                sync.UpdateSelectMenu(false);
            }

            // Collider parenting stays local
            col.transform.parent = gc.transform;

            var collider = col.GetComponent<Collider>();
            var lua = gc.GetComponent<LuaMonoBehavior>();
            if (lua != null)
                lua.innerCollider = collider;

            if (spot.boxCollider != null)
                spot.boxCollider.enabled = false;

            collider.gameObject.layer = LayerMask.NameToLayer("Environment");

            if (sceneSessionManager != null)
            {
                sceneSessionManager.addSceneObject(new SceneSessionManager.SceneObjectData
                {
                    id = spot.URLID,
                    name = anchor.gameObject.name,
                    position = anchor.transform.position,
                    rotation = anchor.transform.rotation.eulerAngles
                });
            }
        }
    }
    else
    {
        Debug.Log("Not the server, so not setting up the spots.");
    }
}


    void Start()
    {
        StartCoroutine(DelaytoCreateSpots());
    }

    void Update()
    {
    }
}
