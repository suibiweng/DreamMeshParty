using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSync : NetworkBehaviour
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
    public void TurnOFFRPC()
    {
     gameObject.SetActive(false);   
        // Additional logic to handle the RPC
       
    }

    public void CallTurnOffMenu(){

        TurnOFFRPC();


    }

}
