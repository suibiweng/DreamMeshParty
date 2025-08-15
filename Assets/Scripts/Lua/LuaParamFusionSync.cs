using Fusion;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;

public enum LuaParamType : byte { Float = 0, Bool = 1, String = 2 }

public class LuaParamFusionSync : NetworkBehaviour
{
    [Header("Targets")]
    public LuaMonoBehavior target;                 // assign in Inspector
    public LuaParamUIBuilder uiBuilder;            // optional: to reflect incoming values visually

    // Guard to avoid echo loops when we set UI from network and UI would fire its handlers again
    private bool _suppressUIEvents;

    // -------- Public API: call this from your UI change handlers --------
    public void SendLocalChange(string key, object value)
    {
        if (string.IsNullOrEmpty(key)) return;

        // Decide param type + pack values
        if (value is bool bb)
        {
            RPC_RequestSetParam(key, LuaParamType.Bool, 0f, bb, "");
        }
        else if (value is float ff)
        {
            RPC_RequestSetParam(key, LuaParamType.Float, ff, false, "");
        }
        else if (value is double dd)
        {
            RPC_RequestSetParam(key, LuaParamType.Float, (float)dd, false, "");
        }
        else if (value is int ii)
        {
            RPC_RequestSetParam(key, LuaParamType.Float, (float)ii, false, "");
        }
        else
        {
            var s = value?.ToString() ?? "";
            RPC_RequestSetParam(key, LuaParamType.String, 0f, false, s);
        }
    }

    // Client/InputAuthority -> StateAuthority
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestSetParam(NetworkString<_32> key, LuaParamType type, float fVal, NetworkBool bVal, NetworkString<_64> sVal)
    {
        // Apply at the authority
        ApplyParamLocal(key.ToString(), type, fVal, bVal, sVal.ToString(), fromNetwork: false);

        // Rebroadcast to everyone (including authority peers); InvokeLocal=false to avoid double-apply here
        RPC_PropagateSetParam(key, type, fVal, bVal, sVal);
    }

    // StateAuthority -> All
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable, InvokeLocal = false)]
    private void RPC_PropagateSetParam(NetworkString<_32> key, LuaParamType type, float fVal, NetworkBool bVal, NetworkString<_64> sVal)
    {
        ApplyParamLocal(key.ToString(), type, fVal, bVal, sVal.ToString(), fromNetwork: true);
    }

    private void ApplyParamLocal(string key, LuaParamType type, float fVal, bool bVal, string sVal, bool fromNetwork)
    {
        if (target == null || target.Script == null) return;

        try
        {
            switch (type)
            {
                case LuaParamType.Float:
                    target.Script.Globals[key] = (double)fVal;
                    break;
                case LuaParamType.Bool:
                    target.Script.Globals[key] = bVal;
                    break;
                case LuaParamType.String:
                    target.Script.Globals[key] = sVal ?? "";
                    break;
            }

            // Optionally mirror to UI so remote peers see the slider/toggle move.
            if (uiBuilder != null)
            {
                _suppressUIEvents = true; // prevent loops while we set controls
                uiBuilder.SetControlVisual(key, type, fVal, bVal, sVal);
                _suppressUIEvents = false;

                // Keep the export dictionary aligned (handy for ApplyUIParamsToLua on Play)
                uiBuilder.luaParamValues[key] = type switch
                {
                    LuaParamType.Float  => (object)fVal,
                    LuaParamType.Bool   => (object)bVal,
                    LuaParamType.String => (object)(sVal ?? ""),
                    _ => sVal
                };
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LuaParamFusionSync] ApplyParamLocal failed for '{key}': {ex.Message}");
        }
    }

    // Helper for UI handlers to check if we’re currently applying a remote change
    public bool SuppressUIEvents => _suppressUIEvents;
}
