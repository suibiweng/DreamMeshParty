using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DropdownScript : MonoBehaviour
{
    // References to the dropdowns
    public TMP_Dropdown dropdown1; // For interactPrefab
    public TMP_Dropdown dropdown2; // For effectPrefab

    // Reference to the button
    public Button generateButton;

    // Prefabs for interactPrefab (based on Dropdown 1)
    public GameObject interactPrefab1; // Dropdown1 value 0
    public GameObject interactPrefab2; // Dropdown1 value 1

    // Prefabs for effectPrefab (based on Dropdown 2)
    public GameObject effectPrefab1; // Dropdown2 value 0
    public GameObject effectPrefab2; // Dropdown2 value 1

    // Spawn positions
    public Vector3 interactPrefabPosition = Vector3.zero; // Default to origin
    public Vector3 effectPrefabPosition = new Vector3(1, 0, 0); // Default to offset position

    // Store the last spawned effect object to control its emission and audio
    private GameObject effectObject;

    // Audio clip to play on the effectObject
    public AudioClip effectAudioClip;

    void Start()
    {
        // Initialize the button listener
        generateButton.onClick.AddListener(OnGenerateButtonClicked);
    }

    // Public methods to set the positions from other scripts/UI
    public void SetInteractPrefabPosition(Vector3 position)
    {
        interactPrefabPosition = position;
    }

    public void SetEffectPrefabPosition(Vector3 position)
    {
        effectPrefabPosition = position;
    }

    void OnGenerateButtonClicked()
    {
        // Get the selected values from the dropdowns
        int dropdownValue1 = dropdown1.value;
        int dropdownValue2 = dropdown2.value;

        // Define the interactPrefab to instantiate based on Dropdown 1
        GameObject interactPrefab = null;
        if (dropdownValue1 == 0)
        {
            interactPrefab = interactPrefab1; // Generate interactPrefab1 for Dropdown1 value 0
        }
        else if (dropdownValue1 == 1)
        {
            interactPrefab = interactPrefab2; // Generate interactPrefab2 for Dropdown1 value 1
        }

        // Define the effectPrefab to instantiate based on Dropdown 2
        GameObject effectPrefab = null;
        if (dropdownValue2 == 0)
        {
            effectPrefab = effectPrefab1; // Generate effectPrefab1 for Dropdown2 value 0
            AddEmissionControl(effectPrefab1); // Add EmissionControl to effectPrefab1
        }
        else if (dropdownValue2 == 1)
        {
            effectPrefab = effectPrefab2; // Generate effectPrefab2 for Dropdown2 value 1
            AddAudioControl(effectPrefab2); // Add AudioControl to effectPrefab2
        }

        // Instantiate the interactPrefab at the specified position
        if (interactPrefab != null)
        {
            GameObject interactObject = Instantiate(interactPrefab, interactPrefabPosition, Quaternion.identity);

            // Add the CollisionDetection script to the interactObject
            CollisionDetection collisionDetection = interactObject.AddComponent<CollisionDetection>();


            // Set the references to the control components on the CollisionDetection script
            if (effectPrefab != null)
            {
                EmissionControl emissionControl = effectPrefab.GetComponent<EmissionControl>();
                AudioControl audioControl = effectPrefab.GetComponent<AudioControl>();

                collisionDetection.emissionControl = emissionControl;
                collisionDetection.audioControl = audioControl;
            }
        }
        else
        {
            Debug.LogWarning("No interactPrefab assigned for this value of dropdown 1.");
        }

        // Instantiate the effectPrefab at the specified position
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, effectPrefabPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No effectPrefab assigned for this value of dropdown 2.");
        }
    }

    // Method to add EmissionControl to effectPrefab1
    void AddEmissionControl(GameObject prefab)
    {
        if (prefab != null)
        {
            EmissionControl emissionControl = prefab.GetComponent<EmissionControl>();
            if (emissionControl == null)
            {
                emissionControl = prefab.AddComponent<EmissionControl>();
            }
            emissionControl.Initialize(prefab);
        }
    }

    // Method to add AudioControl to effectPrefab2
    void AddAudioControl(GameObject prefab)
    {
        if (prefab != null)
        {
            AudioControl audioControl = prefab.GetComponent<AudioControl>();
            if (audioControl == null)
            {
                audioControl = prefab.AddComponent<AudioControl>();
            }
            audioControl.Initialize(prefab, effectAudioClip);
        }
    }
}
