using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlantsMenu : MonoBehaviour
{

    public TextMeshPro _gravity,_airDrag;
    public SolarSystemSimulation solarSystemSimulation;

    public CelestialBody[] celestialBody;


    public Toggle [] Planettoggles;

    public GravitySync gravitySync;


    
    
    
    // Start is called before the first frame update
    void Start()
    {
        gravitySync=GetComponent<GravitySync>();    
         setToggle(0);


    
    
    
    }


    



    // Update is called once per frame
    void Update()
    {

        _gravity.text=""+solarSystemSimulation.planetGravity;
        _airDrag.text=""+solarSystemSimulation.planetAtmosphereDrag;




    }

    public  int selectIndex;


     public void setUPPlanetary(){



        solarSystemSimulation.SetPlanetaryParameters(celestialBody[getPlanet()]);

    }

    public void setToggle(int si){

         for(int i=0;i<Planettoggles.Length;i++)Planettoggles[i].isOn=false;
        Planettoggles[si].isOn=true;



    }


    int getPlanet(){

        for(int i=0;i<Planettoggles.Length;i++)
            if(Planettoggles[i].isOn){
                
                selectIndex=i;

                gravitySync.UpdateSelect(selectIndex) ;
                return i;



            }

        return 0;


    }



}
