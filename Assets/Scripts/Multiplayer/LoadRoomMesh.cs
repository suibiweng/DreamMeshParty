using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;


public class LoadRoomMesh : MonoBehaviour
{
    // private bool roomMeshEnabled = false;
    // public GameObject  EffectMesh;
  


    // Update is called once per frame
    void Update()
    {
        if(OVRInput.GetUp(OVRInput.RawButton.RThumbstick))
        {
            LoadRoomMeshFromDevice();


        }
    }


    public void LoadRoomMeshFromDevice()
    {
        Debug.Log("Setting the Room mesh parts to active");
        MRUK.Instance.LoadSceneFromDevice();
    }
}