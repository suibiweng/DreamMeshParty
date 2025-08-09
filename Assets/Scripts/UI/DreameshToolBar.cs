using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealityEditor;

public class DreameshToolBar : MonoBehaviour
{
    public RealityEditorManager realityEditorManager;
    public SceneSessionManager sessionManager;
    public Button PlayandStopButton;
    public TMP_Text PlayandStopText;


    // Start is called before the first frame update
    void Start()
    {
        sessionManager = FindObjectOfType<SceneSessionManager>();
        realityEditorManager = FindObjectOfType<RealityEditorManager>();
    

    }

    public void createSpotOnMenu()
    {
        if (realityEditorManager != null)
        {
            realityEditorManager.createSpot(transform.position);
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlayandStopText();
    }

    public void UpdatePlayandStopText()
    {
        if (sessionManager.isPlaying)
        {
       
                PlayandStopText.text = "Stop";
            
        }
        else
        {
            PlayandStopText.text = "Start ";
        }
    }
    
}
