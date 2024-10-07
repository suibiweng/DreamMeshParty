using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Fusion;

public class Exsync : NetworkBehaviour
{

    public PressureSteam pressureSteam;
    // Start is called before the first frame update
    void Start()
    {
        pressureSteam=GetComponent<PressureSteam>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void SetExRPC()
    {
        Debug.Log($"ToggleTrigger");

        pressureSteam.ActivateObject();
        //

        // Additional logic to handle the RPC
       
    }

         [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void SetOffExRPC()
    {
        Debug.Log($"ToggleTrigger");

       pressureSteam.DeactivateObject();
        //

        // Additional logic to handle the RPC
       
    }

    public void CallallSetEx(){

         SetExRPC();



    }



   

    public void CallallSetOffEx(){

        
      //  SetOffExRPC();


    }

}
