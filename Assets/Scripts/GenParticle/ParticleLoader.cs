using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

[System.Serializable]
public class VelocityOverLifetime
{
    public bool enabled;
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class ColorKey
{
    public float[] color;
    public float time;
}

[System.Serializable]
public class ColorOverLifetime
{
    public bool enabled;
    public ColorKey[] gradient;
}

[System.Serializable]
public class NoiseModule
{
    public bool enabled;
    public float strength;
    public float frequency;
}

[System.Serializable]
public class RotationOverLifetime
{
    public bool enabled;
    public float angularVelocity;
}

[System.Serializable]
public class EmissionBurst
{
    public float time;
    public int count;
}

[System.Serializable]
public class ParticleConfig
{
    public float[] startColor;
    public float startSize;
    public float startSpeed;
    public float gravityModifier;
    public int maxParticles;
    public float emissionRate;
    public float lifetime;
    public string shape;
    public float shapeRadius;
    public VelocityOverLifetime velocityOverLifetime;
    public ColorOverLifetime colorOverLifetime;
    public NoiseModule noise;
    public RotationOverLifetime rotationOverLifetime;
    public EmissionBurst[] emissionBursts;
}

public class ParticleLoader : MonoBehaviour
{
    public GameObject targetObject; // Assign the target GameObject in the Inspector
    public string jsonUrl = "https://example.com/particleConfig.json"; // Replace with your actual URL

    void Start()
    {
        
    }

    public void setParticle(string url, GameObject target)
    {
        jsonUrl = url;
        targetObject = target;
        StartCoroutine(DownloadJsonAndApply(jsonUrl));
    }




    IEnumerator DownloadJsonAndApply(string jsonUrl)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(jsonUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching JSON: " + request.error);
            }
            else
            {
                string jsonString = request.downloadHandler.text;
                AttachParticleSystemToTarget(targetObject, jsonString);
            }
        }
    }

    public void AttachParticleSystemToTarget(GameObject target, string jsonString)
    {
        if (target == null)
        {
            Debug.LogError("Target object is null. Cannot attach particle system.");
            return;
        }

        // Try to find an existing ParticleSystem component on the target
        ParticleSystem particleSystem = target.GetComponent<ParticleSystem>();

        // If no ParticleSystem exists, create one
        if (particleSystem == null)
        {
            particleSystem = target.AddComponent<ParticleSystem>();
            Debug.Log("Created new ParticleSystem on target object.");
        }

        // Apply the particle settings
        LoadParticleConfig(particleSystem, jsonString);
    }

    public void LoadParticleConfig(ParticleSystem particleSystem, string jsonString)
    {
        try
        {
            ParticleConfig config = JsonUtility.FromJson<ParticleConfig>(jsonString);

            ParticleSystem.MainModule main = particleSystem.main;
            main.startColor = new Color(config.startColor[0], config.startColor[1], config.startColor[2], config.startColor[3]);
            main.startSize = config.startSize;
            main.startSpeed = config.startSpeed;
            main.maxParticles = config.maxParticles;
            main.startLifetime = config.lifetime;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = config.emissionRate;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = config.shape == "cone" ? ParticleSystemShapeType.Cone : ParticleSystemShapeType.Sphere;
            shape.radius = config.shapeRadius;

            // Apply Velocity Over Lifetime
            if (config.velocityOverLifetime.enabled)
            {
                ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = config.velocityOverLifetime.x;
                velocity.y = config.velocityOverLifetime.y;
                velocity.z = config.velocityOverLifetime.z;
            }

            // Apply Color Over Lifetime
            if (config.colorOverLifetime.enabled)
            {
                ParticleSystem.ColorOverLifetimeModule colorModule = particleSystem.colorOverLifetime;
                colorModule.enabled = true;

                Gradient gradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[config.colorOverLifetime.gradient.Length];
                for (int i = 0; i < config.colorOverLifetime.gradient.Length; i++)
                {
                    float[] colorValues = config.colorOverLifetime.gradient[i].color;
                    colorKeys[i] = new GradientColorKey(
                        new Color(colorValues[0], colorValues[1], colorValues[2], colorValues[3]),
                        config.colorOverLifetime.gradient[i].time
                    );
                }
                gradient.colorKeys = colorKeys;
                colorModule.color = new ParticleSystem.MinMaxGradient(gradient);
            }

            // Apply Noise
            if (config.noise.enabled)
            {
                ParticleSystem.NoiseModule noise = particleSystem.noise;
                noise.enabled = true;
                noise.strength = config.noise.strength;
                noise.frequency = config.noise.frequency;
            }

            // Apply Rotation Over Lifetime
            if (config.rotationOverLifetime.enabled)
            {
                ParticleSystem.RotationOverLifetimeModule rotation = particleSystem.rotationOverLifetime;
                rotation.enabled = true;
                rotation.z = config.rotationOverLifetime.angularVelocity;
            }

            // Apply Emission Bursts
            if (config.emissionBursts != null && config.emissionBursts.Length > 0)
            {
                ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[config.emissionBursts.Length];
                for (int i = 0; i < config.emissionBursts.Length; i++)
                {
                    bursts[i] = new ParticleSystem.Burst(config.emissionBursts[i].time, config.emissionBursts[i].count);
                }
                emission.SetBursts(bursts);
            }

            Debug.Log("Particle settings applied successfully from server!");
        }
        catch (Exception e)
        {
            Debug.LogError("Error parsing JSON: " + e.Message);
        }
    }
}
