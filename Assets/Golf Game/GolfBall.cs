using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolfBall : MonoBehaviour
{
    
    private AudioSource audioSource;
    public AudioClip golfSound;
    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();  

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GolfHole"))
        {
            audioSource.PlayOneShot(golfSound);
        }
      
        //need to move the hole to a new spot using the find spawn positions script
    }
}
