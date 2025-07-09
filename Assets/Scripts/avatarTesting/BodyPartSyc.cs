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
         // transform.SetParent(target.transform, worldPositionStays: false);
         

         Target=target;
     }  
    // Start is called before the first frame update
    void Start()
    {
        _networkObject = GetComponent<NetworkObject>();
        _runner=FindObjectOfType<NetworkRunner>();
        takeOwnership();
        Debug.Log("In the start of the Body Sync");
    
    }

    // Update is called once per frame
    void Update()
    {
        if (_networkObject.HasStateAuthority) {
            // transform.position = Target.position;
            transform.rotation = Target.rotation;
            transform.SetPositionAndRotation(Target.position, Target.rotation);
            
        }
        else
        {
            Debug.Log("You dont have authority to change the transfer bud!!!");
        }
        Debug.Log("InputAuthority = " + _networkObject.HasInputAuthority + "StateAuthority" +  _networkObject.HasStateAuthority);

        
        float yOffset = Mathf.Sin(Time.time * 1) * 0.5f;
        transform.position = transform.position + new Vector3(0, yOffset, 0);
        // transform.position = Target.position;
        // transform.rotation = Target.rotation;
        // transform.localScale = Target.lossyScale;
        
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
