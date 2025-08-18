using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GolfClub : MonoBehaviour
{
    public float forceMultiplier = 1.5f; // Tweak this to get the right feel
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    
    private struct PendingHit
    {
        public NetworkObject netObj;
        public Vector3 force;
    }

    private List<GolfClub.PendingHit> pendingHits = new List<GolfClub.PendingHit>();

    void Start()
    {
        previousPosition = transform.position;
    }

    void FixedUpdate()
    {
        // Calculate current linear velocity
        currentVelocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;
    }
    
    
    void OnCollisionEnter(Collision collision)
    {
        

        Rigidbody ballRb = collision.gameObject.GetComponent<Rigidbody>();
        
        NetworkObject netObj = collision.gameObject.GetComponent<NetworkObject>();

        if (ballRb != null)
        {
            // Vector3 impulse = currentVelocity * forceMultiplier;
            Vector3 hitDir = -collision.contacts[0].normal;
            Vector3 impulse = hitDir * 100f;
            ballRb.AddForce(impulse, ForceMode.Impulse);

            if (netObj != null)
            {
                if (netObj.HasStateAuthority)
                {
                    // Already have authority, apply force
                    ballRb.AddForce(impulse, ForceMode.Impulse);
                }
                else
                {
                    // Request authority
                    netObj.RequestStateAuthority();

                    // Queue the hit to apply later
                    pendingHits.Add(new PendingHit
                    {
                        netObj = netObj,
                        force = impulse
                    });
                }
            }
            else
            {
                // Not a network object, just apply
                ballRb.AddForce(impulse, ForceMode.Impulse);
            }
        }
    }
    
    public void Update()
    {
        // Debug.Log("FixedUpdateNetwork");
        // Process any pending hits if we now have authority
        for (int i = pendingHits.Count - 1; i >= 0; i--)
        {
            var pending = pendingHits[i];
            if (pending.netObj != null && pending.netObj.HasStateAuthority)
            {
                Rigidbody rb = pending.netObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(pending.force, ForceMode.Impulse);
                }

                pendingHits.RemoveAt(i);
            }
        }
    }
    
}
