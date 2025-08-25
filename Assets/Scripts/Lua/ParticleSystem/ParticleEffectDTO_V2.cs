// ParticleEffectDTO_V2.cs
using UnityEngine;
using System;
using System.Collections.Generic;
[Serializable] public struct FloatKey { public float time; public float value; }
[Serializable] public struct ColorRGBA { public float r, g, b, a; }
[Serializable] public struct Vec3 { public float x, y, z; }

[Serializable]
public class MinMaxCurveDTO {
    public string mode;              // "Constant" | "Curve" | "TwoConstants" | "TwoCurves"
    public float constant;
    public float constantMin;
    public float constantMax;
    public FloatKey[] curve;
    public FloatKey[] curveMin;
    public FloatKey[] curveMax;
}

[Serializable] public class GradientColorKeyDTO { public float time; public ColorRGBA color; }
[Serializable] public class GradientAlphaKeyDTO { public float time; public float alpha; }
[Serializable] public class GradientDTO { public GradientColorKeyDTO[] colorKeys; public GradientAlphaKeyDTO[] alphaKeys; }

[Serializable]
public class MainModuleDTO {
    public float gravityModifier;
    public string simulationSpace;   // "Local" | "World"
    public string scalingMode;       // "Local" | "Hierarchy" | "Shape"
    public bool prewarm;
    public bool playOnAwake;
    public float duration;
    public ColorRGBA startColor;
    public float startSize;
    public float startSpeed;
    public float startLifetime;
    public int   maxParticles = 1000;
    public bool  loop = true;
}

[Serializable]
public class ShapeModuleDTO {
    public string type; // "Cone","Sphere","Hemisphere","Box","Circle"
    public float angle;
    public float radius;
    public float arc;
    public Vec3  position;
}

[Serializable] public class BurstDTO { public float time; public short count; public int cycleCount; public float repeatInterval; }
[Serializable] public class EmissionDTO { public float rateOverTime; public float rateOverDistance; public BurstDTO[] bursts; }

[Serializable]
public class VelocityOverLifetimeDTO {
    public bool enabled; public string space; // "Local" | "World"
    public MinMaxCurveDTO x, y, z;
    public float orbitalY;
    public MinMaxCurveDTO speedModifier;
}

[Serializable] public class ColorOverLifetimeDTO   { public bool enabled; public GradientDTO    gradient; }
[Serializable] public class SizeOverLifetimeDTO    { public bool enabled; public bool separateAxes; public MinMaxCurveDTO size; }
[Serializable] public class RotationOverLifetimeDTO{ public bool enabled; public bool separateAxes; public MinMaxCurveDTO x, y, z; }

[Serializable]
public class NoiseDTO {
    public bool enabled; public float strength; public float frequency; public float scrollSpeed;
    public int octaveCount; public string quality; // "Low" | "Medium" | "High"
}

[Serializable] public class TrailsDTO { public bool enabled; public float lifetime; public float ratio; public bool dieWithParticles; }

[Serializable]
public class RendererDTO {
    public string renderMode;          // "Billboard","StretchedBillboard","HorizontalBillboard","VerticalBillboard","Mesh"
    public string materialName;        // Resources key or resolver key
    public float  sortingFudge;
}

[Serializable]
public class CollisionDTO
{
    public bool   enabled;
    public string type  = "World";     // "World" | "Planes"
    public string mode  = "3D";        // "3D" | "2D"

    // Choose ONE input: mask (preferred by code) or layers (author-friendly)
    public int?    collidesWithMask;   // e.g., 1<<Default | 1<<GeneratedObject
    public string[] collidesWithLayers;

    // Messaging / relay
    public bool sendCollisionMessages = true;
    public bool attachRelay = true;

    // Response
    public float dampen = 0f;          // 0..1
    public float bounce = 0f;          // 0..1
    public float lifetimeLoss = 0f;    // 0..1 (1 = kill on impact)
    public float minKillSpeed = 0f;
    public float radiusScale = 1f;

    // Quality / perf
    public string quality = "High";    // "Low" | "Medium" | "High"
    public float voxelSize = 0.5f;
    public int   maxCollisionShapes = 256;
    public bool  enableDynamicColliders = false;
    public bool  multiplyColliderForceByParticleSize = false;
}

[Serializable]
public class ParticleEffectConfigV2
{
    // Back‑compat (optional)
    public string effectName;
    public float duration;
    public ColorRGBA startColor;
    public float startSize;
    public float startSpeed;
    public float emissionRate;
    public float lifetime;
    public int   maxParticles;
    public string shape;

    // Authority modules
    public MainModuleDTO            main;
    public ShapeModuleDTO           shapeModule;
    public EmissionDTO              emission;
    public VelocityOverLifetimeDTO  velocityOverLifetime;
    public ColorOverLifetimeDTO     colorOverLifetime;
    public SizeOverLifetimeDTO      sizeOverLifetime;
    public RotationOverLifetimeDTO  rotationOverLifetime;
    public NoiseDTO                 noise;
    public TrailsDTO                trails;
    public RendererDTO              renderer;

    // Single source of truth for collisions
    public CollisionDTO             collision;
}

// Root for V2
[Serializable]
public class DynamicObjectDataV2
{
    public string object_name;
    public string lua_code;
    public string comment;
    public string created_at;
    public ParticleEffectConfigV2[] particle_json;
    public List<RawParamUIDef>      param_ui_particle;
    public List<RawParamUIDef>      param_ui_lua;
}
