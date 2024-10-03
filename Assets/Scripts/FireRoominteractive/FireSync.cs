using System;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FireSync : NetworkBehaviour
{

    public FireSpot fireSpot;



    [Networked, OnChangedRender(nameof(OnFirelifeChange))]
    public int _fireLife { get; set; }






    void OnFirelifeChange()
    {
        fireSpot = GetComponent<FireSpot>();

        // Debug.Log("Networked prompt changed to: " + NetworkedPrompt);
        fireSpot.firelfe = _fireLife; 
    }





     public void UpdateLife(int life)
    {
        if (HasStateAuthority)
        {

            _fireLife=life;

            // Change the string value here, which will then be synchronized across all clients
            
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void SetFireRPC()
    {
        Debug.Log($"ToggleTrigger");
        fireSpot.setFire();

        // Additional logic to handle the RPC
       
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void PutFireOutRPC()
    {
        Debug.Log($"ToggleTrigger");
        fireSpot.putOutFire();

        // Additional logic to handle the RPC
       
    }



        public void CallSetFireRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();
        SetFireRPC();

    }
            public void CallputFireoutRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();
       PutFireOutRPC();

    }

}
