using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheInk : MonoBehaviour
{
    public TrailRenderer trailRenderer;


    public float width = 0.03f;
    // Start is called before the first frame update
    void Start()
    {
        trailRenderer = GetComponent<TrailRenderer>();

        trailRenderer.startWidth = width;
        trailRenderer.endWidth=width;   


        
    }
    
    public void setColor(Color setColor)
    {
        trailRenderer.startColor = setColor;
        trailRenderer.endColor = setColor;
    }

    public void setWidth(float setWidth)
    {


        trailRenderer.startWidth = setWidth;
        trailRenderer.endWidth = setWidth;



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
