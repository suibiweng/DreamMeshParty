using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enum for extinguisher types
public enum ExtinguisherType
{
    CO2,
    DryPowder,
    Foam,
    Water,
    WetChemical
}

public class ExtinguisherBehavior : MonoBehaviour
{
    // Public GameObjects for different extinguisher models
    public GameObject CO2;
    public GameObject DryPowder;
    public GameObject Foam;
    public GameObject Water;
    public GameObject WetChemical;
    public ExtinguisherType Type;

    // Dictionary to map enum values to GameObjects
    private Dictionary<ExtinguisherType, GameObject> extinguisherModels;

    void Start()
    {
        // Initialize the dictionary
        extinguisherModels = new Dictionary<ExtinguisherType, GameObject>
        {
            { ExtinguisherType.CO2, CO2 },
            { ExtinguisherType.DryPowder, DryPowder },
            { ExtinguisherType.Foam, Foam },
            { ExtinguisherType.Water, Water },
            { ExtinguisherType.WetChemical, WetChemical }
        };

        // Hide all models at the start
        HideAllModels();
        ShowExtinguisherModel(Type);
    }

 
    /// <summary>
    /// Makes the specified extinguisher model visible and hides others.
    /// </summary>
    /// <param name="type">The type of extinguisher to show.</param>
    public void ShowExtinguisherModel(ExtinguisherType type)
    {
        // Hide all models first
        HideAllModels();

        // Show the selected model
        if (extinguisherModels.TryGetValue(type, out GameObject model))
        {
            model.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Extinguisher type {type} not found!");
        }
    }

    /// <summary>
    /// Hides all extinguisher models.
    /// </summary>
    private void HideAllModels()
    {
        foreach (var model in extinguisherModels.Values)
        {
            model.SetActive(false);
        }
    }
}
