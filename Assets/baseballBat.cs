using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class BaseballBat : NetworkBehaviour
{
    // Cache pending hit to apply when authority is granted
    private struct PendingHit
    {
        public NetworkObject netObj;
        public Vector3 force;
    }

    private List<PendingHit> pendingHits = new List<PendingHit>();

    private void OnCollisionEnter(Collision other)
    {
        // if (!Object.HasInputAuthority)
        //     return;

        Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
        // if (rb == null)
        //     return;

        NetworkObject netObj = other.gameObject.GetComponent<NetworkObject>();

        Vector3 hitDir = -other.contacts[0].normal;
        Vector3 force = hitDir * 5f;

        if (netObj != null)
        {
            if (netObj.HasStateAuthority)
            {
                // Already have authority, apply force
                rb.AddForce(force, ForceMode.Impulse);
            }
            else
            {
                // Request authority
                netObj.RequestStateAuthority();

                // Queue the hit to apply later
                pendingHits.Add(new PendingHit
                {
                    netObj = netObj,
                    force = force
                });
            }
        }
        else
        {
            // Not a network object, just apply
            rb.AddForce(force, ForceMode.Impulse);
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
