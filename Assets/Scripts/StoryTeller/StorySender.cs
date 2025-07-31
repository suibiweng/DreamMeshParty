using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using RealityEditor;
using UnityEngine.UI;

public class StorySender : MonoBehaviour
{
    public string serverUrl = "http://192.168.0.139:5000/StoryGenerator";  // Change to your Flask server URL

    public DreamTellerRemoteReader dreamTellerRemoteReader;

    RealityEditorManager realityEditorManager;

    public InputField storyText;
    
    public string ModelType;

    public Toggle modelType;

    void Start()
    {
        realityEditorManager = FindObjectOfType<RealityEditorManager>();
        serverUrl = realityEditorManager.ServerURL + ":" + realityEditorManager.uploadPort + "/StoryGenerator";

    }





    void upadateModelType(){


       ModelType= modelType.isOn? "ChatGPT":"Text23D";



    }



    
    public void DebugSendStory()
    {

         upadateModelType();

        string urlid= IDGenerator.GenerateID();
        StartCoroutine(PostStoryCoroutine("A bear infront of a house", urlid,ModelType));
        dreamTellerRemoteReader.GenerateScene(urlid);

    }


    public void SendInputStory(){


         upadateModelType();

      


        string urlid= IDGenerator.GenerateID();
        StartCoroutine(PostStoryCoroutine( storyText.text, urlid,ModelType));
        dreamTellerRemoteReader.GenerateScene(urlid);




    }






    public void SendStory(string text)

    {

         upadateModelType();
        string urlid= IDGenerator.GenerateID();
        StartCoroutine(PostStoryCoroutine(text, urlid,"ChatGPT"));
        dreamTellerRemoteReader.GenerateScene(urlid);

    }

    IEnumerator PostStoryCoroutine(string story, string urlid,string type)
    {
        WWWForm form = new WWWForm();
        form.AddField("Story", story);
        form.AddField("URLID", urlid);
        form.AddField("type",type);

        using (UnityWebRequest request = UnityWebRequest.Post(serverUrl, form))
        {
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[StoryGenerator] Response: " + request.downloadHandler.text);

                // Optional: Parse JSON result
                // JSONObject obj = new JSONObject(request.downloadHandler.text);
                // do something with obj
            }
            else
            {
                Debug.LogError("[StoryGenerator] Error: " + request.error);
            }
        }
    }
}
