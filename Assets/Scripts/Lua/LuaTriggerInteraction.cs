using System.Collections;
using UnityEngine;
using Fusion;

public class LuaTriggerInteraction : NetworkBehaviour
{
    [Header("Refs")]
    public GenerateSpot generateSpot;
    public LuaMonoBehavior luaTarget;

    [Header("Muzzle / Orientation")]
    public Transform SpawnPoint; // assign to your controller muzzle or any transform

    [Header("Input")]
    public OVRInput.Button leftTrigger  = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button rightTrigger = OVRInput.Button.SecondaryIndexTrigger;

    [Header("Haptics (optional)")]
    public bool hapticsOnFire = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    [Range(0f, 1f)] public float hapticDuration  = 0.05f;

    private ProjectilePSOrienter[] _orienters;

    private void Awake()
    {
        if (generateSpot == null) generateSpot = GetComponent<GenerateSpot>();
        if (luaTarget == null) luaTarget = GetComponent<LuaMonoBehavior>();

        // Cache any orienters (created by ParticleDTOApplier for projectile FX)
        _orienters = GetComponentsInChildren<ProjectilePSOrienter>(true);

        SpawnPoint = luaTarget.SpwanPoint;
    }

    private void Start()
    {
        if (generateSpot == null) Debug.LogWarning("[LuaTriggerInteraction] Missing GenerateSpot reference.");
        if (luaTarget    == null) Debug.LogWarning("[LuaTriggerInteraction] Missing LuaMonoBehavior reference.");
        if (SpawnPoint   == null) Debug.LogWarning("[LuaTriggerInteraction] SpawnPoint is not assigned.");

        // Wire SpawnPoint into all orienters
        if (_orienters != null)
        {
            foreach (var o in _orienters)
            {
                if (o != null) o.SpawnPoint = SpawnPoint;
            }
        }
    }

    private void Update()
    {
        bool leftDown  = OVRInput.GetDown(leftTrigger);
        bool rightDown = OVRInput.GetDown(rightTrigger);
        if (!(leftDown || rightDown)) return;

        if (!Object || !HasInputAuthority) return;
        if (generateSpot != null && generateSpot.isGrabing)
        {
            // Local: orient projectile FX to current SpawnPoint pose, then trigger
            CallLuaTriggerLocal();

            // Network: tell authority to propagate
            RPC_RequestTrigger();

            if (hapticsOnFire)
            {
                TryPulseHaptics(leftDown ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch,
                                hapticAmplitude, hapticDuration);
            }
        }
    }

    private void CallLuaTriggerLocal()
    {
        // 1) Orient ONLY the projectile PS (collision-enabled) to SpawnPoint
        if (_orienters != null)
        {
            foreach (var o in _orienters)
            {
                if (o != null)
                {
                    // make sure the latest SpawnPoint is applied (in case reassigned at runtime)
                    o.SpawnPoint = SpawnPoint;
                    o.OrientIfProjectile();
                }
            }
        }

        // 2) Now let Lua play whatever effects it wants (projectile will be aligned)
        if (luaTarget != null) luaTarget.Trigger();
        else Debug.LogWarning("[LuaTriggerInteraction] No LuaMonoBehavior assigned.");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestTrigger()
    {
        CallLuaTriggerLocal();
        RPC_PropagateTrigger();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = false)]
    private void RPC_PropagateTrigger()
    {
        CallLuaTriggerLocal();
    }

    private void TryPulseHaptics(OVRInput.Controller controller, float amplitude, float duration)
    {
        StartCoroutine(HapticPulse(controller, amplitude, duration));
    }

    private IEnumerator HapticPulse(OVRInput.Controller controller, float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(1f, Mathf.Clamp01(amplitude), controller);
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
