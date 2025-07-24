using System;
using Fusion;
using UnityEngine;

public class GenerateSpotRPC : NetworkBehaviour
{
    public GenerateSpot _generateSpot;
    public LuaMonoBehavior _luaMonoBehavior;

    private void Start()
    {
        _generateSpot = GetComponent<GenerateSpot>();
        _luaMonoBehavior = GetComponent<LuaMonoBehavior>();
    

    }

    // This method will be executed on all clients when invoked
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ConfirmGeneration()
    {
        Debug.Log($"RPC received to ConfirmGeneration");
        // _generateSpot.initAdd();
        // _generateSpot.Outlinebox.wire_renderer = false;
        // _generateSpot.VoicePanel.SetActive(false);
       _generateSpot.RPCGenrateModel();
        // Additional logic to handle the RPC
    }
    public void RPCTrigger()
    {
        Debug.Log($"RPC received to Trigger");
        _luaMonoBehavior.RPCTrigger();
    }




    // Example of how to call an RPC
    public void CallConfirmGenerationRPC()
    {
        // Call the RPC on all clients
        RPC_ConfirmGeneration();


    }


    public void CallTriggerRPC()
    {
        // Call the RPC on all clients
        RPCTrigger();
    }
    
    //Remove spot from all player's dictionaries to properly delete a spot
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_DeleteSpot()
    {
        Debug.Log($"RPC received to Delete spot" + _generateSpot.URLID);
        _generateSpot.Remove();
        // Additional logic to handle the RPC
    }

    // Example of how to call an RPC
    public void CallDeleteSpotRPC()
    {
        // Call the RPC on all clients
        RPC_DeleteSpot();
    }
}
