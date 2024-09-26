using UnityEngine;

public class ParticleCollisionDestroy : MonoBehaviour
{
    // The particle system that this object will detect collisions from
    public ParticleSystem particleSystem;

    void Start()
    {
        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }
    }

    // This function is called when particles collide with this object
    void OnParticleCollision(GameObject other)
    {
        // Check if this object collided with a valid particle
        if (other != null)
        {
            Debug.Log("Particle collided with " + gameObject.name);

            // Deactivate the GameObject this script is attached to
            gameObject.SetActive(false);
        }
    }
}