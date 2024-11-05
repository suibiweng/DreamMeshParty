using Fusion;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class ScenesSelect : NetworkBehaviour
{
    // public GameObject SceneEditorPrefab;
    public GameObject Cube;
    public RealityEditorManager REM; 
    private void Start()
    {
        if (Runner.IsServer)
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (!netObj.HasStateAuthority)
            {
                netObj.RequestStateAuthority(); // Claim authority over the object
            }
        }
        
        // if (Runner.IsServer) // If we are the host
        // {
        //     REM.SpawnNetworkObject(new Vector3(1, 1, 1), quaternion.identity, SceneEditorPrefab);
        // }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ConfirmGeneration(Color newColor)
    {



        Debug.Log($"RPC received to ConfirmGeneration with color: {newColor}");
        Cube.GetComponent<Renderer>().material.color = newColor;
    }





    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SwithchScene(int scene)
    {
        switch (scene)
        {
            case 0:
            REM.isFireScene= false;
            REM.isPhysics= false;
            break;



            case 1:
            REM.isFireScene= false;
            REM.isPhysics= true;
            break;




            case 2:
            REM.isFireScene= true;
            REM.isPhysics= false;
            break;




           
        }



        // Debug.Log($"RPC received to ConfirmGeneration with color: {newColor}");
        // Cube.GetComponent<Renderer>().material.color = newColor;
    }







    public void ToPhisic(){





    }


    public void ToFire(){





    }




    
    public void CallSceneChangeRPC(int scene)
    {
        // Check if the client has authority before calling the RPC
        if (HasStateAuthority)
        {



            switch (scene)
        {
            case 0:
            REM.isFireScene= false;
            REM.isPhysics= false;
            break;



            case 1:
            REM.isFireScene= false;
            REM.isPhysics= true;
            break;




            case 2:
            REM.isFireScene= true;
            REM.isPhysics= false;
            break;




           
        }







            RPC_SwithchScene(scene);
        }
        else
        {
            Debug.LogError("You do not have State Authority to change the color.");
        }
    }




    public void CallColorChangeRPC()
    {
        // Check if the client has authority before calling the RPC
        if (HasStateAuthority)
        {
            Color randomColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            RPC_ConfirmGeneration(randomColor);
        }
        else
        {
            Debug.LogError("You do not have State Authority to change the color.");
        }
    }
}