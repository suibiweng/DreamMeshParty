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
    public SceneSessionManager sceneSessionManager;


    private void Awake()
    {
        manager = GetComponent<RealityEditorManager>();
        sceneSessionManager = FindAnyObjectByType<SceneSessionManager>();


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
            GameObject gc = manager.createSpot(anchor.transform.position);
            gc.GetComponent<GenerateSpot>().Prompt = anchor.gameObject.name;
            gc.tag = "RealObject";

            if (sceneSessionManager != null)
            {

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
