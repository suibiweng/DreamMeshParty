using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bodyTransform : MonoBehaviour
{
    public Transform head; 
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = head.position +  Vector3.down * 0.314f;
    }
}
