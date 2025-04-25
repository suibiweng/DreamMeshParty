using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WearableAttach : MonoBehaviour
{

    public Transform Bodypart; 
    // public Transform Parent;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Bodypart!=null)
        {
            transform.position=Bodypart.position;
            transform.rotation=Bodypart.rotation;
        }
        
    }

    private void OnCollisionEnter(Collision other) {
       if(other.gameObject.tag=="AvatarHead")
       {
        Bodypart = other.gameObject.transform;

        // transform.SetParent(Parent);
       }
    
        
    }

    private void OnCollisionExit(Collision other) {
        if(other.gameObject.tag=="AvatarHead")
        {
              Bodypart=null;
            // transform.SetParent(Parent);
        }
        
    }
}
