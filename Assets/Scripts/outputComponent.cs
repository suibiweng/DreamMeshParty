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

        //light=GetComponent<Light>();
        //particleSystem=GetComponent<ParticleSystem>();
        //Sound=GetComponent<AudioSource>();
               
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
                    }
                }
                else
                {
                    if (interactableAssets.interactableDreamMesh.output_style == 0)
                    {
                        //gun
                        TogglegunState();

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

    // Method to start the Sound
    void PlaySound()
    {
        Sound.Play();
        isSoundPlaying = true;
        Debug.Log("Sound started.");
    }

    // Method to stop the Sound
    void StopSound()
    {
        Sound.Stop();
        isSoundPlaying = false;
        Debug.Log("Sound stopped.");
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
