using UnityEngine;
using UnityEngine.Networking;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaProxies; // ✅ Import the namespace
using UnityEngine.UI;

[System.Serializable]
public class ParticleEffectConfig
{
    public string effectName;
    public float duration;
    public Color startColor;
    public float startSize;
    public float startSpeed;
    public int maxParticles;
    public string shape;
    public float emissionRate;
    public float lifetime;
}

[System.Serializable]
public class DynamicObjectData
{
    public string objectName;
    public string lua_code;
    public List<ParticleEffectConfig> particle_json;
}

public class LuaMonoBehavior : MonoBehaviour
{
    public Transform SpwanPoint;
    public string ID; // Object ID to fetch JSON (e.g., "Campfire")
    public string serverURL = "http://yourserver.com/"; // Set your server URL here
    public Text uiText;
    public Button uiButton;
    public Rigidbody rb;
    public string luaScriptText;
    public float checkInterval = 10f;
    public Material defaultParticleMaterial; // Set this in the inspector
    public event Action<bool> OnURLResponse = delegate { };

    private Script luaScript;
    private UnityEngine.Coroutine fileCheckCoroutine;
    private bool isDownloading = false;

    // Lua function handles
    private DynValue startFunction;
    private DynValue updateFunction;
    private DynValue fixedUpdateFunction;
    private DynValue lateUpdateFunction;
    private DynValue onTriggerEnterFunction;
    private DynValue onTriggerExitFunction;
    private DynValue onCollisionEnterFunction;
    private DynValue onCollisionExitFunction;
    private DynValue onButtonClickFunction;

    // Proxies for Unity components available in Lua
    TransformProxy transformProxy;
    GameObjectProxy gameObjectProxy;

    // Dictionary to store ParticleSystems for each effect
    private Dictionary<string, ParticleSystem> effectSystems = new Dictionary<string, ParticleSystem>();

    void Start()
    {
        // Register Lua types
        UserData.RegisterType<GameObject>();
        UserData.RegisterType<Vector3>();
        UserData.RegisterType<TransformProxy>(); // from LuaProxies namespace
        UserData.RegisterType<GameObjectProxy>();  // from LuaProxies namespace

        // Initialize proxies
        transformProxy = new TransformProxy(transform);
        gameObjectProxy = new GameObjectProxy(gameObject);

        // Initialize Lua
        luaScript = new Script();

        // (Default Lua script can be set here if needed)
        // Now, for dynamic objects we'll fetch our JSON file
    }

    // Fetch and process JSON from server
    public void StartFetchingCode(string downloadURL, string downloadID)
    {
        if (fileCheckCoroutine == null)
        {
            string urlToCheck = downloadURL + downloadID + "_DynamicCoding.json";
            fileCheckCoroutine = StartCoroutine(CheckFileAvailability(urlToCheck));
        }
    }

