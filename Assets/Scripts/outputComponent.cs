using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class outputComponent : MonoBehaviour
{
    
    public InteractableAssets interactableAssets;

    
    public Light light;
    public ParticleSystem particleSystem;
    public AudioSource Sound;

   // public InteractableDreamMesh interactableDreamMesh;

    

    // Start is called before the first frame update
    void Start()
    {

        light=GetComponent<Light>();
        particleSystem=GetComponent<ParticleSystem>();
        Sound=GetComponent<AudioSource>();







        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void triggerOutPut(){

        if(interactableAssets.interactableDreamMesh.output_style==0){ //is trigger
            if(interactableAssets.interactableDreamMesh.trigger_style==1){

                    //output type and Behaviour





            }

            if(interactableAssets.interactableDreamMesh.trigger_style==2){

                        //output type and Behaviour


                
            }




        }else if(interactableAssets.interactableDreamMesh.output_style==1){ // is switch




        //output type and Behaviour



        }

        






    }
}
