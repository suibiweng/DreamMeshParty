using UnityEngine;

public class AudioControl : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip audioClip;
    private bool isAudioPlaying = false;

    // Initialize the AudioControl with an AudioClip
    public void Initialize(GameObject effectObject, AudioClip clip)
    {
        // Add and configure the AudioSource component
        //audioSource = effectObject.AddComponent<AudioSource>();
        audioClip = clip;
        audioSource.clip = audioClip;
        audioSource.playOnAwake = false;

        // Enable looping for the audio
        audioSource.loop = true;
        audioSource.enabled = true; // Ensure AudioSource is enabled
    }

    // Method to play the audio
    public void PlayAudio()
    {
        if (audioSource != null && audioClip != null && !audioSource.isPlaying)
        {
            audioSource.enabled = true; // Ensure AudioSource is enabled
            audioSource.Play();
            isAudioPlaying = true;
            Debug.Log("Audio started.");
        }
    }

    // Method to stop the audio
    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            isAudioPlaying = false;
            Debug.Log("Audio stopped.");
        }
    }

    // Method to toggle audio playback
    public void ToggleAudio()
    {
        if (isAudioPlaying)
        {
            StopAudio();
        }
        else
        {
            PlayAudio();
        }
    }
}
