using UnityEngine;

public class PersistentManagers : MonoBehaviour
{
    private static PersistentManagers instance;

    private void Awake()
    {
        // if (instance == null)
        // {
        //     instance = this;
            DontDestroyOnLoad(gameObject); 
        // }
        // else
        // {
        //     Destroy(gameObject); // Ensure only one instance
        // }
    }
}