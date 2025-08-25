using UnityEngine;
using Fusion;

public class UserEffectRouter : NetworkBehaviour
{
    public ProtectedLuaMonoBehavior protectedLua;

    [Header("Effect Mapping (names in your DynamicCoding.json)")]
    public string UserTouchEffect = "UserTouchEffect"; // touching something
    public string UserHitEffect = "UserHitEffect";     // something hits you

    private void Awake()
    {
        if (!protectedLua)
            protectedLua = GetComponentInChildren<ProtectedLuaMonoBehavior>(true);

        if (protectedLua != null)
        {
            protectedLua.ActivateEffect = effectName => TrySpawnNetworkEffect(effectName);
        }
    }

    public void OnUserTriggerEnter(GameObject selfPart, GameObject other)
    {
        // Call Lua hook
        protectedLua?.DispatchOnTriggerEnter(other);

        // Also network the effect name (in case Lua or mapping wants it)
        if (!string.IsNullOrEmpty(UserTouchEffect))
            TrySpawnNetworkEffect(UserTouchEffect);
    }

    public void OnUserCollisionEnter(GameObject selfPart, GameObject other)
    {
        protectedLua?.DispatchOnCollisionEnter(other);

        if (!string.IsNullOrEmpty(UserHitEffect))
            TrySpawnNetworkEffect(UserHitEffect);
    }

    private void TrySpawnNetworkEffect(string effectName)
    {
        if (string.IsNullOrEmpty(effectName)) return;

        if (Object != null && (Object.HasStateAuthority || Object.HasInputAuthority))
        {
            RPC_SpawnEffect(effectName);
        }
        else
        {
            // Non-authority can still request locally if your FX is cosmetic
            // But for strict sync rely on RPC only
        }
    }
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
private void RPC_SpawnEffect(string effectName)
{
    var dynamicId = protectedLua != null ? protectedLua.GetDynamicCodingId() : null;
    SimpleEffectSpawner.Spawn(effectName, dynamicId, transform);
}

}
