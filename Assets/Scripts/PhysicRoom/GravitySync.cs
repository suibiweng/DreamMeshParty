using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using ExitGames.Client.Photon.StructWrapping;
public class GravitySync : NetworkBehaviour
{

    public SolarSystemSimulation solarSystemSimulation;

    [Networked, OnChangedRender(nameof(OnGravityChanged))]
    public float _gravity { get; set; }
    
    [Networked, OnChangedRender(nameof(OnAirChanged))]
    public float _airDrag { get; set; }



    [Networked, OnChangedRender(nameof(OnSelectChanged))]
    public int _select { get; set; }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {




        
    }



    void OnGravityChanged()
    {
            solarSystemSimulation = GetComponent<SolarSystemSimulation>();

        // Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        // _generateSpot.URLID = NetworkedUrlID; 
        solarSystemSimulation.planetGravity=_gravity;
    }
    void OnAirChanged()
    {
        solarSystemSimulation = GetComponent<SolarSystemSimulation>();

        // Debug.Log("Networked urlid changed to: " + NetworkedUrlID);
        // _generateSpot.URLID = NetworkedUrlID; 
        solarSystemSimulation.planetAtmosphereDrag=_airDrag;
    }


    void OnSelectChanged(){

        PlantsMenu menu = GetComponent<PlantsMenu>();

        menu.setToggle(_select);


    }


    public void UpdateGravity(float g)
    {
        if (HasStateAuthority)
        {

            _gravity=g;

            // Change the string value here, which will then be synchronized across all clients
            
        }
    }
    public void UpdateAir(float a)
    {
        if (HasStateAuthority)
        {
            _airDrag=a;
            // Change the string value here, which will then be synchronized across all clients
            // NetworkedPrompt = newUrlID;
        }
    }



        public void UpdateSelect(int s)
    {
        if (HasStateAuthority)
        {
            _select=s;
            // Change the string value here, which will then be synchronized across all clients
            // NetworkedPrompt = newUrlID;
        }
    }
}
