using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityEditor;

public class SetCollider : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

        var GeneratedmeshCollider = ColliderUtils.AddMeshCollider(gameObject, convex: true);
         int genLayer = LayerMask.NameToLayer("GeneratedObject");
        GeneratedmeshCollider.excludeLayers = LayerMask.GetMask("GeneratedObject");

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
