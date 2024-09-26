using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressureSteam : MonoBehaviour
{
    public GameObject targetObject; // The GameObject to activate/deactivate
    
    void Update()
    {
        // Check if both grab and trigger are pressed for either hand (left or right)
        bool leftHandActive = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
        bool rightHandActive = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) && OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

        if (leftHandActive || rightHandActive)
        {
            ActivateObject();
        }
        else
        {
            DeactivateObject();
        }
    }

    // Method to activate the GameObject
    void ActivateObject()
    {
        if (targetObject != null && !targetObject.activeSelf)
        {
            targetObject.SetActive(true); // Activate the GameObject
            Debug.Log("GameObject activated.");
        }
    }

    // Method to deactivate the GameObject
    void DeactivateObject()
    {
        if (targetObject != null && targetObject.activeSelf)
        {
            targetObject.SetActive(false); // Deactivate the GameObject
            Debug.Log("GameObject deactivated.");
        }
    }
}
