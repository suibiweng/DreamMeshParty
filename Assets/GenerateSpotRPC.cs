using System;
using Fusion;
using UnityEngine;

public class GenerateSpotRPC : NetworkBehaviour
{
    private GenerateSpot _generateSpot;

    private void Start()
    {
        _generateSpot = GetComponent<GenerateSpot>(); 
    }

    // This method will be executed on all clients when invoked
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_ConfirmGeneration()
    {
        Debug.Log($"RPC received to ConfirmGeneration");
        _generateSpot.initAdd();
        // Additional logic to handle the RPC
    }

    // Example of how to call an RPC
    public void CallConfirmGenerationRPC()
    {
        // Call the RPC on all clients
        RPC_ConfirmGeneration();

        
    }
}
