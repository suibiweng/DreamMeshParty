using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followTransform : MonoBehaviour
{
    public Transform target; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
        float yOffset = Mathf.Sin(Time.time * 1) * 0.5f;
        transform.position = transform.position + new Vector3(0, yOffset, 0);

    }
    
    
    
    
}
