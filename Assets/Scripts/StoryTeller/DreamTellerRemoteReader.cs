using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using RealityEditor;

public class Vector3Data
{
    public float x, y, z;
}

public class TransformData
{
    public Vector3Data position;
    public Vector3Data rotation;
    public Vector3Data size;
}

public class MRObject
{
    public string name;
    public string id;
    public string url;
    public TransformData transform;
}

public class DreamTellerRemoteReader : MonoBehaviour
{
    [Header("Server Settings")]
    public string serverIP = "192.168.0.25";
    public string port = "8000";
    public string sessionName = "StoryTest";

    RealityEditorManager realityEditorManager;  


    public GameObject GenObj; 

    [Header("Retry Settings")]
    public float retryInterval = 2f; // seconds

    void Start()
    {

        realityEditorManager = FindObjectOfType<RealityEditorManager>();
        serverIP = realityEditorManager.ServerURL;
        port = realityEditorManager.downloadPort;
        // StartCoroutine(PollUntilReady(sessionName));
    }


    public void GenerateScene(string URLID){


        StartCoroutine(PollUntilReady(URLID));




    }





    IEnumerator PollUntilReady(string session)
    {
        string fileName = $"{session}_DreamTeller_Ready.json";
        string url = $"http://{serverIP}:{port}/{fileName}";

        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[Ready JSON Found] {fileName}");
                    string json = request.downloadHandler.text;

                    List<MRObject> objectList = JsonConvert.DeserializeObject<List<MRObject>>(json);

                    foreach (var obj in objectList)
                    {
                        Vector3 pos = ToVector(obj.transform.position);
                        Vector3 rot = ToVector(obj.transform.rotation);
                        Vector3 scale = ToVector(obj.transform.size);
                        string objurl= obj.url;

                        GameObject genobj=Instantiate(GenObj,pos,Quaternion.Euler(rot));

                        genobj.GetComponent<StorySpot>().startDownload(obj.url);

                        genobj.transform.localScale=scale;





                        Debug.Log($"[✓] {obj.name} ({obj.id})");
                        Debug.Log($"     Position: {pos}, Rotation: {rot}, Scale: {scale}");
                    }

                    yield break; // ✅ Stop polling
                }
                else
                {
                    Debug.Log($"[Waiting] File not ready yet... Retrying in {retryInterval} seconds");
                    yield return new WaitForSeconds(retryInterval);
                }
            }
        }
    }

    Vector3 ToVector(Vector3Data v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
}
