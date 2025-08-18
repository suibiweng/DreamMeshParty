using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using RealityEditor;

public class PromptRestructure : MonoBehaviour
{
    public TMP_InputField OtoOPrompt;  // Object-to-Object
    public TMP_InputField OtoUPrompt;  // Object-to-User
    public TMP_InputField OtoEPrompt;  // Object-to-Environment

    public TMP_InputField AdditionalPrompts; // Additional prompts

    public RealityEditorManager realityEditorManager;
    public GenerateSpot generateSpot;

    public string combinedPrompt;

    public string debugtext;

    void Start()
    {
        combinedPrompt = FormatPrompt();
        realityEditorManager = FindObjectOfType<RealityEditorManager>();
    }

    void Update()
    {
        var debugSelect = generateSpot.gameObject.GetComponent<LuaMonoBehavior>().debugSelect;

        if (Input.GetKeyDown(KeyCode.F6) && debugSelect)
        {
            OnEdit();
        }



        if (Input.GetKeyDown(KeyCode.F7) && debugSelect)
        {
            OnDebugEdit();
        }
        // Optional: live updating
        combinedPrompt = FormatPrompt();
    }

    public string GetCombinedPrompt()
    {
        combinedPrompt = FormatPrompt();
        return combinedPrompt;
    }

    private string FormatPrompt()
    {
        return "[Object-to-Object] " + OtoOPrompt.text.Trim() + ". " +
               "[Object-to-User] " + OtoUPrompt.text.Trim() + ". " +
               "[Object-to-Environment] " + OtoEPrompt.text.Trim() + "." +
               "Additional Prompts: " + AdditionalPrompts.text.Trim();
    }


    public void OnEdit()
    {
        combinedPrompt = FormatPrompt();

        generateSpot.Prompt = combinedPrompt;
        generateSpot.EditCode();
    }


    

    public void OnDebugEdit()
    {
        combinedPrompt = debugtext;

        generateSpot.Prompt = combinedPrompt;
        generateSpot.EditCode();
    }


}
