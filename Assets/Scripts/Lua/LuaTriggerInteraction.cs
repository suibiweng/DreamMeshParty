using System.Collections;
using UnityEngine;
using Fusion;            // Fusion RPCs
using UnityEngine.Assertions;

public class LuaTriggerInteraction : NetworkBehaviour
{
    [Header("Refs")]
    public GenerateSpot generateSpot;      // set in Inspector (or auto-find)
    public LuaMonoBehavior luaTarget;      // set in Inspector (the one that has .Trigger())

    [Header("Input")]
    public OVRInput.Button leftTrigger  = OVRInput.Button.PrimaryIndexTrigger;
    public OVRInput.Button rightTrigger = OVRInput.Button.SecondaryIndexTrigger;

    [Header("Haptics (optional)")]
    public bool hapticsOnFire = true;
    [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
    [Range(0f, 1f)] public float hapticDuration  = 0.05f;

    private void Awake()
    {
        // Try to auto-wire if not assigned
        if (generateSpot == null) generateSpot = GetComponent<GenerateSpot>();
        if (luaTarget    == null) luaTarget    = GetComponent<LuaMonoBehavior>();
    }

    private void Start()
    {
        // Not strictly required, but helpful to catch missing refs early.
        if (generateSpot == null) Debug.LogWarning("[LuaTriggerInteraction] Missing GenerateSpot reference.");
        if (luaTarget    == null) Debug.LogWarning("[LuaTriggerInteraction] Missing LuaMonoBehavior reference.");
    }

    private void Update()
    {
        // We only want to react to the rising edge (GetDown).
        bool leftDown  = OVRInput.GetDown(leftTrigger);
        bool rightDown = OVRInput.GetDown(rightTrigger);

        if (!(leftDown || rightDown))
            return;

        // Optional: if you only want *local* user with Input Authority to drive network,
        // you can require HasInputAuthority. (This component inherits NetworkBehaviour.)
        if (!Object || !HasInputAuthority)
        {
            // No input authority on this peer; ignore input.
            return;
        }

        // Only act if we’re "grabbing" (your original condition).
        if (generateSpot != null && generateSpot.isGrabing)
        {
            // Fire locally so the local user gets immediate feedback.
            CallLuaTriggerLocal();

            // Notify State Authority (host) to rebroadcast. Everyone will run it.
            RPC_RequestTrigger();

            // Optional haptics
            if (hapticsOnFire)
            {
                TryPulseHaptics(leftDown ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch,
                                hapticAmplitude, hapticDuration);
            }
        }
    }

    /// <summary>
    /// Calls the Lua "trigger()" via your LuaMonoBehavior API.
    /// </summary>
    private void CallLuaTriggerLocal()
    {
        if (luaTarget == null)
        {
            Debug.LogWarning("[LuaTriggerInteraction] No LuaMonoBehavior assigned.");
            return;
        }

        // Your LuaMonoBehavior already exposes Trigger() which invokes Lua 'trigger' if present.
        luaTarget.Trigger();
    }

    // -----------------------
    // Fusion RPCs
    // -----------------------

    // Client/InputAuthority -> StateAuthority
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestTrigger()
    {
        // Apply at authority first
        CallLuaTriggerLocal();

        // Then propagate to everyone else (reliable, no local invoke here to avoid double fire on host)
        RPC_PropagateTrigger();
    }

    // StateAuthority -> All
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = false)]
    private void RPC_PropagateTrigger()
    {
        // Run on all peers (including authority peers, except the caller because InvokeLocal=false)
        CallLuaTriggerLocal();
    }

    // -----------------------
    // Haptics (optional)
    // -----------------------
    private void TryPulseHaptics(OVRInput.Controller controller, float amplitude, float duration)
    {
        // OVR recommends using a coroutine or the OVRHaptics APIs; this quick pulse uses OVRInput.SetControllerVibration.
        StartCoroutine(HapticPulse(controller, amplitude, duration));
    }

    private IEnumerator HapticPulse(OVRInput.Controller controller, float amplitude, float duration)
    {
        // frequency: 0-1, amplitude: 0-1
        OVRInput.SetControllerVibration(1f, Mathf.Clamp01(amplitude), controller);
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
