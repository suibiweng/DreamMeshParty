using System;
using Fusion;
using UnityEngine;


public class TriggerSync : NetworkBehaviour
{
    // Start is called before the first frame update
    // private GenerateSpot _generateSpot;
    public outputComponent outputComponent;

    private void Start()
    {
        // _generateSpot = GetComponent<GenerateSpot>(); 
    }

    // // This method will be executed on all clients when invoked
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void TriggerRPC()
    {
        Debug.Log($"ToggleTrigger");

        // Additional logic to handle the RPC
        //outputComponent.ShowOutputWithoutInput();
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void LightTriggerRPC(){


        outputComponent.ToggleLightState();


    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void SoundTriggerRPC(){


        outputComponent.ToggleSoundState();


    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void GunTriggerRPC(){


        outputComponent.TogglegunState();


    }


    public void CallLightRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        LightTriggerRPC();

        
    }



    
    public void CallSoundRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        SoundTriggerRPC();

        
    }



    public void CallGunRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        GunTriggerRPC();

        
    }












    // // Example of how to call an RPC
    public void CallTriggerRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        TriggerRPC();

        
    }
}
