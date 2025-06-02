using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DistanceFadeUI : MonoBehaviour
{
    private CanvasGroup CanvasGroup;
    private Transform playerTransform;
    
    // The minimum distance for the UI element to be fully visible
    public float minDistance = 2f;

    // The maximum distance for the UI element to be fully transparent
    public float maxDistance = 10f;
    private void Start()
    {
        CanvasGroup = GetComponent<CanvasGroup>(); 
        playerTransform=Camera.main.gameObject.transform;

    }

    private void Update()
    {
        // Calculate distance between the player and the canvas
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        // Calculate the opacity based on the distance, clamping it between 0 and 1
        float opacity = Mathf.Clamp01((maxDistance - distance) / (maxDistance - minDistance));

        CanvasGroup.alpha = opacity; 
    }

    private void UpdateUIElementOpacity(Graphic uiElement)
    {
        if (uiElement == null || playerTransform == null) return;

        // Calculate distance between the player and the UI element
        float distance = Vector3.Distance(playerTransform.position, uiElement.transform.position);

        // Calculate the opacity based on the distance, clamping it between 0 and 1
        float opacity = Mathf.Clamp01((maxDistance - distance) / (maxDistance - minDistance));

        // Update the opacity of the UI element
        Color color = uiElement.color;
        color.a = opacity;
        uiElement.color = color;
    }
    private void UpdateUIElementScale(RectTransform uiElement)
    {
        if (uiElement == null || playerTransform == null) return;
        float distance = Vector3.Distance(playerTransform.position, uiElement.transform.position);
        float opacity = Mathf.Clamp01((maxDistance - distance) / (maxDistance - minDistance));
        
    }
}
