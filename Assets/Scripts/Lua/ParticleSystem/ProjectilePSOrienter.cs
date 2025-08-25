// ProjectilePSOrienter.cs
using UnityEngine;

/// <summary>
/// Orients the projectile particle system to a given SpawnPoint right before firing.
/// Does NOT Play(); Lua remains the source of truth for playing.
/// </summary>
[DisallowMultipleComponent]
public class ProjectilePSOrienter : MonoBehaviour
{
    [Tooltip("If left null, this component does nothing until assigned by LuaTriggerInteraction.")]
    public Transform SpawnPoint;

    [Tooltip("Auto-detected on Awake; can be overridden.")]
    public ParticleSystem ps;

    private void Awake()
    {
        if (!ps) ps = GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Orient the PS to SpawnPoint if this is a projectile (collision.enabled).
    /// Call this immediately before Lua trigger() plays any effects.
    /// </summary>
    public void OrientIfProjectile()
    {
        if (!ps || SpawnPoint == null) return;

        var col = ps.collision;
        if (!col.enabled) return; // only the shoot-out system

        // Ensure world-space and forward alignment for projectiles
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var shape = ps.shape;
        shape.alignToDirection = true;

        // Align transform to controller/muzzle
        transform.position = SpawnPoint.position;
        transform.rotation = SpawnPoint.rotation;
    }
}