    private IEnumerator CheckFileAvailability(string url)
    {
        yield return new WaitForSeconds(10f);

        while (!isDownloading)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("✅ JSON file is available! Downloading...");
                    isDownloading = true;
                    ProcessJsonData(www.downloadHandler.text);
                    OnURLResponse(true);
                }
                else
                {
                    OnURLResponse(false);
                }
            }
            if (!isDownloading)
            {
                yield return new WaitForSeconds(checkInterval);
            }
        }
        fileCheckCoroutine = null;
    }

    // Process JSON data from a file hosted on the server
    void ProcessJsonData(string json)
    {
        DynamicObjectData data = JsonUtility.FromJson<DynamicObjectData>(json);

        luaScriptText= data.lua_code;
        

        // Load Lua code from JSON
        InitializeLuaScript(data.lua_code);

        // Create or update ParticleSystems for each effect in the JSON
        foreach (ParticleEffectConfig effect in data.particle_json)
        {
            CreateOrUpdateParticleSystem(effect);
        }
    }

    // Create or update a ParticleSystem for a given effect config
    void CreateOrUpdateParticleSystem(ParticleEffectConfig config)
    {
        ParticleSystem ps;
        if (effectSystems.ContainsKey(config.effectName))
        {
            ps = effectSystems[config.effectName];
        }
        else
        {
            // Create a new child GameObject to hold the ParticleSystem
            GameObject psObject = new GameObject(config.effectName + "_Effect");
            psObject.transform.parent = SpwanPoint;
            psObject.transform.localPosition = Vector3.zero;
            psObject.transform.rotation = Quaternion.Euler(new Vector3(-90, 0, 0));
            psObject.transform.localScale = Vector3.one;


            ps = psObject.AddComponent<ParticleSystem>();

            // Set up the ParticleSystemRenderer with a default material
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (defaultParticleMaterial != null)
                renderer.material = defaultParticleMaterial;
            else
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

            effectSystems[config.effectName] = ps;
        }

        // Configure the main module
        var main = ps.main;
        main.startColor = config.startColor;
        main.startSize = config.startSize;
        main.startSpeed = config.startSpeed;
        main.duration = config.duration;
        main.startLifetime = config.lifetime;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Configure the emission module
        var emission = ps.emission;
        emission.rateOverTime = config.emissionRate;

        ps.Stop(); // Ensure it's not playing automatically
        Debug.Log($"✨ Created/Updated Particle Effect: {config.effectName}");
        ps.Play(); // Ensure it's not playing automatically
    }

    // Initialize Lua script and bind Unity proxies and functions
    public void InitializeLuaScript(string code)
    {
        try
        {
            luaScript = new Script();

            // Bind proxies for Lua to access Unity components
            luaScript.Globals["transformProxy"] = UserData.Create(transformProxy);
            luaScript.Globals["gameObjectProxy"] = UserData.Create(gameObjectProxy);
            luaScript.Globals["Vector3"] = (Func<float, float, float, Vector3>)((x, y, z) => new Vector3(x, y, z));
            luaScript.Globals["Vector2"] = (Func<float, float, Vector2>)((x, y) => new Vector2(x, y));
            luaScript.Globals["Quaternion"] = (Func<float, float, float, float, Quaternion>)((x, y, z, w) => new Quaternion(x, y, z, w));
            luaScript.Globals["Color"] = (Func<float, float, float, float, Color>)((r, g, b, a) => new Color(r, g, b, a));

            // Execute the Lua code
            luaScript.DoString(code);

            // Bind trigger events to C# functions
            luaScript.Globals["onTriggerPress"] = (Action)OnTriggerPress;
            luaScript.Globals["onTriggerRelease"] = (Action)OnTriggerRelease;

            // Fetch Lua functions
            startFunction = luaScript.Globals.Get("start");
            updateFunction = luaScript.Globals.Get("update");
            fixedUpdateFunction = luaScript.Globals.Get("fixedUpdate");
            lateUpdateFunction = luaScript.Globals.Get("lateUpdate");
            onTriggerEnterFunction = luaScript.Globals.Get("onTriggerEnter");
            onTriggerExitFunction = luaScript.Globals.Get("onTriggerExit");
            onCollisionEnterFunction = luaScript.Globals.Get("onCollisionEnter");
            onCollisionExitFunction = luaScript.Globals.Get("onCollisionExit");
            onButtonClickFunction = luaScript.Globals.Get("onButtonClick");

            // Call the start function if available
            if (startFunction != null && startFunction.Type == DataType.Function)
            {
                Debug.Log("✅ Lua Start() function found! Calling it...");
                luaScript.Call(startFunction);
            }

            // Bind button click if a UI button is provided
            if (uiButton != null && onButtonClickFunction != null)
            {
                uiButton.onClick.AddListener(() => luaScript.Call(onButtonClickFunction));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Lua Script Error: {ex.Message}");
        }
    }

    // Update is called once per frame; calls Lua's update(deltaTime) function
    void Update()
    {
        // Debug: Press F3 to manually reinitialize Lua script
        if (Input.GetKeyDown(KeyCode.F3))
        {
            InitializeLuaScript(luaScriptText);
        }

        if (updateFunction == null || updateFunction.Type != DataType.Function)
        {
            Debug.LogWarning("⚠️ Lua update function is missing or not properly registered!");
            return;
        }

        luaScript.Call(updateFunction, Time.deltaTime);
    }

    // Collision handling: forward collision events to Lua
    void OnCollisionEnter(Collision collision)
    {
        string otherObjectName = collision.gameObject.name;
        if (onCollisionEnterFunction != null && onCollisionEnterFunction.Type == DataType.Function)
        {
            luaScript.Call(onCollisionEnterFunction, otherObjectName);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        string otherObjectName = collision.gameObject.name;
        if (onCollisionExitFunction != null && onCollisionExitFunction.Type == DataType.Function)
        {
            luaScript.Call(onCollisionExitFunction, otherObjectName);
        }
    }

    // Trigger events: forward trigger events to Lua
    void OnTriggerPress()
    {
        DynValue func = luaScript.Globals.Get("onTriggerPress");
        if (func != null && func.Type == DataType.Function)
        {
            luaScript.Call(func);
        }
    }

    void OnTriggerRelease()
    {
        DynValue func = luaScript.Globals.Get("onTriggerRelease");
        if (func != null && func.Type == DataType.Function)
        {
            luaScript.Call(func);
        }
    }

    // Activate an effect by name: play the corresponding ParticleSystem
    void ActivateEffect(string effectName)
    {
        if (effectSystems.ContainsKey(effectName))
        {
            ParticleSystem ps = effectSystems[effectName];
            ps.Play();
            Debug.Log($"✨ Effect Activated: {effectName}");
        }
        else
        {
            Debug.LogWarning($"No particle effect configuration found for: {effectName}");
        }
    }

    // Deactivate all effects (or a specific one if desired)
    void DeactivateEffect()
    {
        foreach (var ps in effectSystems.Values)
        {
            ps.Stop();
        }
        Debug.Log("✨ All effects deactivated!");
    }
}
