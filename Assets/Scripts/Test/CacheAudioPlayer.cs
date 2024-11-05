using System.IO;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CachedAudioPlayer : MonoBehaviour
{
    public string audioUrl = "http://localhost:8080/sound.wav"; // Your remote audio URL
    private string localFilePath;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.GetComponent<AudioSource>();
        // localFilePath = Path.Combine(Application.persistentDataPath, "cachedAudio.wav");
        StartCoroutine(DownloadAndPlayAudio(audioUrl));
    }



    void Update()   {
        if(Input.GetKeyDown(KeyCode.Space)){

            audioSource.Play();
        }



    }

IEnumerator DownloadAndPlayAudio(string url)
{
    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG)) // MPEG for MP3
    {
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(www.error);
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}


    IEnumerator LoadAndPlayAudio(string path)
    {
        // Load the audio from the file path
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(www.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }
}
