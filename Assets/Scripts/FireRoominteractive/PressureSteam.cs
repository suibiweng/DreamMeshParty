using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;
using Oculus.Interaction;
public class PressureSteam : MonoBehaviour
{
    public Exsync exsync;
    public GameObject targetObject; // The GameObject to activate/deactivate


    public RealityEditorManager REM;

   public Grabbable grabbable;

    

    void Start(){

        REM=FindAnyObjectByType<RealityEditorManager>();

        grabbable.WhenPointerEventRaised += HandlePointerEventRaised;




    }

    public bool isGrabing; 


        private void HandlePointerEventRaised(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
               

                isGrabing=true;
                break;
            case PointerEventType.Unselect:
         
                 isGrabing=false;

                break;
        }
    }



    void Update()
    {
        // Check if both grab and trigger are pressed for either hand (left or right)
        bool leftHandActive = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger) && OVRInput.Get(OVRInput.Button.PrimaryHandTrigger)&& isGrabing ;
        bool rightHandActive = OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger) && OVRInput.Get(OVRInput.Button.SecondaryHandTrigger) && isGrabing;
     
        //if(REM.isFireScene==false)return;


        if (leftHandActive || rightHandActive)
        {
            ActivateObject();
              exsync.CallallSetEx();
        }
        else
        {
            DeactivateObject();
              exsync.CallallSetOffEx();
        }
    }

    // Method to activate the GameObject
   public void ActivateObject()
    {
        if (targetObject != null && !targetObject.activeSelf)
        {
            targetObject.SetActive(true); // Activate the GameObject
            Debug.Log("GameObject activated.");

          
        }
    }

    // Method to deactivate the GameObject
  public  void DeactivateObject()
    {
        if (targetObject != null && targetObject.activeSelf)
        {
            targetObject.SetActive(false); // Deactivate the GameObject
            Debug.Log("GameObject deactivated.");
           
        }
    }
}
