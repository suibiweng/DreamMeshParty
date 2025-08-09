using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;
using UnityEngine.UI;



public class DrawingSystem : MonoBehaviour
{
    public Toggle DrawingMode;

    public  bool isDrawing=false;
    public  GameObject Stroke;
    public Transform pen;

    public GameObject Zerocanvas;

    float StrokeWidth=0.01f;

    Stack<GameObject> Strokestack ;

    



    // Start is called before the first frame update
    void Start()
    {
        Strokestack = new Stack<GameObject>();
        
    }

    public void ClearAndDestroyStackObjects()
    {
        // Check if the stack has objects
        if (Strokestack.Count > 0)
        {
            // Destroy all GameObjects in the stack
            foreach (GameObject obj in Strokestack)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            // Clear the stack
            Strokestack.Clear();
            Debug.Log("Stack has been cleared and all GameObjects destroyed.");
        }
        else
        {
            Debug.Log("Stack is already empty.");
        }
    }


    public GameObject tempSroke=null;

    // Update is called once per frame
    void Update()
    {

        if(!DrawingMode.isOn) return;

        





         if(OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)){
            if(!isDrawing){
                isDrawing=true;
                tempSroke=Instantiate(Stroke, pen.transform.position, pen.transform.rotation) as GameObject;  
                tempSroke.transform.parent=pen.transform;

                


            }
        }



        if(OVRInput.GetUp(OVRInput.Button.SecondaryIndexTrigger)){
            if( isDrawing){
                isDrawing=false;
                tempSroke.transform.parent=Zerocanvas.transform;
                Strokestack.Push(tempSroke);


                // tempSroke=null;
                // StrokeWidth=0.01f;




            }
        }




        if(OVRInput.Get(OVRInput.Button.SecondaryThumbstickUp)){

            StrokeWidth+=0.01f;
           
        }



        if(OVRInput.Get(OVRInput.Button.SecondaryThumbstickDown)){
           StrokeWidth-=0.01f;
        }





    

        //Update Style

        tempSroke.GetComponent<TheInk>().setWidth(StrokeWidth);






    if(OVRInput.Get(OVRInput.Button.SecondaryThumbstickLeft))
        {

            Destroy(Strokestack.Pop());

        


        }



    







        
    }
}
