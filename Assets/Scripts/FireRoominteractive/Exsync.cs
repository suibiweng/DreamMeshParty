using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Fusion;

public class Exsync : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void SetExRPC()
    {
        Debug.Log($"ToggleTrigger");

        GetComponent<PressureSteam>().ActivateObject();
        //

        // Additional logic to handle the RPC
       
    }

    public void CallallSetEx(){

         SetExRPC();



    }



        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void SetOffExRPC()
    {
        Debug.Log($"ToggleTrigger");

        GetComponent<PressureSteam>().DeactivateObject();
        //

        // Additional logic to handle the RPC
       
    }

    public void CallallSetOffEx(){

        
        SetOffExRPC();


    }

}
