using UnityEngine;

public class RaycastShooter : MonoBehaviour
{
    public float rayDistance = 100f;      // Distance of the raycast
    public LayerMask targetLayerMask;     // Layer mask to filter which objects can be hit by the ray

    void Update()
    {
        // Check if both grab and trigger are pressed for either hand (left or right)
        bool leftHandActive = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightHandActive = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) && OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);


        if (leftHandActive || rightHandActive)
        //if(OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
        {
            
            // Cast a ray from the object's position forward
            Ray ray = new Ray(transform.position, -transform.right);
            RaycastHit hit;
            //Debug.Log("pressed");

            // Perform the raycast
            if (Physics.Raycast(ray, out hit, rayDistance, targetLayerMask))
            {
                // Check if the hit object has the ParticleLife component
                FireParticleLife particleLife = hit.collider.GetComponent<FireParticleLife>();

                if (particleLife != null)
                {
                    // Call the method to reduce life of the particle system
                    particleLife.TakeDamage(1);  // Reduce by 1 life point
                }
            }
        }
    }

    // Optional: visualize the ray in the Unity editor
    // void OnDrawGizmos()
    // {
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawRay(transform.position, -transform.right * rayDistance);
    // }
}
