using System;
using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

public class OVRSnapSocket : MonoBehaviour
{
    public string acceptableTag = "Snappable"; // Tag to identify acceptable objects
    private Grabbable _currentGrabbable;
    public Transform WearingClothsPos; 

    private void Start()
    {
        GameObject.FindGameObjectWithTag("Hat");
        
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("On trigger enter in the hat attach script");
        if (other.CompareTag(acceptableTag))
        {
            AttachObject(other.gameObject);
            Debug.Log("Likely attached the hat");

        }
    }

    private void AttachObject(GameObject grabbable)
    {
        // Release the object from any current grabbers

        // Set parent to the socket
        grabbable.transform.SetParent(WearingClothsPos, true);
        grabbable.transform.localPosition = Vector3.zero;
        grabbable.transform.localRotation = Quaternion.identity;

        // Make the object kinematic
        Rigidbody rb = grabbable.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    

    
}
