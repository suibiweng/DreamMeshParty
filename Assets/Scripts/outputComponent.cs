using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class outputComponent : MonoBehaviour
{
    
    public InteractableAssets interactableAssets;

    
    public ParticleSystem light;
    public ParticleSystem gun;
    public AudioSource Sound;

    // public InteractableDreamMesh interactableDreamMesh;

    private bool isGunPlaying = false;
    private bool isSoundPlaying = false;
    private bool isLightPlaying = false;


    public float lightOnDuration = 1f; // How long the light stays on, in seconds



    // Start is called before the first frame update
    void Start()
    {


               
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void triggerOutPut(){

        if(interactableAssets.interactableDreamMesh.input_style==0){ //is trigger
            if(interactableAssets.interactableDreamMesh.trigger_style==1){
                //contious
                //output type and Behaviour
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) )
                {
                    if (interactableAssets.interactableDreamMesh.output_style == 0)
                    {
                        //gun
                        TogglegunState();
                        ToggleSoundState();

                    }
                    if (interactableAssets.interactableDreamMesh.output_style == 1)
                    {
                        //sound
                        ToggleSoundState();

                    }
                    if (interactableAssets.interactableDreamMesh.output_style == 2)
                    {
                        //light
                        ToggleLightState();
                        ToggleSoundState();
                    }
                }
                else
                {
                    if (interactableAssets.interactableDreamMesh.output_style == 0)
                    {
                        //gun
                        TogglegunState();
                        ToggleSoundState();

                    }
                    if (interactableAssets.interactableDreamMesh.output_style == 1)
                    {
                        //sound
                        ToggleSoundState();
                        
                    }
                    if (interactableAssets.interactableDreamMesh.output_style == 2)
                    {
                        //light
                        ToggleLightState();
                        ToggleSoundState();
                    }
                }
                
            }

            if(interactableAssets.interactableDreamMesh.trigger_style==2){
                //one shot
                //output type and Behaviour
                if (interactableAssets.interactableDreamMesh.output_style == 0)
                {
                    //gun
                    StartCoroutine(PlayGunForOneSecond());
                }
                if (interactableAssets.interactableDreamMesh.output_style == 1)
                {
                    //sound
                    StartCoroutine(PlaySoundForOneSecond());
                }
                if (interactableAssets.interactableDreamMesh.output_style == 2)
                {
                    //light
                    if (!isLightPlaying)
                    {
                        StartCoroutine(ToggleLightForDuration());
                    }
                    
                }


            }




        }else if(interactableAssets.interactableDreamMesh.input_style==1){ // is switch
            //output type and Behaviour
            if ( OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)  )
            {
                if (interactableAssets.interactableDreamMesh.output_style == 0)
                {
                    //gun
                    TogglegunState();
                    ToggleSoundState();

                }
                if (interactableAssets.interactableDreamMesh.output_style == 1)
                {
                    //sound
                    ToggleSoundState();
                }
                if (interactableAssets.interactableDreamMesh.output_style == 2)
                {
                    //light
                    ToggleLightState();
                    ToggleSoundState();
                }
            }
        }
             
    }



bool audioLoaded =false;

        IEnumerator StreamAudioFromUrl(string audioUrl)
    {
        bool audioLoadedSuccessfully = false;

        while (!audioLoadedSuccessfully) // Keep looping until audio is loaded
        {
            using (var www = new WWW(audioUrl))
            {
                yield return www;

                if (string.IsNullOrEmpty(www.error))
                {
                    Sound.clip = www.GetAudioClip(false, true, AudioType.WAV);
                    audioLoaded = true;
                    audioLoadedSuccessfully = true; // Exit loop when audio is successfully loaded
                    Debug.Log("Audio loaded and ready to play.");
                }
                else
                {
                    Debug.LogError("Error loading audio: " + www.error);
                    yield return new WaitForSeconds(5); // Wait for 5 seconds before retrying
                }
            }
        }
    }

    // Method to toggle the gun on or off
    void TogglegunState()
    {
        if (gun != null)
        {
            if (isGunPlaying)
            {
                Stopgun();
            }
            else
            {
                Playgun();
            }
        }
    }

    // Method to start the gun
    void Playgun()
    {
        gun.Play();
        isGunPlaying = true;
        Debug.Log("gun started.");
    }

    // Method to stop the gun
    void Stopgun()
    {
        gun.Stop();
        isGunPlaying = false;
        Debug.Log("gun stopped.");
    }

    // Method to toggle the Sound on or off
    void ToggleSoundState()
    {
        if(!audioLoaded)
        StartCoroutine(StreamAudioFromUrl(interactableAssets.audioUrl));


        if (Sound != null)
        {
            if (isGunPlaying)
            {
                StopSound();
            }
            else
            {
                PlaySound();
            }
        }
    }

    // Play the audio if it is loaded
    public void PlaySound()
    {
        if (audioLoaded && Sound.clip != null)
        {
            if (Sound.isPlaying)
            {
                Sound.Stop(); // Stop if it's already playing (optional)
            }
            Sound.Play(); // Play the audio clip
            Debug.Log("Playing sound.");
        }
        else
        {
            Debug.Log("Audio is not loaded yet.");
        }
    }

    // Stop the audio if it's playing
    public void StopSound()
    {
        if (Sound.isPlaying)
        {
            Sound.Stop(); // Stop the audio clip
            Debug.Log("Sound stopped.");
        }
        else
        {
            Debug.Log("No sound is currently playing.");
        }
    }





    // Method to toggle the light on or off
    void ToggleLightState()
    {
        if (light != null)
        {
            if (isLightPlaying)
            {
                TurnOffLight();
            }
            else
            {
                TurnOnLight();
            }
        }
    }
    IEnumerator PlayGunForOneSecond()
    {
        // Start the gun
        if (gun != null)
        {
            gun.Play(); // Start gun

            // Wait for 1 second
            yield return new WaitForSeconds(1f);

            // Stop the gun after 1 second
            gun.Stop();
        }
    }
    IEnumerator PlaySoundForOneSecond()
    {
        // Start the sound
        if (Sound != null)
        {
            Sound.Play(); // Start sound

            // Wait for 1 second
            yield return new WaitForSeconds(1f);

            // Stop the sound after 1 second
            Sound.Stop();
        }
    }

    IEnumerator ToggleLightForDuration()
    {
        // Turn on the light
        TurnOnLight();

        // Wait for the specified duration
        yield return new WaitForSeconds(lightOnDuration);

        // Turn off the light
        TurnOffLight();
    }

    // Method to turn on the light
    void TurnOnLight()
    {
        light.Play();
        isLightPlaying = true;
        Debug.Log("light started.");
    }

    // Method to turn off the light
    void TurnOffLight()
    {
        light.Stop();
        isLightPlaying = false;
        Debug.Log("light stopped.");
    }
}
