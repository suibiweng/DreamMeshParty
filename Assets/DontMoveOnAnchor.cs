using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DontMoveOnAnchor : MonoBehaviour
{
    private Vector3 recordedPosition;
    private Transform RoomMesh; 
    
    public float moveSpeed = 1.0f;
    public float rotateSpeed = 45f; // degrees per second
 
    public void RecordPosition()
    {
        RoomMesh =  GameObject.Find("MeshVolume").transform;
        recordedPosition = RoomMesh.transform.position;
        Debug.Log($"{gameObject.name} position recorded: {recordedPosition}");
    }
    
    public void RestorePosition()
    {
        RoomMesh.transform.position = recordedPosition;
        Debug.Log($"{gameObject.name} position restored to: {recordedPosition}");
    }

    private void Update()
    {
        if (RoomMesh != null)
        {
            // Move input
            Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);
            float moveX = moveInput.x;
            float moveZ = moveInput.y;

            // Rotate input
            float rotateInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).x;

            // Move
            Vector3 pos = RoomMesh.position;
            pos += new Vector3(moveX, 0, moveZ) * moveSpeed * Time.deltaTime;
            RoomMesh.position = pos;

            // Rotate
            Vector3 eulerAngles = RoomMesh.eulerAngles;
            eulerAngles.y += rotateInput * rotateSpeed * Time.deltaTime;
            RoomMesh.rotation = Quaternion.Euler(eulerAngles);
        }
    }
}
