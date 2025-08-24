// ParticleDTOApplier_V2.cs
using UnityEngine;

public static class ParticleDTOApplier_V2
{
    static Gradient ToGradient(GradientDTO dto)
    {
        var g = new Gradient();
        if (dto == null) return g;
        var ck = new GradientColorKey[dto.colorKeys?.Length ?? 0];
        var ak = new GradientAlphaKey[dto.alphaKeys?.Length ?? 0];
        for (int i = 0; i < ck.Length; i++)
        {
            var c = dto.colorKeys[i].color;
            ck[i] = new GradientColorKey(new Color(c.r, c.g, c.b, 1f), dto.colorKeys[i].time);
        }
        for (int i = 0; i < ak.Length; i++)
            ak[i] = new GradientAlphaKey(dto.alphaKeys[i].alpha, dto.alphaKeys[i].time);
        g.SetKeys(ck, ak);
        return g;
    }

    static AnimationCurve ToCurve(FloatKey[] keys)
    {
        var curve = new AnimationCurve();
        if (keys != null)
            foreach (var k in keys) curve.AddKey(k.time, k.value);
        return curve;
    }

    static ParticleSystem.MinMaxCurve ToMinMax(MinMaxCurveDTO m)
    {
        if (m == null || string.IsNullOrEmpty(m.mode)) return new ParticleSystem.MinMaxCurve(0f);
        switch (m.mode)
        {
            case "Constant":     return new ParticleSystem.MinMaxCurve(m.constant);
            case "Curve":        return new ParticleSystem.MinMaxCurve(1f, ToCurve(m.curve));
            case "TwoConstants": return new ParticleSystem.MinMaxCurve(m.constantMin, m.constantMax);
            case "TwoCurves":    return new ParticleSystem.MinMaxCurve(1f, ToCurve(m.curveMin), ToCurve(m.curveMax));
            default:             return new ParticleSystem.MinMaxCurve(m.constant);
        }
    }

    public static void Apply(ParticleSystem ps, ParticleEffectConfigV2 cfg, Material fallbackMat = null)
    {
        if (!ps || cfg == null) return;

        var main = ps.main;

        // Back‑compat start values if present
        if (cfg.startColor.a != 0f || cfg.startColor.r != 0f || cfg.startColor.g != 0f || cfg.startColor.b != 0f)
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(cfg.startColor.r, cfg.startColor.g, cfg.startColor.b, cfg.startColor.a));
        if (cfg.startSize != 0f)   main.startSize = cfg.startSize;
        if (cfg.startSpeed != 0f)  main.startSpeed = cfg.startSpeed;
        if (cfg.lifetime != 0f)    main.startLifetime = cfg.lifetime;
        if (cfg.maxParticles > 0)  main.maxParticles = cfg.maxParticles;
        if (cfg.duration > 0f)     main.duration = cfg.duration;

        if (cfg.main != null)
        {
            main.gravityModifier = cfg.main.gravityModifier;
            main.simulationSpace = cfg.main.simulationSpace == "World" ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.scalingMode = cfg.main.scalingMode == "Hierarchy" ? ParticleSystemScalingMode.Hierarchy :
                               cfg.main.scalingMode == "Shape" ? ParticleSystemScalingMode.Shape : ParticleSystemScalingMode.Local;
            main.prewarm = cfg.main.prewarm;
            main.playOnAwake = cfg.main.playOnAwake;
        }

        var shape = ps.shape;
        if (cfg.shapeModule != null)
        {
            shape.enabled = true;
            shape.position = new Vector3(cfg.shapeModule.position.x, cfg.shapeModule.position.y, cfg.shapeModule.position.z);
            shape.arc = cfg.shapeModule.arc;
            shape.radius = cfg.shapeModule.radius;
            shape.angle = cfg.shapeModule.angle;

            switch (cfg.shapeModule.type)
            {
                case "Cone":        shape.shapeType = ParticleSystemShapeType.Cone; break;
                case "Sphere":      shape.shapeType = ParticleSystemShapeType.Sphere; break;
                case "Hemisphere":  shape.shapeType = ParticleSystemShapeType.Hemisphere; break;
                case "Box":         shape.shapeType = ParticleSystemShapeType.Box; break;
                case "Circle":      shape.shapeType = ParticleSystemShapeType.Circle; break;
            }
        }
        else if (!string.IsNullOrEmpty(cfg.shape))
        {
            shape.enabled = true;
            switch (cfg.shape)
            {
                case "Cone": shape.shapeType = ParticleSystemShapeType.Cone; break;
                case "Sphere": shape.shapeType = ParticleSystemShapeType.Sphere; break;
                case "Box": shape.shapeType = ParticleSystemShapeType.Box; break;
            }
        }

        var emission = ps.emission;
        emission.enabled = true;
        if (cfg.emission != null)
        {
            emission.rateOverTime = cfg.emission.rateOverTime;
            emission.rateOverDistance = cfg.emission.rateOverDistance;
            if (cfg.emission.bursts != null)
            {
                emission.SetBursts(null);
                var bursts = new ParticleSystem.Burst[cfg.emission.bursts.Length];
                for (int i = 0; i < bursts.Length; i++)
                {
                    var b = cfg.emission.bursts[i];
                    var burst = new ParticleSystem.Burst(b.time, b.count);
                    burst.cycleCount = Mathf.Max(1, b.cycleCount);
                    burst.repeatInterval = Mathf.Max(0f, b.repeatInterval);
                    bursts[i] = burst;
                }
                emission.SetBursts(bursts);
            }
        }
        else if (cfg.emissionRate != 0f)
        {
            emission.rateOverTime = cfg.emissionRate;
        }

