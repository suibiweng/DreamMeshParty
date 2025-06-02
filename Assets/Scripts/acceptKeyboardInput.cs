using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class acceptKeyboardInput : MonoBehaviour
{
    public TextMeshProUGUI textDisplay; 

    private string fullText = ""; // Stores the full input string

    // Called when the keyboard commits text (each keypress)
    public void DisplayInput(string input)
    {
        fullText += input; // Append the new letter to the full string
        // textDisplay.text = fullText; // Update the display
    }

    // Optional: Add a backspace function
    public void Backspace()
    {
        if (fullText.Length > 0)
        {
            fullText = fullText.Substring(0, fullText.Length - 1);
            textDisplay.text = fullText;
        }
    }
    
    public void ClearInput()
    {
        fullText = "";
        textDisplay.text = "";
    }

    public void enterText()
    {
        textDisplay.text = fullText; 
        fullText = "";
    }
}
