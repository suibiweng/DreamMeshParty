using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;

public class FireSpot : MonoBehaviour
{
    public bool isOnFire;

    public FireSync fireSync;

    public int firelfe=5;

    public FireParticleLife fireParticleLife;

    public FiresceneManager firesceneManager;
    // Start is called before the first frame update
    void Start()
    {

        firesceneManager=FindObjectOfType<FiresceneManager>();  
        if(firesceneManager!=null){

                fireSync=GetComponent<FireSync>();

        }
       


       
        




    }

    // Update is called once per frame
    void Update()
    {
        if(firesceneManager==null) return;

       


        if(fireParticleLife.currentLife<=0 && isOnFire){
             putOutFire();
             firesceneManager.putOutFire(GetComponent<GenerateSpot>().URLID);



        }


    
        
    }


    public void setFire(){
        isOnFire=true;
        fireParticleLife.gameObject.SetActive(true);
       // fireSync.CallSetFireRPC();
     

    }


    public void putOutFire(){
        fireParticleLife.gameObject.SetActive(false);
        // fireSync.CallputFireoutRPC();
        isOnFire=false;        

    }
}
