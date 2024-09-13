using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class outputComponent : MonoBehaviour
{
    
    public InteractableAssets interactableAssets;

    
    public Light light;
    public ParticleSystem particleSystem;
    public AudioSource Sound;

    // public InteractableDreamMesh interactableDreamMesh;

    private bool isParticleSystemPlaying = false;
    private bool isSoundPlaying = false;
    
    public float lightOnDuration = 1f; // How long the light stays on, in seconds
    private bool isLightOn = false; // Track if the light is currently on



    // Start is called before the first frame update
    void Start()
    {

        light=GetComponent<Light>();
        particleSystem=GetComponent<ParticleSystem>();
        Sound=GetComponent<AudioSource>();







        
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
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) && interactableAssets.generateSpot.isselsected)
                {
                    if (interactableAssets.interactableDreamMesh.output_style == 0)
                    {
                        //particle
                        ToggleParticleSystemState();

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
                        //particle
                        ToggleParticleSystemState();

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
                    //particle
                    StartCoroutine(PlayParticleForOneSecond());
                }
                if (interactableAssets.interactableDreamMesh.output_style == 1)
                {
                    //sound
                    StartCoroutine(PlaySoundForOneSecond());
                }
                if (interactableAssets.interactableDreamMesh.output_style == 2)
                {
                    //light
                    if (!isLightOn)
                    {
                        StartCoroutine(ToggleLightForDuration());
                    }
                }


            }




        }else if(interactableAssets.interactableDreamMesh.input_style==1){ // is switch
            //output type and Behaviour
            if ( OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) || OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger) && interactableAssets.generateSpot.isselsected )
            {
                if (interactableAssets.interactableDreamMesh.output_style == 0)
                {
                    //particle
                    ToggleParticleSystemState();

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

    // Method to toggle the particle system on or off
    void ToggleParticleSystemState()
    {
        if (particleSystem != null)
        {
            if (isParticleSystemPlaying)
            {
                StopParticleSystem();
            }
            else
            {
                PlayParticleSystem();
            }
        }
    }

    // Method to start the particle system
    void PlayParticleSystem()
    {
        particleSystem.Play();
        isParticleSystemPlaying = true;
        Debug.Log("Particle system started.");
    }

    // Method to stop the particle system
    void StopParticleSystem()
    {
        particleSystem.Stop();
        isParticleSystemPlaying = false;
        Debug.Log("Particle system stopped.");
    }

    // Method to toggle the Sound on or off
    void ToggleSoundState()
    {
        if (Sound != null)
        {
            if (isParticleSystemPlaying)
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

    void ToggleLightState()
    {
        if (light != null)
        {
            if (isLightOn)
            {
                TurnOffLight();
            }
            else
            {
                TurnOnLight();
            }
        }
    }
    IEnumerator PlayParticleForOneSecond()
    {
        // Start the particle system
        if (particleSystem != null)
        {
            particleSystem.Play(); // Start particle system

            // Wait for 1 second
            yield return new WaitForSeconds(1f);

            // Stop the particle system after 1 second
            particleSystem.Stop();
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
        if (light != null)
        {
            light.enabled = true;
            isLightOn = true;
            Debug.Log("Light turned on.");
        }
    }

    // Method to turn off the light
    void TurnOffLight()
    {
        if (light != null)
        {
            light.enabled = false;
            isLightOn = false;
            Debug.Log("Light turned off.");
        }
    }
}
