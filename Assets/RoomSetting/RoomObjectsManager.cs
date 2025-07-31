using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;
using RealityEditor;
using Oculus.Platform;


public class RoomObjectsManager : MonoBehaviour
{
    public MRUKAnchor[] objectsinRoom;
    public RealityEditorManager manager;
    // public SceneSessionManager sceneSessionManager;
    public SceneSaverTest SceneSaverTest; //I'm Taking over this code, MUHAHAHA

    private void Awake()
    {
        manager = GetComponent<RealityEditorManager>();
        SceneSaverTest = FindAnyObjectByType<SceneSaverTest>();


    }

    public void InitRoomSession()
    {
        objectsinRoom = FindObjectsOfType<MRUKAnchor>();
        if (objectsinRoom.Length > 0)
        {
            //SetuptheSpots();
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

            // Setup each anchor as needed
            GameObject gc = manager.createRealobjectSpot(anchor.transform.position);
            gc.GetComponent<GenerateSpot>().Prompt = anchor.gameObject.name;
            gc.tag = "RealObject";
            gc.name = anchor.gameObject.name;

            if (SceneSaverTest != null)
            {

                SceneSaverTest.addSceneObject(new SceneSaverTest.SceneObjectData
                {
                    id = gc.GetComponent<GenerateSpot>().URLID,
                    name = anchor.gameObject.name,
                    position = anchor.transform.position,
                    rotation = anchor.transform.rotation.eulerAngles

                }
                  );


            }


        }


    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
