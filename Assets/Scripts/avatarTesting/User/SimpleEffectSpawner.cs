using UnityEngine;

public static class SimpleEffectSpawner
{
    public static Material GlobalFallbackMat;

    private static GameObject ResolvePrefab(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName)) return null;
        var go = Resources.Load<GameObject>($"Particles/{effectName}");
        if (!go) go = Resources.Load<GameObject>($"Effects/{effectName}");
        if (!go) go = Resources.Load<GameObject>(effectName);
        return go;
    }

    public static void Spawn(string effectName, string dynamicCodingId, Transform parent, Material fallbackMat = null)
    {
        if (!EffectConfigCache.TryGetConfig(dynamicCodingId, effectName, out var cfg))
        {
            Debug.LogWarning($"SimpleEffectSpawner: cfg not found for '{effectName}' in '{dynamicCodingId}'");
            return;
        }

        var prefab = ResolvePrefab(effectName);
        if (!prefab)
        {
            Debug.LogWarning($"SimpleEffectSpawner: prefab not found for '{effectName}' (Resources)");
            return;
        }

        var inst = Object.Instantiate(prefab, parent, false);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;
        inst.transform.localScale = Vector3.one;

        var ps = inst.GetComponentInChildren<ParticleSystem>(true);
        if (!ps)
        {
            Debug.LogWarning($"SimpleEffectSpawner: no ParticleSystem in prefab '{effectName}'");
            Object.Destroy(inst);
            return;
        }

        var auto = inst.GetComponent<ParticleAutoApplier_NoCollision>();
        if (!auto) auto = inst.AddComponent<ParticleAutoApplier_NoCollision>();
        auto.ps = ps;
        auto.cfg = cfg;
        auto.fallbackMat = fallbackMat ? fallbackMat : GlobalFallbackMat;

        ps.Clear(true);
        ps.Play(true);
    }
}

public class ParticleAutoApplier_NoCollision : MonoBehaviour
{
    public ParticleSystem ps;
    public ParticleEffectConfigV2 cfg;
    public Material fallbackMat;

    void Update()
    {
        if (!ps || cfg == null) return;

        try
        {
            ParticleDTOApplier_V2.Apply(ps, cfg, fallbackMat);

            // explicitly ensure collisions are OFF
            var col = ps.collision;
            col.enabled = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ParticleAutoApplier_NoCollision: Apply failed: {e}");
        }
    }
}
