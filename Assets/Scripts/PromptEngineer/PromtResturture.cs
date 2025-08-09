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

    void Start()
    {
        combinedPrompt = FormatPrompt();
        realityEditorManager = FindObjectOfType<RealityEditorManager>();
    }

    void Update()
    {
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
        // realityEditorManager.SubmitPrompt(combinedPrompt);
        generateSpot.Prompt = combinedPrompt;
        generateSpot.EditCode();
    }
}
