using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExtinguisherTest : MonoBehaviour
{
    public ExtinguisherBehavior extinguisherBehavior;

    void Update()
    {
        // Check for key inputs to test showing extinguisher models
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            extinguisherBehavior.ShowExtinguisherModel(ExtinguisherBehavior.ExtinguisherType.CO2);
            Debug.Log("Showing CO2 extinguisher.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            extinguisherBehavior.ShowExtinguisherModel(ExtinguisherBehavior.ExtinguisherType.DryPowder);
            Debug.Log("Showing Dry Powder extinguisher.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            extinguisherBehavior.ShowExtinguisherModel(ExtinguisherBehavior.ExtinguisherType.Foam);
            Debug.Log("Showing Foam extinguisher.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            extinguisherBehavior.ShowExtinguisherModel(ExtinguisherBehavior.ExtinguisherType.Water);
            Debug.Log("Showing Water extinguisher.");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            extinguisherBehavior.ShowExtinguisherModel(ExtinguisherBehavior.ExtinguisherType.WetChemical);
            Debug.Log("Showing Wet Chemical extinguisher.");
        }
    }
}
