using UnityEngine;
using MoonSharp.Interpreter;

[RequireComponent(typeof(ParticleSystem))]
public class ParticleCollisionRelay : MonoBehaviour
{
    [Tooltip("Lua script that owns/plays this particle effect (the shooter).")]
    public LuaMonoBehavior owner;  // assign the gun's LuaMonoBehavior

    void OnParticleCollision(GameObject other)
    {
        // 1) Call trigger() on the object that was hit (if it has Lua)
        var hitLua = other.GetComponent<LuaMonoBehavior>();
        if (hitLua != null && hitLua.Script != null)
        {
            try
            {
                var fn = hitLua.Script.Globals.Get("trigger");
                if (fn.Type == DataType.Function) hitLua.Script.Call(fn);
            }
            catch (System.Exception e) { Debug.LogWarning($"trigger() on hit object failed: {e.Message}"); }
        }

        // 2) Notify the shooter’s Lua (optional callback)
        if (owner != null && owner.Script != null)
        {
            try
            {
                var cb = owner.Script.Globals.Get("onParticleHit");
                if (cb.Type == DataType.Function) owner.Script.Call(cb, other.name);
            }
            catch (System.Exception e) { Debug.LogWarning($"onParticleHit callback failed: {e.Message}"); }
        }
    }
}
