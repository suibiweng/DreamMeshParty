using System;
using UnityEngine;

public class ControllerLaserPointer : MonoBehaviour
{
    public LineRenderer lineRenderer;      // Assign a LineRenderer in the inspector
    public LayerMask keyboardLayer;        // Set this to the keyboard layer in the inspector

    public float maxDistance = 5f;         // Max raycast distance
    

    void Update()
    {
        // Cast a ray from the controller
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance, keyboardLayer))
        {
            // Show and update the laser if we hit the keyboard
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);  // Start point
            lineRenderer.SetPosition(1, hit.point);  // End point (where it hits)
        }
        else
        {
            // Hide the laser if not pointing at the keyboard
            lineRenderer.enabled = false;
        }
    }
}