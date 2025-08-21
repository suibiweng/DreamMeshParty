using UnityEngine;
using RealityEditor; // for GeneratedColliderMarker

[DefaultExecutionOrder(-10000)]
public class EarlyMeshReadabilityGuard : MonoBehaviour
{
    public GameObject root;

    void Awake()
    {
        if (!root) root = gameObject;
        MeshColliderSweeper.CleanupGeneratedColliders(root);
        RuntimeMeshReadabilityPatcher.PatchAllUnder(root);
    }
}
