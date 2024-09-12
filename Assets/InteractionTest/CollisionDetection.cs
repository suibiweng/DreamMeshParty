using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    // Reference to the component that will handle emission and audio
    public EmissionControl emissionControl;
    public AudioControl audioControl;

    void OnCollisionEnter(Collision collision)
    {
        // Handle collision logic for controlling emission
        if (emissionControl != null)
        {
            emissionControl.ToggleEmission(); // Toggle emission effect on collision
        }

        // Handle collision logic for controlling audio
        if (audioControl != null)
        {
            audioControl.ToggleAudio(); // Toggle audio playback on collision
        }

        Debug.Log("Collision detected with: " + collision.gameObject.name);
    }
}
