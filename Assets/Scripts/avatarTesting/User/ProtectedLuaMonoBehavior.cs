using UnityEngine;
using MoonSharp.Interpreter;

public class ProtectedLuaMonoBehavior : MonoBehaviour
{
    [TextArea(5, 20)]
    [SerializeField] private string luaScript;  // visible but ignored if locked
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string dynamicCodingId;

    private Script lua;
    private bool started;

    // Replace these with your actual bindings
    public System.Action<string> ActivateEffect; // bound to your particle system (activateEffect)

    public void ApplyLockedScript(string luaCode, string dynamicId)
    {
        isLocked = true;
        dynamicCodingId = dynamicId;
        luaScript = luaCode; // stored for inspection, but guarded by isLocked
        InitAndRun();
    }

    public void SetScript(string any)  // legacy API safeguard
    {
        if (isLocked)
        {
            Debug.LogWarning("ProtectedLuaMonoBehavior: script is locked; SetScript ignored.");
            return;
        }
        luaScript = any;
        InitAndRun();
    }

    private void InitAndRun()
    {
        lua = new Script(CoreModules.Preset_SoftSandbox);
        BindProxies();

        try
        {
            lua.DoString(luaScript ?? "");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ProtectedLuaMonoBehavior: DoString error: {e}");
        }

        // call start() if present
        var startFn = lua.Globals.Get("start");
        if (startFn.Type == DataType.Function)
        {
            try { lua.Call(startFn); started = true; }
            catch (System.Exception e) { Debug.LogError($"ProtectedLuaMonoBehavior: start() error: {e}"); }
        }
    }

    private void BindProxies()
    {
        // Bind your existing proxies (transformProxy, rigidbodyProxy, etc.) as usual.
        // Also bind activateEffect so Lua can call it.

        // Example minimal binding:
        lua.Globals["activateEffect"] = (System.Action<string>)((effectName) =>
        {
            if (string.IsNullOrEmpty(effectName)) return;
            ActivateEffect?.Invoke(effectName);
        });
    }

    public void DispatchOnTriggerEnter(GameObject other)
    {
        CallIfExists("onTriggerEnter", other);
    }

    public void DispatchOnCollisionEnter(GameObject other)
    {
        CallIfExists("onCollisionEnter", other);
    }

    private void CallIfExists(string fnName, GameObject other)
    {
        if (lua == null) return;
        var fn = lua.Globals.Get(fnName);
        if (fn.Type != DataType.Function) return;

        try
        {
            // If you have a GameObjectProxy, pass that; else pass a simple name
            lua.Call(fn, other != null ? other.name : "");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"ProtectedLuaMonoBehavior: {fnName} error: {e}");
        }
    }

    // Optional accessor if other systems need the id
    public string GetDynamicCodingId() => dynamicCodingId;
}
