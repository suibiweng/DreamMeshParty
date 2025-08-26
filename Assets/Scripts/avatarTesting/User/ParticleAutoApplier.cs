using UnityEngine;

public class ParticleAutoApplier : MonoBehaviour
{
    public ParticleSystem ps;
    public ParticleEffectConfigV2 cfg;
    public Material fallbackMat;

    // Extra plumbing supported by your ParticleDTOApplier_V2.Apply(...)
    public Component relayOwner;
    public Transform orienterSpawnPoint;
    public LayerMask? overrideCollisionMask;

    public enum Mode { OnSpawn, EveryFrame, OnDirty, Interval }
    public Mode ApplyMode = Mode.EveryFrame;
    public float IntervalSeconds = 0.1f;

    private float _t;
    private bool _dirty;

    void Awake()
    {
        if (!ps) ps = GetComponent<ParticleSystem>() ?? GetComponentInChildren<ParticleSystem>(true);
    }

    void Start()
    {
        if (ApplyMode == Mode.OnSpawn) SafeApply();
    }

    void Update()
    {
        if (!ps || cfg == null) return;

        switch (ApplyMode)
        {
            case Mode.EveryFrame:
                SafeApply();
                break;

            case Mode.OnDirty:
                if (_dirty) { _dirty = false; SafeApply(); }
                break;

            case Mode.Interval:
                _t += Time.deltaTime;
                if (_t >= IntervalSeconds) { _t = 0f; SafeApply(); }
                break;
        }
    }

    public void SetConfig(ParticleEffectConfigV2 newCfg)
    {
        cfg = newCfg;
        _dirty = true;
    }

    public void MarkDirty() => _dirty = true;

private void SafeApply()
{
    try
    {
        // 1) Apply your DTO with the existing 3‑arg signature
        ParticleDTOApplier_V2.Apply(ps, cfg, fallbackMat);

        // 2) If you want a specific collision mask, force it here
        if (overrideCollisionMask.HasValue)
        {
            var col = ps.collision;
            col.enabled = true;                  // ensure it's on if you’re forcing a mask
            col.collidesWith = overrideCollisionMask.Value;
        }

        // 3) If you want to set the ParticleCollisionRelay owner, do it after Apply
        // if (relayOwner != null)
        // {
        //     var relay = ps.GetComponent<ParticleCollisionRelay>();
        //     if (!relay) relay = ps.gameObject.AddComponent<ParticleCollisionRelay>();
        //     relay.Owner = relayOwner;
        // }

        // 4) If you want world‑space aiming, set the orienter’s spawn point here
        if (orienterSpawnPoint != null)
        {
            var orienter = ps.GetComponent<ProjectilePSOrienter>();
            if (!orienter) orienter = ps.gameObject.AddComponent<ProjectilePSOrienter>();
            orienter.ps = ps;
            orienter.SpawnPoint = orienterSpawnPoint;
        }
    }
    catch (System.Exception e)
    {
        Debug.LogError($"ParticleAutoApplier: Apply failed: {e}");
    }
}

}
