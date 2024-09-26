using UnityEngine;

public class ParticleLife : MonoBehaviour
{
    public int maxLife = 5;               // Maximum life of the particle system
    private int currentLife;              // Current life of the particle system    
    public ParticleSystem particle1;
    public ParticleSystem particle2;
    public ParticleSystem particle3;

    void Start()
    {
        // Initialize current life to max life
        currentLife = maxLife;

        //// Get the ParticleSystem component attached to this GameObject
        //particle1 = GetComponent<ParticleSystem>();
        //particle2 = GetComponent<ParticleSystem>();
        //particle3 = GetComponent<ParticleSystem>();

        if (particle1 == null)
        {
            Debug.LogError("No Particle1 attached to this GameObject.");
        }
        if (particle2 == null)
        {
            Debug.LogError("No Particle2 attached to this GameObject.");
        }
        if (particle3 == null)
        {
            Debug.LogError("No Particle3 attached to this GameObject.");
        }
    }

    // Method to decrease life when hit
    public void TakeDamage(int damage)
    {
        currentLife -= damage;
        Debug.Log(gameObject.name + " took damage, remaining life: " + currentLife);

        // Check if life has reached zero
        if (currentLife <= 0)
        {
            StopParticleEffect();
        }
    }

    // Method to stop the particle system
    private void StopParticleEffect()
    {
        if (particle1 != null && particle1.isPlaying)
        {
            particle1.Stop();
            Debug.Log(gameObject.name + " particle 1 stopped.");
        }
        if (particle2 != null && particle2.isPlaying)
        {
            particle2.Stop();
            Debug.Log(gameObject.name + " particle 2 stopped.");
        }
        if (particle3 != null && particle3.isPlaying)
        {
            particle3.Stop();
            Debug.Log(gameObject.name + " particle 3 stopped.");
        }

    }
}
