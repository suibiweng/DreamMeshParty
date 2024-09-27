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


    
    
    
    // Start is called before the first frame update
    void Start()
    {



    
    
    
    }


    



    // Update is called once per frame
    void Update()
    {

        _gravity.text=""+solarSystemSimulation.planetGravity;
        _airDrag.text=""+solarSystemSimulation.planetAtmosphereDrag;




    }


     public void setUPPlanetary(){



        solarSystemSimulation.SetPlanetaryParameters(celestialBody[getPlanet()]);

    }


    int getPlanet(){

        for(int i=0;i<Planettoggles.Length;i++)
            if(Planettoggles[i].isOn)return i;

        return 0;


    }



}
