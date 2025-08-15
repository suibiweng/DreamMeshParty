using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using RealityEditor;
using Oculus.Platform;
using Collada141;


public class RoomObjectsManager : MonoBehaviour
{
    public MRUKAnchor[] objectsinRoom;
    public RealityEditorManager manager;
    public SceneSessionManager sceneSessionManager;
    public SceneSaverTest SceneSaverTest; //I'm Taking over this code, MUHAHAHA

    private void Awake()
    {
        manager = GetComponent<RealityEditorManager>();
        SceneSaverTest = FindAnyObjectByType<SceneSaverTest>();
        sceneSessionManager = FindAnyObjectByType<SceneSessionManager>();


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

        foreach (MRUKAnchor anchor in objectsinRoom)
        {

            Collider col = anchor.GetComponentInChildren<Collider>();






            // Setup each anchor as needed
            GameObject gc = manager.createRealobjectSpot(anchor.transform.position, anchor.transform.localScale);
            gc.GetComponent<GenerateSpot>().Prompt = anchor.gameObject.name;
            gc.tag = "RealObject";
            gc.name = anchor.gameObject.name;

            gc.GetComponent<GenerateSpot>().Outlinebox.enabled = false;
            gc.GetComponent<GenerateSpot>().selectMenu.SetActive(false);
            gc.GetComponent<GenerateSpot>().isRealObject = true; // Mark this as a real object spot

            col.transform.parent = gc.transform;

            var collider = col.GetComponent<Collider>();
            gc.GetComponent<LuaMonoBehavior>().innerCollider = collider;
            var boxCollider= gc.GetComponent<GenerateSpot>().boxCollider;
            boxCollider.enabled = false; // Disable the box collider for the generated spot 
            collider.gameObject.layer = LayerMask.NameToLayer("GeneratedObject");



            

            // Add any additional setup for the generated spot here


            // Add any additional setup for the generated spot here





            sceneSessionManager.addSceneObject(new SceneSessionManager.SceneObjectData
                {
                    id = gc.GetComponent<GenerateSpot>().URLID,
                    name = anchor.gameObject.name,
                    position = anchor.transform.position,
                    rotation = anchor.transform.rotation.eulerAngles

                }
                  );


            


        }


    }



    // Start is called before the first frame update
    void Start()
    {
                StartCoroutine(DelaytoCreateSpots());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
