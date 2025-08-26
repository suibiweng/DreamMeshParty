using Fusion;
using UnityEngine;
using MoonSharp.Interpreter;

public class LuaNetRPC : NetworkBehaviour
{
    [SerializeField] private LuaMonoBehavior target; // auto-binds if left empty

    // Exposed read-only status for UI polling (kept in sync on all clients)
    public bool IsRunning { get; private set; }

    public override void Spawned()
    {
        if (!target) target = GetComponent<LuaMonoBehavior>();
    }

    // UNITY-FACING CALLS (C#) -----------------------------------------------

    public void PlaySynced(bool run)
    {
        if (!Object) return;
        if (Object.HasStateAuthority) RPC_SetRunning(run);
        else if (Object.HasInputAuthority) RPC_RequestPlay(run);
    }

    public void TriggerSynced()
    {
        if (!Object) return;
        if (Object.HasStateAuthority) RPC_InvokeLua("trigger");
        else if (Object.HasInputAuthority) RPC_RequestTrigger();
    }

    public void InvokeLuaSynced(string funcName)
    {
        if (string.IsNullOrWhiteSpace(funcName) || !Object) return;
        if (Object.HasStateAuthority) RPC_InvokeLua(funcName);
        else if (Object.HasInputAuthority) RPC_RequestInvokeLua(funcName);
    }

    public void SetGlobalFloatSynced(string key, float value)
    {
        if (string.IsNullOrEmpty(key) || !Object) return;
        if (Object.HasStateAuthority) RPC_SetGlobalFloat(key, value);
        else if (Object.HasInputAuthority) RPC_RequestSetGlobalFloat(key, value);
    }

    public void SetGlobalBoolSynced(string key, bool value)
    {
        if (string.IsNullOrEmpty(key) || !Object) return;
        if (Object.HasStateAuthority) RPC_SetGlobalBool(key, value);
        else if (Object.HasInputAuthority) RPC_RequestSetGlobalBool(key, value);
    }

    public void SetGlobalStringSynced(string key, string value)
    {
        if (string.IsNullOrEmpty(key) || !Object) return;
        if (Object.HasStateAuthority) RPC_SetGlobalString(key, value ?? "");
        else if (Object.HasInputAuthority) RPC_RequestSetGlobalString(key, value ?? "");
    }

    public void ActivateEffectSynced(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName) || !Object) return;
        if (Object.HasStateAuthority) RPC_ActivateEffect(effectName);
        else if (Object.HasInputAuthority) RPC_RequestActivateEffect(effectName);
    }

    // CLIENT -> SERVER REQUESTS ---------------------------------------------

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestPlay(bool run, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_SetRunning(run);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestTrigger(RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_InvokeLua("trigger");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestInvokeLua(string funcName, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_InvokeLua(funcName);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetGlobalFloat(string key, float value, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_SetGlobalFloat(key, value);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetGlobalBool(string key, bool value, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_SetGlobalBool(key, value);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetGlobalString(string key, string value, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_SetGlobalString(key, value ?? "");
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestActivateEffect(string effectName, RpcInfo info = default)
    {
        if (!Object.HasStateAuthority) return;
        RPC_ActivateEffect(effectName ?? "");
    }

    // SERVER -> ALL (fan-out) -----------------------------------------------

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetRunning(bool run, RpcInfo info = default)
    {
        if (target)
        {
            if (run) target.Play();
            else     target.Stop();
        }
        IsRunning = run; // cache for UI on all peers
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_InvokeLua(string funcName, RpcInfo info = default)
    {
        if (!target || string.IsNullOrWhiteSpace(funcName)) return;
        var f = target.Script?.Globals.Get(funcName);
        if (f != null && f.Type == DataType.Function)
            target.Script.Call(f);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetGlobalFloat(string key, float value, RpcInfo info = default)
    {
        if (!target || target.Script == null || string.IsNullOrEmpty(key)) return;
        target.Script.Globals[key] = value;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetGlobalBool(string key, bool value, RpcInfo info = default)
    {
        if (!target || target.Script == null || string.IsNullOrEmpty(key)) return;
        target.Script.Globals[key] = value;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_SetGlobalString(string key, string value, RpcInfo info = default)
    {
        if (!target || target.Script == null || string.IsNullOrEmpty(key)) return;
        target.Script.Globals[key] = value ?? "";
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ActivateEffect(string effectName, RpcInfo info = default)
    {
        if (!target) return;
        target.SendMessage("ActivateEffect", effectName, SendMessageOptions.DontRequireReceiver);
    }
}
