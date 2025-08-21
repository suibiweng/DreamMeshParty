using UnityEngine;
using RealityEditor; // for GeneratedColliderMarker

/// <summary>
/// Routes collision/trigger events to a parent (or chosen target) ONLY when:
/// 1) They involve a specific inner collider (or one of several), OR
/// 2) If no inner set: the collider is "generated" (marker or layer).
///
/// Attach to the RigidBody holder (usually the parent).
/// Consumers can implement:
///   void OnInnerCollisionEnter(Collision c)
///   void OnInnerCollisionStay(Collision c)
///   void OnInnerCollisionExit(Collision c)
///   void OnInnerTriggerEnter(Collider other)
///   void OnInnerTriggerStay(Collider other)
///   void OnInnerTriggerExit(Collider other)
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class InnerCompoundRouter : MonoBehaviour
{
    [Header("Inner collider filtering")]
    [Tooltip("Primary inner collider to listen for (usually a child).")]
    public Collider inner;

    [Tooltip("Optional additional inner colliders that should also be accepted.")]
    public Collider[] additionalInners;

    [Tooltip("If no inner is assigned, accept only from generated colliders (marker or layer).")]
    public bool onlyFromGenerated = true;

    [Tooltip("Layer name used to tag generated hulls (fallback if no marker).")]
    public string generatedLayerName = "GeneratedObject";

    [Header("Forwarding")]
    [Tooltip("Where to forward messages. If null, uses this GameObject.")]
    public GameObject forwardTarget;

    [Tooltip("If true, use SendMessageUpwards; otherwise SendMessage to the target only.")]
    public bool forwardUpwards = true;

    [Tooltip("Also forward the standard Unity message names (OnCollisionEnter/Exit/Stay, OnTriggerEnter/Exit/Stay) in addition to the 'OnInner*' ones.")]
    public bool alsoForwardStandard = false;

    [Header("Extras")]
    [Tooltip("Forward only if the collision impulse magnitude is at least this value (0 = no threshold).")]
    public float minImpulse = 0f;

    [Tooltip("Also route trigger events (if your setup uses triggers).")]
    public bool useTriggersToo = false;

    [Tooltip("Print basic routing decisions to the Console.")]
    public bool debugLogs = false;

    // cache
    private int _genLayer = -1;
    private System.Collections.Generic.HashSet<Collider> _innerSet;

    void Awake()
    {
        // Resolve generated layer once
        _genLayer = string.IsNullOrEmpty(generatedLayerName) ? -1 : LayerMask.NameToLayer(generatedLayerName);

        // Build inner set
        _innerSet = new System.Collections.Generic.HashSet<Collider>();
        if (inner) _innerSet.Add(inner);
        if (additionalInners != null)
        {
            for (int i = 0; i < additionalInners.Length; i++)
                if (additionalInners[i]) _innerSet.Add(additionalInners[i]);
        }

        if (!forwardTarget) forwardTarget = gameObject;
    }

    /// <summary>Assign/replace the inner collider at runtime.</summary>
    public void SetInner(Collider c, bool clearOthers = true)
    {
        if (clearOthers) _innerSet.Clear();
        if (c) _innerSet.Add(c);
        inner = c;
        if (debugLogs) Debug.Log($"[InnerCompoundRouter] SetInner -> {(c ? c.name : "null")}", this);
    }

    // ---------- COLLISIONS ----------
    void OnCollisionEnter(Collision c)
    {
        if (!ShouldForward(c)) return;
        ForwardCollision("OnInnerCollisionEnter", c);
    }

    void OnCollisionStay(Collision c)
    {
        if (!ShouldForward(c)) return;
        ForwardCollision("OnInnerCollisionStay", c);
    }

    void OnCollisionExit(Collision c)
    {
        if (!ShouldForward(c)) return;
        ForwardCollision("OnInnerCollisionExit", c);
    }

    // ---------- TRIGGERS (optional) ----------
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggersToo) return;
        if (!ShouldForward(other)) return;
        ForwardTrigger("OnInnerTriggerEnter", other);
    }

    void OnTriggerStay(Collider other)
    {
        if (!useTriggersToo) return;
        if (!ShouldForward(other)) return;
        ForwardTrigger("OnInnerTriggerStay", other);
    }

    void OnTriggerExit(Collider other)
    {
        if (!useTriggersToo) return;
        if (!ShouldForward(other)) return;
        ForwardTrigger("OnInnerTriggerExit", other);
    }

    // ---------- FILTERS ----------
    bool ShouldForward(Collision c)
    {
        // Impulse threshold (Enter/Stay have impulse; Exit usually has zero)
        if (minImpulse > 0f && c.impulse.magnitude < minImpulse)
        {
            if (debugLogs) Debug.Log($"[InnerCompoundRouter] Blocked by impulse<{minImpulse}: {c.impulse.magnitude}", this);
            return false;
        }

        // Exact inner match takes precedence
        if (_innerSet != null && _innerSet.Count > 0)
        {
            for (int i = 0; i < c.contactCount; i++)
            {
                var mine = c.GetContact(i).thisCollider;
                if (mine && _innerSet.Contains(mine))
                    return true;
            }
            if (debugLogs) Debug.Log("[InnerCompoundRouter] Collision ignored: no contact with specified inner collider(s).", this);
            return false;
        }

        // Fallback: generated only?
        if (!onlyFromGenerated) return true;

        for (int i = 0; i < c.contactCount; i++)
        {
            var mine = c.GetContact(i).thisCollider;
            if (!mine) continue;
            if (IsGenerated(mine)) return true;
        }

        if (debugLogs) Debug.Log("[InnerCompoundRouter] Collision ignored: not from generated collider/layer.", this);
        return false;
    }

    bool ShouldForward(Collider mine)
    {
        // Exact inner match
        if (_innerSet != null && _innerSet.Count > 0)
            return mine && _innerSet.Contains(mine);

        // Fallback: generated only?
        if (!onlyFromGenerated) return true;

        return mine && IsGenerated(mine);
    }

    bool IsGenerated(Collider col)
    {
        if (!col) return false;
        if (col.GetComponent<GeneratedColliderMarker>() != null) return true;
        return (_genLayer != -1 && col.gameObject.layer == _genLayer);
    }

    // ---------- FORWARDERS ----------
    void ForwardCollision(string innerMethod, Collision payload)
    {
        if (!forwardTarget) forwardTarget = gameObject;

        if (forwardUpwards) forwardTarget.SendMessageUpwards(innerMethod, payload, SendMessageOptions.DontRequireReceiver);
        else               forwardTarget.SendMessage(innerMethod, payload, SendMessageOptions.DontRequireReceiver);

        if (alsoForwardStandard)
        {
            // Map to standard Unity names (strip "Inner")
            string std = innerMethod.Replace("Inner", string.Empty);
            if (forwardUpwards) forwardTarget.SendMessageUpwards(std, payload, SendMessageOptions.DontRequireReceiver);
            else               forwardTarget.SendMessage(std, payload, SendMessageOptions.DontRequireReceiver);
        }

        if (debugLogs) Debug.Log($"[InnerCompoundRouter] -> {innerMethod} to {(forwardUpwards ? "Upwards" : "Target")} ({forwardTarget.name})", this);
    }

    void ForwardTrigger(string innerMethod, Collider payload)
    {
        if (!forwardTarget) forwardTarget = gameObject;

        if (forwardUpwards) forwardTarget.SendMessageUpwards(innerMethod, payload, SendMessageOptions.DontRequireReceiver);
        else               forwardTarget.SendMessage(innerMethod, payload, SendMessageOptions.DontRequireReceiver);

        if (alsoForwardStandard)
        {
            string std = innerMethod.Replace("Inner", string.Empty);
            if (forwardUpwards) forwardTarget.SendMessageUpwards(std, payload, SendMessageOptions.DontRequireReceiver);
            else               forwardTarget.SendMessage(std, payload, SendMessageOptions.DontRequireReceiver);
        }

        if (debugLogs) Debug.Log($"[InnerCompoundRouter] -> {innerMethod} to {(forwardUpwards ? "Upwards" : "Target")} ({forwardTarget.name})", this);
    }
}
