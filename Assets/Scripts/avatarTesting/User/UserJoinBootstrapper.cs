using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Fusion;   // if you’re using Fusion’s NetworkBehaviour
using RealityEditor; // your namespace where LuaMonoBehavior likely is

public class UserJoinBootstrapper : NetworkBehaviour
{
    [Header("Server Endpoint")]
    public string userTriggerUrl = "http://127.0.0.1:5000/userTrigger"; // your endpoint

    [Header("References")]
    public ProtectedLuaMonoBehavior protectedLua;  // place on a child GameObject
    public UserEffectRouter effectRouter;          // place on Player root (this GO)

    [Header("Player Parts With Colliders")]
    public Collider head;
    public Collider body;
    public Collider leftHand;
    public Collider rightHand;

    [Header("Optional")]
    public string urlid;   // set from your session manager
    public string userPrompt = "[This is a user]";

    // If you want this to only run on the local spawned player
    public bool onlyWhenInputAuthority = true;

    [System.Serializable]
    private class ServerPayload {
        public string urlid;
        public string dynamicCodingId;
        public string lua;
        public string sha256;
        public bool allowEdit;
    }

    public override void Spawned()
    {
        base.Spawned();
        if (onlyWhenInputAuthority && !Object.HasInputAuthority) return;
        StartCoroutine(FetchAndApply());
    }

    private IEnumerator FetchAndApply()
    {
        WWWForm form = new WWWForm();
        form.AddField("urlid", string.IsNullOrEmpty(urlid) ? Guid.NewGuid().ToString("N") : urlid);
        form.AddField("prompt", userPrompt);

        using (UnityWebRequest req = UnityWebRequest.Post(userTriggerUrl, form))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"UserJoinBootstrapper: request failed {req.error}");
                yield break;
            }

            var payload = JsonUtility.FromJson<ServerPayload>(req.downloadHandler.text);
            if (payload == null || string.IsNullOrEmpty(payload.lua))
            {
                Debug.LogError("UserJoinBootstrapper: bad payload");
                yield break;
            }

            // Verify hash if provided
            if (!string.IsNullOrEmpty(payload.sha256))
            {
                string actual = Sha256Hex(payload.lua);
                if (!actual.Equals(payload.sha256, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"UserJoinBootstrapper: Lua hash mismatch. Expected {payload.sha256}, got {actual}");
                    yield break;
                }
            }

            // Lock and apply
            protectedLua.ApplyLockedScript(payload.lua, payload.dynamicCodingId);

            // Route player part colliders to the router
            WireColliderRelays();
        }
    }

    private void WireColliderRelays()
    {
        if (effectRouter == null)
        {
            effectRouter = GetComponent<UserEffectRouter>();
            if (effectRouter == null)
                effectRouter = gameObject.AddComponent<UserEffectRouter>();
        }

        AddRelay(head);
        AddRelay(body);
        AddRelay(leftHand);
        AddRelay(rightHand);
    }

    private void AddRelay(Collider c)
    {
        if (c == null) return;
        var relay = c.gameObject.GetComponent<UserColliderRelay>();
        if (!relay) relay = c.gameObject.AddComponent<UserColliderRelay>();
        relay.Router = effectRouter;
    }

    private static string Sha256Hex(string text)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (byte b in hash) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
