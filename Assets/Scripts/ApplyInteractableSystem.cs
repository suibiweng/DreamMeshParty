using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;

public class ApplyInteractableSystem : MonoBehaviour
{
    public RealityEditorManager Manager;
    public Camera interactiveCam;
    public GameObject objectToInstantiate; // Assign the GameObject to instantiate in the Inspector
    // Start is called before the first frame update
    void Start()
    {

        Manager=GetComponent<RealityEditorManager>();

        //((768, 204), (512, 870))

       // latest_bulb_coordinates = (650, 350)

// # Switch likely near the base (center-bottom of the lamp)
// latest_switch_coordinates = (520, 750)

    //    InstantiateAtPosition2D(new Vector2(512, 350));
    //      InstantiateAtPosition2D(new Vector2(512, 850));
        
    }


    GameObject spot;
    void Update()
    {
        spot=Manager.getSelectSpot();

        if(spot!=null){
            interactiveCam=spot.GetComponent<InteractableAssets>().targetCamera; 




        }else
        {
            return;

        }
 
      
    }

    public void InstantiateAtPosition2D(Vector2 screenPosition)
    {
        // Convert the 2D screen coordinates to a 3D ray
        Ray ray = interactiveCam.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Perform a raycast using the ray
        if (Physics.Raycast(ray, out hit))
        {
            // If the ray hits something, instantiate the object at the hit point
            Instantiate(objectToInstantiate, hit.point, Quaternion.identity);
        }
    }
}
