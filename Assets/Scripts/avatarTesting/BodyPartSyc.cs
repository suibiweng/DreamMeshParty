using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class BodyPartSyc : MonoBehaviour
{

    private NetworkObject _networkObject;
     private NetworkRunner _runner;


     public Transform Target;



     public void SetTarget(Transform target)
     {
         Target=target;
     }  
    // Start is called before the first frame update
    void Start()
    {
        _networkObject = GetComponent<NetworkObject>();
        _runner=FindObjectOfType<NetworkRunner>();
        takeOwnership();
    
    }

    // Update is called once per frame
    void Update()
    {
        // transform.position= Target.position;
        // transform.rotation= Target.rotation;

        
    }



    
    private void takeOwnership()
    {
        StartCoroutine(GimmeYoAuthority()); 
    }

    IEnumerator GimmeYoAuthority()
    {
        while (!_networkObject.HasInputAuthority || !_networkObject.HasStateAuthority)
        {
            if (!_networkObject.HasStateAuthority)
            {
                _networkObject.RequestStateAuthority();
                yield return 0.5f;
            }
            else
            {
                _networkObject.AssignInputAuthority(_runner.LocalPlayer);
                yield return 0.5f;
            }
             
        }
    }
}
