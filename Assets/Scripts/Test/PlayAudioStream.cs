using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlayAudioStream : MonoBehaviour
{
public string url = "http://localhost:8080/sound.wav"; // Replace with your local server URL
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>(); // Add AudioSource to this GameObject
     
    }

    void Update(){

    if(Input.anyKeyDown){


        //StartCoroutine(DownloadAndPlayWav(url));
       StartCoroutine( PlayAudioFromURL(url));



    }




    }



        IEnumerator PlayAudioFromURL(string url)
    {
        // Start downloading the audio file
        WWW www = new WWW(url);
        yield return www;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("Error downloading audio: " + www.error);
        }
        else
        {
            // Assign the downloaded clip to the AudioSource
            audioSource.clip = www.GetAudioClip(false, false); // Streaming set to false
            audioSource.Play(); // Play the audio
        }
    }




    IEnumerator DownloadAndPlayWav(string url)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
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
