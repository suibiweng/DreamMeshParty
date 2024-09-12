using UnityEngine;

public class EmissionControl : MonoBehaviour
{
    private Material targetMaterial;
    private bool isEmissionOn = false;
    public Color emissionColor = Color.white;

    // Initialize the EmissionControl with the target material
    public void Initialize(GameObject effectObject)
    {
        Renderer renderer = effectObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            targetMaterial = renderer.sharedMaterial; // Use sharedMaterial to modify material properties
        }
    }

    // Method to turn on emission
    public void TurnOnEmission()
    {
        if (targetMaterial != null)
        {
            targetMaterial.EnableKeyword("_EMISSION");
            targetMaterial.SetColor("_EmissionColor", emissionColor);
            isEmissionOn = true;
            Debug.Log("Emission turned on.");
        }
    }

    // Method to turn off emission
    public void TurnOffEmission()
    {
        if (targetMaterial != null)
        {
            targetMaterial.DisableKeyword("_EMISSION");
            targetMaterial.SetColor("_EmissionColor", Color.black);
            isEmissionOn = false;
            Debug.Log("Emission turned off.");
        }
    }

    // Method to toggle emission effect
    public void ToggleEmission()
    {
        if (isEmissionOn)
        {
            TurnOffEmission();
        }
        else
        {
            TurnOnEmission();
        }
    }
}
