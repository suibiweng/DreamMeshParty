using System;
using Fusion;
using UnityEngine;


public class TriggerSync : NetworkBehaviour
{
    // Start is called before the first frame update
    // private GenerateSpot _generateSpot;
    public outputComponent outputComponent;
    public InteractableAssets interactableAssets;

    private void Start()
    {
        interactableAssets = GetComponent<InteractableAssets>();
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

        Debug.Log("Called Output RPC : Light trigger before");

        outputComponent.ToggleLightState();
        Debug.Log("Called Output RPC : Light trigger before");


    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void SoundTriggerRPC(){

        Debug.Log("Called Output RPC : Sound trigger before");

        outputComponent.ToggleSoundState();
        Debug.Log("Called Output RPC : Sound trigger after");


    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void GunTriggerRPC(){

        Debug.Log("Called Output RPC : Gun trigger before");
        outputComponent.TogglegunState();
        Debug.Log("Called Output RPC: Gun trigger after");


    }

  [Rpc(RpcSources.InputAuthority, RpcTargets.All)]

    public void AlignwihtContorllerRPC(){


     //  interactableAssets.AlignWithController(controller);



    }




    public void CallAlignUpRPC(){


        AlignwihtContorllerRPC();



    }

    [ContextMenu("Call CallLightRPC")]
    public void CallLightRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        LightTriggerRPC();

        
    }



    [ContextMenu("Call CallSoundRPC")]
    public void CallSoundRPC()
    {
        // Call the RPC on all clients
        //RPC_ConfirmGeneration();

        SoundTriggerRPC();

        
    }

    [ContextMenu("Call CallGunRPC")]
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
