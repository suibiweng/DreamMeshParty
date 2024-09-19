using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPTParticles : MonoBehaviour
{
    public ParticleSystem SwordTrail;       // Reference to the sword trail particle system
    private ParticleSystem SubEmitter;      // Sub-emitting particle system for additional effects

    public Transform SwordTransform;        // The transform of the sword
    public float EmissionRate = 10f;        // Emission rate per unit distance

    void Start()
    {
        // Initialize the sword trail particle system
        SetupSwordTrail();

        // Create and configure sub-emitter if needed
        CreateSubEmitter();
    }

    void Update()
    {
        // Align the particle system with the sword
        SwordTrail.transform.position = SwordTransform.position;
        SwordTrail.transform.rotation = SwordTransform.rotation;
    }

    void SetupSwordTrail()
    {
        var mainModule = SwordTrail.main;
        mainModule.loop = true;
        mainModule.startLifetime = 0.5f;                        // Particles live for 0.5 seconds
        mainModule.startSpeed = 0f;                             // Particles stick to the sword's path
        mainModule.startSize = 0.2f;                            // Size of the particles
        mainModule.startColor = new Color(0.8f, 0.8f, 1f, 0.5f); // Light blue color with some transparency
        mainModule.gravityModifier = 0f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World; // Simulate in world space

        // Emission module to emit particles over distance
        var emissionModule = SwordTrail.emission;
        emissionModule.rateOverTime = 0f;
        emissionModule.rateOverDistance = EmissionRate;         // Emit particles based on sword movement

        // Shape module (not emitting from a shape)
        var shapeModule = SwordTrail.shape;
        shapeModule.enabled = false;

        // Trails module to add trailing effect to particles
        var trailsModule = SwordTrail.trails;
        trailsModule.enabled = true;
        trailsModule.mode = ParticleSystemTrailMode.PerParticle;
        trailsModule.ribbonCount = 1;
        trailsModule.lifetime = 0.5f;
        trailsModule.dieWithParticles = true;
        trailsModule.sizeAffectsWidth = true;
        trailsModule.sizeAffectsLifetime = false;
        trailsModule.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, 0f); // Taper the trail
        trailsModule.colorOverLifetime = new ParticleSystem.MinMaxGradient(new Gradient()
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.8f, 0.8f, 1f, 1f), 0f),
                new GradientColorKey(new Color(0.8f, 0.8f, 1f, 0f), 1f)
            },
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        });

        // Renderer settings for the sword trail
        var renderer = SwordTrail.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.SetColor("_Color", new Color(0.8f, 0.8f, 1f, 0.5f));
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.sortingOrder = 1;

        // Assign the sub-emitter
        var subEmittersModule = SwordTrail.subEmitters;
        subEmittersModule.enabled = true;
        subEmittersModule.AddSubEmitter(SubEmitter, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
    }

    void CreateSubEmitter()
    {
        // Create a new GameObject for the sub-emitter effect
        GameObject subEmitterGO = new GameObject("SubEmitter");
        subEmitterGO.transform.SetParent(transform);

        // Add a ParticleSystem component
        SubEmitter = subEmitterGO.AddComponent<ParticleSystem>();
        var mainModule = SubEmitter.main;
        mainModule.loop = false;
        mainModule.startLifetime = 0.3f;
        mainModule.startSpeed = 0f;
        mainModule.startSize = 0.1f;
        mainModule.startColor = new Color(1f, 1f, 1f, 0.5f);
        mainModule.gravityModifier = 0f;
        mainModule.simulationSpace = ParticleSystemSimulationSpace.World;

        var emissionModule = SubEmitter.emission;
        emissionModule.rateOverTime = 0f;

        var shapeModule = SubEmitter.shape;
        shapeModule.enabled = false;

        // Configure particle renderer
        var renderer = SubEmitter.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
}