        if (cfg.velocityOverLifetime != null)
        {
            var vol = ps.velocityOverLifetime;
            vol.enabled = cfg.velocityOverLifetime.enabled;
            vol.space = cfg.velocityOverLifetime.space == "World" ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            vol.x = ToMinMax(cfg.velocityOverLifetime.x);
            vol.y = ToMinMax(cfg.velocityOverLifetime.y);
            vol.z = ToMinMax(cfg.velocityOverLifetime.z);
            vol.orbitalY = cfg.velocityOverLifetime.orbitalY;
            vol.speedModifier = ToMinMax(cfg.velocityOverLifetime.speedModifier);
        }

        if (cfg.colorOverLifetime != null)
        {
            var col = ps.colorOverLifetime;
            col.enabled = cfg.colorOverLifetime.enabled;
            if (cfg.colorOverLifetime.enabled && cfg.colorOverLifetime.gradient != null)
                col.color = new ParticleSystem.MinMaxGradient(ToGradient(cfg.colorOverLifetime.gradient));
        }

        if (cfg.sizeOverLifetime != null)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = cfg.sizeOverLifetime.enabled;
            if (cfg.sizeOverLifetime.enabled)
            {
                sol.separateAxes = cfg.sizeOverLifetime.separateAxes;
                sol.size = ToMinMax(cfg.sizeOverLifetime.size);
            }
        }

        if (cfg.rotationOverLifetime != null)
        {
            var rol = ps.rotationOverLifetime;
            rol.enabled = cfg.rotationOverLifetime.enabled;
            if (cfg.rotationOverLifetime.enabled)
            {
                rol.separateAxes = cfg.rotationOverLifetime.separateAxes;
                rol.z = ToMinMax(cfg.rotationOverLifetime.z);
            }
        }




        // Simulation space (World vs Local)
if (cfg.main != null && !string.IsNullOrEmpty(cfg.main.simulationSpace))
{
    var ms = cfg.main.simulationSpace.Trim().ToLowerInvariant();
    main.simulationSpace = (ms == "world")
        ? ParticleSystemSimulationSpace.World
        : ParticleSystemSimulationSpace.Local;
}

// Collision (World)
if (cfg.collision != null)
{
    var col = ps.collision;
    col.enabled = cfg.collision.enabled;
    col.sendCollisionMessages = cfg.collision.sendCollisionMessages;

    // type
    var t = (cfg.collision.type ?? "World").Trim().ToLowerInvariant();
    col.type = (t == "world")
        ? ParticleSystemCollisionType.World
        : ParticleSystemCollisionType.Planes;

    // quality
    var q = (cfg.collision.quality ?? "High").Trim().ToLowerInvariant();
    col.quality = q == "low" ? ParticleSystemCollisionQuality.Low
               : q == "medium" ? ParticleSystemCollisionQuality.Medium
               : ParticleSystemCollisionQuality.High;

    // radius scale
    if (cfg.collision.radiusScale > 0f) col.radiusScale = cfg.collision.radiusScale;

    // collidesWith (comma‑separated layer names)
    if (!string.IsNullOrWhiteSpace(cfg.collision.collidesWith))
    {
        int mask = 0;
        foreach (var part in cfg.collision.collidesWith.Split(','))
        {
            var layerName = part.Trim();
            if (string.IsNullOrEmpty(layerName)) continue;
            int lid = LayerMask.NameToLayer(layerName);
            if (lid >= 0) mask |= (1 << lid);
        }
        if (mask != 0) col.collidesWith = mask;
    }
}

        
        



        

        if (cfg.noise != null)
        {
            var noise = ps.noise;
            noise.enabled = cfg.noise.enabled;
            if (cfg.noise.enabled)
            {
                noise.strength = cfg.noise.strength;
                noise.frequency = Mathf.Max(0.001f, cfg.noise.frequency);
                noise.scrollSpeed = cfg.noise.scrollSpeed;
                noise.octaveCount = Mathf.Clamp(cfg.noise.octaveCount, 1, 3);
                noise.quality = cfg.noise.quality == "Low" ? ParticleSystemNoiseQuality.Low :
                                cfg.noise.quality == "Medium" ? ParticleSystemNoiseQuality.Medium : ParticleSystemNoiseQuality.High;
            }
        }

        if (cfg.trails != null)
        {
            var trails = ps.trails;
            trails.enabled = cfg.trails.enabled;
            if (cfg.trails.enabled)
            {
                trails.lifetime = cfg.trails.lifetime;
                trails.ratio = cfg.trails.ratio;
                trails.dieWithParticles = cfg.trails.dieWithParticles;
            }
        }

        if (cfg.renderer != null)
        {
            var rdr = ps.GetComponent<ParticleSystemRenderer>();
            if (!rdr) rdr = ps.gameObject.AddComponent<ParticleSystemRenderer>();

            switch (cfg.renderer.renderMode)
            {
                case "StretchedBillboard": rdr.renderMode = ParticleSystemRenderMode.Stretch; break;
                case "HorizontalBillboard": rdr.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
                case "VerticalBillboard":   rdr.renderMode = ParticleSystemRenderMode.VerticalBillboard; break;
                case "Mesh":                rdr.renderMode = ParticleSystemRenderMode.Mesh; break;
                default:                    rdr.renderMode = ParticleSystemRenderMode.Billboard; break;
            }

            rdr.sortingFudge = cfg.renderer.sortingFudge;

            if (!string.IsNullOrEmpty(cfg.renderer.materialName))
            {
                var mat = Resources.Load<Material>(cfg.renderer.materialName);
                if (mat) rdr.material = mat;
                else if (fallbackMat) rdr.material = fallbackMat;
            }
            else if (fallbackMat) rdr.material = fallbackMat;
        }
    }
}
