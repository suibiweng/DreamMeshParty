using UnityEngine;
using UnityEngine.Networking;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using LuaProxies;
using UnityEngine.UI;
using RealityEditor;
using TMPro;
using System.Text.RegularExpressions;
using System.Globalization;

[System.Serializable]
public class ParticleEffectConfig
{
    public string effectName;
    public float duration;
    public Color startColor;
    public float startSize;
    public float startSpeed;
    public float emissionRate;
    public float lifetime;
    public int maxParticles;
    public string shape;
}

[Serializable]
public class RawParamUIDef
{
    public string name;       // optional: display or fallback key (legacy)
    public string variable;   // optional: Lua global (legacy)
    public string key;        // supports JSON that uses "key"
    public string type;       // slider/toggle/dropdown/inputfield/button
    public float min;
    public float max;
    public float @default;
    public List<string> options;
    public string label;      // optional pretty label if you have it
    public string effectName; // preferred for param_ui_particle
}

[Serializable]
public class ParticleEffectConfigRaw
{
    public string effectName;
    public string duration;
    public string startColor;   // "Color(r, g, b, a)" or "r,g,b,a"
    public string startSize;
    public string startSpeed;
    public string emissionRate;
    public string lifetime;
    public string maxParticles;
    public string shape;
}

[Serializable]
public class ParamUIDef
{
    public string effectName;
    public string type;
    public string label;
    public string key;
    public float min;
    public float max;
    public List<string> options;
    public float @default;
}

[Serializable]
public class DynamicObjectData
{
    public string object_name;
    public string lua_code;
    public List<ParticleEffectConfigRaw> particle_json; // accept raw and parse
    public List<RawParamUIDef> param_ui_particle;       // raw
    public List<RawParamUIDef> param_ui_lua;            // raw
    public string comment;
    public string created_at;
}

public class LuaMonoBehavior : MonoBehaviour
{
    public TMP_Text object_name_text;
    public bool isRunning = false;

    [Header("Collision Filter")]
    public Collider innerCollider; // Assign in Inspector

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 initialVelocity;

    public Transform SpwanPoint;
    public string ID;
    public string serverURL = "http://yourserver.com/";
    public Text uiText;
    public Button uiButton;
    public Rigidbody rb;
    public string luaScriptText;
    public float checkInterval = 10f;
    public Material defaultParticleMaterial;
    public event Action<bool> OnURLResponse = delegate { };

    public RealityEditorManager manager;
    public bool hasluaScript = false;

    public GameObject controlPanel;

    private Script luaScript;
    private UnityEngine.Coroutine fileCheckCoroutine;
    private bool isDownloading = false;
    public string lastLoadedTimestamp = "";

    private DynValue startFunction, updateFunction, fixedUpdateFunction, lateUpdateFunction;
    private DynValue onTriggerEnterFunction, onTriggerExitFunction;
    private DynValue onCollisionEnterFunction, onCollisionExitFunction;
    private DynValue onButtonClickFunction;

    TransformProxy transformProxy;
    GameObjectProxy gameObjectProxy;
    RigidbodyProxy rigidbodyProxy;
    AudioSourceProxy audioSourceProxy;
    TextProxy textProxy;
    ButtonProxy buttonProxy;
    ParticleSystemProxy particleSystemProxy;
    AnimatorProxy animatorProxy;

    private readonly Dictionary<string, ParticleSystem> effectSystems = new Dictionary<string, ParticleSystem>();
    public GenerateSpotRPC generateSpotRPC;

    public Toggle informationToggle;
    public Toggle[] tabsToggles;
    public GameObject[] Tabs;
    public GameObject InfoTab;
    public TMP_Text CodeInfo, ExplanationsInfo;

    public LuaParamUIBuilder LuaParamUIBuilder;

    private string _lastJsonSnapshot = "";

    // --- Object name robustness ---
    private string _lastObjectNameFromJson = null;
    private UnityEngine.Coroutine _applyNameCo = null;

    // Helpers for robust particle name handling and diagnostics
    private static string NormalizeEffectKey(string name)
    {
        return string.IsNullOrEmpty(name) ? "" : name.Trim().ToLowerInvariant();
    }

    private void LogAvailableEffects(string context = "")
    {
        var names = string.Join(", ", effectSystems.Keys);
        Debug.Log($"[Particles] {context} Available effects (normalized keys): [{names}]");
    }

    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        if (rb != null) initialVelocity = rb.velocity;

        UserData.RegisterType<GameObject>();
        UserData.RegisterType<Vector3>();
        UserData.RegisterType<TransformProxy>();
        UserData.RegisterType<GameObjectProxy>();
        UserData.RegisterType<RigidbodyProxy>();
        UserData.RegisterType<AudioSourceProxy>();
        UserData.RegisterType<TextProxy>();
        UserData.RegisterType<ButtonProxy>();
        UserData.RegisterType<ParticleSystemProxy>();
        UserData.RegisterType<AnimatorProxy>();

        transformProxy = new TransformProxy(transform);
        gameObjectProxy = new GameObjectProxy(gameObject);
        if (rb != null) rigidbodyProxy = new RigidbodyProxy(rb);
        if (GetComponent<AudioSource>() != null) audioSourceProxy = new AudioSourceProxy(GetComponent<AudioSource>());
        if (uiText != null) textProxy = new TextProxy(uiText);
        if (uiButton != null) buttonProxy = new ButtonProxy(uiButton);
        if (GetComponent<ParticleSystem>() != null) particleSystemProxy = new ParticleSystemProxy(GetComponent<ParticleSystem>());
        if (GetComponent<Animator>() != null) animatorProxy = new AnimatorProxy(GetComponent<Animator>());

        luaScript = new Script();
        manager = FindAnyObjectByType<RealityEditorManager>();
        generateSpotRPC = GetComponent<GenerateSpotRPC>();
        fileCheckCoroutine = null;

        // Initialize name stickiness at boot (if it already has a name)
        if (!string.IsNullOrEmpty(gameObject.name))
            ApplyObjectName(gameObject.name, force: false);
    }

    // Debug JSON injection (Editor/Runtime)
    [Header("Debug JSON (Editor)")]
    [TextArea(5, 20)] public string debugJson = "";
    public bool debugSelect = false;

    // Add this method to run a full DynamicCoding JSON string (forces apply, bypassing timestamp)
    public void DebugRunJson(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogWarning("DebugRunJson: Provided JSON string is empty.");
            return;
        }

        try
        {
            Debug.Log("DebugRunJson: Forcing execution of provided JSON...");
            _lastJsonSnapshot = jsonString; // store snapshot for debugging
            lastLoadedTimestamp = string.Empty; // reset gate
            ProcessJsonData(jsonString, force: true);    // reuse pipeline with force
        }
        catch (Exception ex)
        {
            Debug.LogError("DebugRunJson: Failed to process provided JSON. " + ex.Message);
        }
    }

    public string urlToCheck;

    public void StartFetchingCode(string downloadURL, string downloadID)
    {
        if (fileCheckCoroutine != null) return;

        urlToCheck = downloadURL + "/objects/" + downloadID + "/" + downloadID + "_DynamicCoding.json";
        fileCheckCoroutine = StartCoroutine(CheckFileAvailability(urlToCheck));
    }

    public void FetchAgain()
    {
        if (fileCheckCoroutine != null || string.IsNullOrEmpty(urlToCheck)) return;
        fileCheckCoroutine = StartCoroutine(CheckFileAvailability(urlToCheck));
    }

    private IEnumerator CheckFileAvailability(string url)
    {
        yield return new WaitForSeconds(10f);

        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        DynamicObjectData tempData = JsonUtility.FromJson<DynamicObjectData>(www.downloadHandler.text);
                        if (tempData == null)
                        {
                            Debug.LogWarning("JSON parse returned null DynamicObjectData.");
                            OnURLResponse(false);
                        }
                        else if (tempData.created_at != lastLoadedTimestamp)
                        {
                            isDownloading = true;
                            _lastJsonSnapshot = www.downloadHandler.text;
                            ProcessJsonData(_lastJsonSnapshot); // has its own try/catch

                            OnURLResponse(true);
                            break; // stop polling on new data
                        }
                        else
                        {
                            OnURLResponse(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error parsing JSON in poller: " + ex.Message);
                        Debug.LogError("Raw JSON snapshot:\n" + www.downloadHandler.text);
                        LuaErrorReporting(ex.Message);
                        OnURLResponse(false);
                    }
                }
                else
                {
                    Debug.LogWarning("Failed to fetch JSON. Retrying...");
                    OnURLResponse(false);
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }

        fileCheckCoroutine = null;
    }

    // UPDATED: allow 'force' to bypass the created_at gate
    private void ProcessJsonData(string json, bool force = false)
    {
        try
        {
            DynamicObjectData data = JsonUtility.FromJson<DynamicObjectData>(json);
            if (data == null)
            {
                Debug.LogError("ProcessJsonData: Parsed data is null.");
                return;
            }

            if (!force && data.created_at == lastLoadedTimestamp)
            {
                Debug.Log("ProcessJsonData: Timestamp unchanged. Skipping update.");
                return;
            }

            lastLoadedTimestamp = data.created_at ?? string.Empty;
            luaScriptText = Regex.Unescape(data.lua_code ?? "");

            if (CodeInfo != null) CodeInfo.text = data.lua_code ?? "";
            if (ExplanationsInfo != null)
                ExplanationsInfo.text = $" {data.comment}\n Generated at: {data.created_at}";

            InitializeLuaScript(luaScriptText);

            // Build or update particle systems from raw
            var typedEffects = new List<ParticleEffectConfig>();
            if (data.particle_json != null)
            {
                foreach (var raw in data.particle_json)
                {
                    var cfg = ParseParticleConfig(raw);
                    typedEffects.Add(cfg);
                    CreateOrUpdateParticleSystem(cfg);
                    Debug.Log($"[JSON->PS] effectName='{cfg.effectName}', duration={cfg.duration}, startColor={cfg.startColor}, startSize={cfg.startSize}, startSpeed={cfg.startSpeed}, emissionRate={cfg.emissionRate}, lifetime={cfg.lifetime}, maxParticles={cfg.maxParticles}, shape='{cfg.shape}'");
                }
            }

            // After systems are available, rebind Lua's particleSystemProxy to the first effect (legacy compatibility)
            RebindParticleProxyToFirstEffect();

            // Build split UI with both panels visible (JSON-first; comment fallback)
            if (LuaParamUIBuilder != null)
            {
                LuaParamUIBuilder.targetBehavior = this;

                LuaParamUIBuilder.particleSystems.Clear();
                foreach (var kv in effectSystems) LuaParamUIBuilder.particleSystems[kv.Key] = kv.Value;

                LuaParamUIBuilder.SetDescription(data.comment ?? "");

                var luaParams = ConvertLuaParams(data.param_ui_lua);
                var particleParams = ConvertParticleParams(data.param_ui_particle, typedEffects);

                if ((luaParams != null && luaParams.Count > 0) || (particleParams != null && particleParams.Count > 0))
                    LuaParamUIBuilder.BuildFromJSON(luaParams, particleParams);
                else
                    LuaParamUIBuilder.BuildUIFromComment(data.comment ?? "");

                if (LuaParamUIBuilder.showBothPanels)
                    LuaParamUIBuilder.ShowBothPanels();
            }

            if (!string.IsNullOrEmpty(data.object_name))
            {
                ApplyObjectName(data.object_name, force: true);
            }

            LogAvailableEffects("After ProcessJsonData");

            isDownloading = false;
        }
        catch (Exception ex)
        {
            Debug.LogError("ProcessJsonData: Exception while reading JSON: " + ex.Message);
            Debug.LogError("Last JSON snapshot:\n" + json);
            LuaErrorReporting(ex.Message);
        }
    }

    private ParticleEffectConfig ParseParticleConfig(ParticleEffectConfigRaw raw)
    {
        float F(string s, float fallback = 0f)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            return fallback;
        }

        Color ParseColor(string s, Color fallback)
        {
            if (string.IsNullOrEmpty(s)) return fallback;
            try
            {
                string inner = s;
                int lp = s.IndexOf('(');
                int rp = s.IndexOf(')');
                if (lp >= 0 && rp > lp) inner = s.Substring(lp + 1, rp - lp - 1);
                var parts = inner.Split(',');
                float r = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float g = float.Parse(parts[1], CultureInfo.InvariantCulture);
                float b = float.Parse(parts[2], CultureInfo.InvariantCulture);
                float a = parts.Length > 3 ? float.Parse(parts[3], CultureInfo.InvariantCulture) : 1f;
                return new Color(r, g, b, a);
            }
            catch { return fallback; }
        }

        return new ParticleEffectConfig
        {
            effectName   = raw.effectName,
            duration     = F(raw.duration, 1f),
            startColor   = ParseColor(raw.startColor, Color.white),
            startSize    = F(raw.startSize, 1f),
            startSpeed   = F(raw.startSpeed, 5f),
            emissionRate = F(raw.emissionRate, 10f),
            lifetime     = F(raw.lifetime, 2f),
            maxParticles = Mathf.RoundToInt(F(raw.maxParticles, 100f)),
            shape        = raw.shape
        };
    }

    private List<LuaParamUIBuilder.ParamUIDef> ConvertLuaParams(List<RawParamUIDef> raw)
    {
        if (raw == null) return null;

        var list = new List<LuaParamUIBuilder.ParamUIDef>();
        foreach (var r in raw)
        {
            // Resolve Lua global: prefer 'variable', then 'name', then 'key'
            string resolvedKey =
                !string.IsNullOrEmpty(r.variable) ? r.variable :
                !string.IsNullOrEmpty(r.name) ? r.name :
                r.key;

            if (string.IsNullOrEmpty(resolvedKey))
            {
                Debug.LogWarning("[LuaMonoBehavior] Skipping a Lua param: missing 'variable'/'name'/'key'.");
                continue;
            }

            // Label (label > name > key > resolvedKey)
            string uiLabel =
                !string.IsNullOrEmpty(r.label) ? r.label :
                !string.IsNullOrEmpty(r.name) ? r.name :
                !string.IsNullOrEmpty(r.key) ? r.key :
                resolvedKey;

            list.Add(new LuaParamUIBuilder.ParamUIDef
            {
                effectName = null,
                label = uiLabel,
                key = resolvedKey,
                type = NormalizeType(r.type),
                min = r.min,
                max = r.max,
                @default = r.@default,
                options = r.options
            });
        }

        Debug.Log($"[LuaMonoBehavior] ConvertLuaParams -> {list.Count} Lua controls resolved.");
        foreach (var p in list) Debug.Log($"  - {p.key} ({p.type}) label='{p.label}'");
        return list;
    }

    // Helper: split "EffectName: key" into parts
    private static bool TrySplitEffectAndKey(string raw, out string effect, out string key)
    {
        effect = null; key = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        int colon = raw.IndexOf(':');
        if (colon <= 0 || colon >= raw.Length - 1) return false;
        effect = raw.Substring(0, colon).Trim();
        key    = raw.Substring(colon + 1).Trim();
        return !string.IsNullOrEmpty(effect) && !string.IsNullOrEmpty(key);
    }

    // UPDATED: honor new/legacy param_ui_particle shapes.
    // Accepts any of:
    //   { effectName:"HIT_SPARK", key:"duration", ... }
    //   { name:"HIT_SPARK: duration", ... }  or  { label:"HIT_SPARK: duration", ... }
    //   { name:"duration", ... }  // if only one effect exists, target that effect
    private List<LuaParamUIBuilder.ParamUIDef> ConvertParticleParams(List<RawParamUIDef> raw, List<ParticleEffectConfig> effects)
    {
        if (raw == null) return null;

        string defaultEffect = (effects != null && effects.Count == 1) ? effects[0].effectName : null;

        var list = new List<LuaParamUIBuilder.ParamUIDef>();
        foreach (var r in raw)
        {
            // Prefer explicit fields if provided
            string effectName = !string.IsNullOrEmpty(r.effectName) ? r.effectName : null;
            string propKey    = !string.IsNullOrEmpty(r.key)        ? r.key        : null;

            // If missing, try "name" or "label" carrier in the form "Effect: key"
            if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey))
            {
                string carrier = !string.IsNullOrEmpty(r.name) ? r.name : r.label;
                if (!string.IsNullOrEmpty(carrier))
                {
                    if (TrySplitEffectAndKey(carrier, out var effFromCarrier, out var keyFromCarrier))
                    {
                        if (string.IsNullOrEmpty(effectName)) effectName = effFromCarrier;
                        if (string.IsNullOrEmpty(propKey))    propKey    = keyFromCarrier;
                    }
                    else
                    {
                        // Carrier might be just key (e.g., "duration")
                        if (string.IsNullOrEmpty(propKey)) propKey = carrier.Trim();
                    }
                }
            }

            // If still no effect name and only one exists, use that
            if (string.IsNullOrEmpty(effectName)) effectName = defaultEffect;

            if (string.IsNullOrEmpty(propKey))
            {
                Debug.LogWarning("[LuaMonoBehavior] Skipping particle param: missing 'key' (could not infer).");
                continue;
            }

            // Build label
            string displayLabel =
                !string.IsNullOrEmpty(r.label) ? r.label :
                !string.IsNullOrEmpty(r.name)  ? r.name  :
                (string.IsNullOrEmpty(effectName) ? propKey : $"{effectName}: {propKey}");

            list.Add(new LuaParamUIBuilder.ParamUIDef
            {
                effectName = effectName,
                label      = displayLabel,
                key        = propKey,
                type       = NormalizeType(r.type),
                min        = r.min,
                max        = r.max,
                @default   = r.@default,
                options    = r.options
            });
        }
        Debug.Log($"[LuaMonoBehavior] ConvertParticleParams -> {list.Count} particle controls resolved.");
        return list;
    }

    private string NormalizeType(string t)
    {
        if (string.IsNullOrEmpty(t)) return "InputField";
        t = t.Trim().ToLower();
        switch (t)
        {
            case "slider": return "Slider";
            case "toggle": return "Toggle";
            case "dropdown": return "Dropdown";
            case "input":
            case "inputfield":
            case "input field": return "InputField";
            case "button": return "Button";
            default: return "InputField";
        }
    }

    private void CreateOrUpdateParticleSystem(ParticleEffectConfig config)
    {
        if (config == null || string.IsNullOrEmpty(config.effectName)) return;

        string normKey = NormalizeEffectKey(config.effectName);

        ParticleSystem ps;
        if (!effectSystems.TryGetValue(normKey, out ps))
        {
            GameObject psObject = new GameObject(config.effectName + "_Effect");
            psObject.transform.parent = SpwanPoint != null ? SpwanPoint : transform;
            psObject.transform.localPosition = Vector3.zero;
            psObject.transform.localRotation = Quaternion.identity;
            psObject.transform.localScale = Vector3.one;

            ps = psObject.AddComponent<ParticleSystem>();
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = defaultParticleMaterial ?? new Material(Shader.Find("Particles/Standard Unlit"));

            effectSystems[normKey] = ps;
            Debug.Log($"[Particles] Created effect '{config.effectName}' (key='{normKey}').");
        }

        var main = ps.main;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        main.startColor    = config.startColor;
        main.startSize     = config.startSize;
        main.startSpeed    = config.startSpeed;
        main.duration      = config.duration;
        main.startLifetime = config.lifetime;
        main.loop          = true;
        main.scalingMode   = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles  = Mathf.Max(1, config.maxParticles);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = config.emissionRate;

        var shape = ps.shape;
        shape.enabled = true;
        if (!string.IsNullOrEmpty(config.shape))
        {
            var s = config.shape.Trim().ToLowerInvariant();
            if (s == "cone")   shape.shapeType = ParticleSystemShapeType.Cone;
            else if (s == "sphere") shape.shapeType = ParticleSystemShapeType.Sphere;
            else if (s == "box")    shape.shapeType = ParticleSystemShapeType.Box;
        }

        Debug.Log($"[Particles] Updated '{config.effectName}': dur={config.duration}, size={config.startSize}, speed={config.startSpeed}, emit={config.emissionRate}, life={config.lifetime}, max={config.maxParticles}, shape={config.shape}");
    }

    // Rebind Lua's particleSystemProxy to first JSON effect for legacy scripts that call particleSystemProxy:Play()
    private void RebindParticleProxyToFirstEffect()
    {
        if (luaScript == null) return;
        foreach (var kv in effectSystems)
        {
            try
            {
                var ps = kv.Value;
                if (ps == null) continue;
                var newProxy = new ParticleSystemProxy(ps);
                luaScript.Globals["particleSystemProxy"] = UserData.Create(newProxy);
                Debug.Log($"[Particles] particleSystemProxy rebound to JSON effect '{kv.Key}'.");
                break; // first effect is enough for legacy calls
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Particles] Failed to rebind particleSystemProxy: {ex.Message}");
            }
        }
    }

    public void InitializeLuaScript(string code)
    {
        try
        {
            luaScript = new Script();
            hasluaScript = true;
            if (controlPanel != null) controlPanel.SetActive(true);

            luaScript.Globals["transformProxy"] = UserData.Create(transformProxy);
            luaScript.Globals["gameObjectProxy"] = UserData.Create(gameObjectProxy);
            if (rigidbodyProxy != null) luaScript.Globals["rigidbodyProxy"] = UserData.Create(rigidbodyProxy);
            if (audioSourceProxy != null) luaScript.Globals["audioSourceProxy"] = UserData.Create(audioSourceProxy);
            if (textProxy != null) luaScript.Globals["textProxy"] = UserData.Create(textProxy);
            if (buttonProxy != null) luaScript.Globals["buttonProxy"] = UserData.Create(buttonProxy);
            if (particleSystemProxy != null) luaScript.Globals["particleSystemProxy"] = UserData.Create(particleSystemProxy);
            if (animatorProxy != null) luaScript.Globals["animatorProxy"] = UserData.Create(animatorProxy);

            luaScript.Globals["activateEffect"] = (Action<string>)ActivateEffect;
            luaScript.Globals["deactivateEffect"] = (Action)DeactivateEffect;
            luaScript.Globals["Vector3"] = (Func<float, float, float, Vector3>)((x, y, z) => new Vector3(x, y, z));
            luaScript.Globals["Color"] = (Func<float, float, float, float, Color>)((r, g, b, a) => new Color(r, g, b, a));
            luaScript.Globals["Vector2"] = (Func<float, float, Vector2>)((x, y) => new Vector2(x, y));
            luaScript.Globals["Quaternion"] = (Func<float, float, float, float, Quaternion>)((x, y, z, w) => new Quaternion(x, y, z, w));

            luaScript.DoString(code);

            startFunction = luaScript.Globals.Get("start");
            updateFunction = luaScript.Globals.Get("update");
            fixedUpdateFunction = luaScript.Globals.Get("fixedUpdate");
            lateUpdateFunction = luaScript.Globals.Get("lateUpdate");
            onTriggerEnterFunction = luaScript.Globals.Get("onTriggerEnter");
            onTriggerExitFunction = luaScript.Globals.Get("onTriggerExit");
            onCollisionEnterFunction = luaScript.Globals.Get("onCollisionEnter");
            onCollisionExitFunction = luaScript.Globals.Get("onCollisionExit");
            onButtonClickFunction = luaScript.Globals.Get("onButtonClick");

            if (startFunction != null && startFunction.Type == DataType.Function)
                luaScript.Call(startFunction);

            if (uiButton != null && onButtonClickFunction != null)
                uiButton.onClick.AddListener(() => luaScript.Call(onButtonClickFunction));

            var gs = GetComponent<GenerateSpot>();
            if (gs != null && gs.loadingParticles != null)
                gs.loadingParticles.Stop();

            // Re-apply last known name after Lua init to beat late UI hookups
            if (!string.IsNullOrEmpty(_lastObjectNameFromJson))
                ApplyObjectName(_lastObjectNameFromJson, force: false);
        }
        catch (Exception ex)
        {
            Debug.LogError("Lua Script Error: " + ex.Message);
            // Ensure name stays sticky even after errors
            if (!string.IsNullOrEmpty(_lastObjectNameFromJson))
                ApplyObjectName(_lastObjectNameFromJson, force: true);
            LuaErrorReporting(ex.Message);
        }
    }

    public void LuaErrorReporting(string errormsg)
    {
        if (manager != null) manager.sendCommandwithPrompt("Luagoterror", ID, errormsg);
        FetchAgain();
        // Re-assert name on error-driven recalls
        if (!string.IsNullOrEmpty(_lastObjectNameFromJson))
            ApplyObjectName(_lastObjectNameFromJson, force: false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            PlayorStopBtn();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (debugSelect) DebugRunJson(debugJson);
        }

        if (btnLabel != null && !isRunning )
        {
            btnLabel.text = "Play";
        }
        else
        {
            if (btnLabel != null) btnLabel.text = "Stop";
        }

        if (!isRunning) return;

        if (informationToggle != null && InfoTab != null)
            InfoTab.SetActive(informationToggle.isOn);

        if (updateFunction != null && updateFunction.Type == DataType.Function)
            luaScript.Call(updateFunction, Time.deltaTime);
    }



[Tooltip("If true and innerCollider is null, allow collisions from any collider marked as GeneratedColliderMarker or on 'generatedLayerName'.")]
public bool acceptGeneratedHullCollisions = false;
    [Tooltip("Layer name used for generated hull colliders.")]
    public string generatedLayerName = "GeneratedObject";





// Add this field somewhere inside LuaMonoBehavior (top of the class is fine)
private int _generatedLayerCache = int.MinValue; // -1 = not found, other >=0 = cached layer id

// Add this helper inside LuaMonoBehavior
private bool IsGeneratedCollider(Collider col)
{
    if (!col) return false;

    // cache the layer id once
    if (_generatedLayerCache == int.MinValue)
    {
        _generatedLayerCache = string.IsNullOrEmpty(generatedLayerName)
            ? -1
            : LayerMask.NameToLayer(generatedLayerName);
    }

    // marker component OR layer match
    if (col.GetComponent<GeneratedColliderMarker>() != null) return true;
    if (_generatedLayerCache != -1 && col.gameObject.layer == _generatedLayerCache) return true;
    return false;
}

private void OnCollisionEnter(Collision collision)
    {
        if (!isRunning) return;

        print("OnCollisionEnter with: " + collision.gameObject.name);

        if (innerCollider != null)
        {
            if (collision.contactCount == 0 || collision.GetContact(0).thisCollider != innerCollider)
                return;
        }

        var otherGO = collision.gameObject;
        var otherProxy = new GameObjectProxy(otherGO);

        if (onCollisionEnterFunction != null && onCollisionEnterFunction.Type == DataType.Function)
            luaScript.Call(onCollisionEnterFunction, UserData.Create(otherProxy));
    }

    private void OnCollisionExit(Collision collision)
    {
        if (!isRunning) return;
        print("OnCollisionExit with: " + collision.gameObject.name);

        if (innerCollider != null)
        {
            if (collision.contactCount == 0 || collision.GetContact(0).thisCollider != innerCollider)
                return;
        }

        var otherGO = collision.gameObject;
        var otherProxy = new GameObjectProxy(otherGO);

        if (onCollisionExitFunction != null && onCollisionExitFunction.Type == DataType.Function)
            luaScript.Call(onCollisionExitFunction, UserData.Create(otherProxy));
    }

    public void RPCTrigger()
    {
        if (!isRunning) return;
        if (generateSpotRPC != null) generateSpotRPC.CallTriggerRPC();
        var func = luaScript.Globals.Get("trigger");
        if (func != null && func.Type == DataType.Function)
            luaScript.Call(func);
    }

    public void Trigger()
    {
        if (!isRunning) return;
        var func = luaScript.Globals.Get("trigger");
        if (func != null && func.Type == DataType.Function)
            luaScript.Call(func);
    }

    private void ActivateEffect(string effectName)
    {
        if (string.IsNullOrWhiteSpace(effectName))
        {
            Debug.LogWarning("[Particles] activateEffect called with empty name.");
            LogAvailableEffects("Empty request");
            return;
        }

        string norm = NormalizeEffectKey(effectName);

        if (effectSystems.TryGetValue(norm, out var ps))
        {
            ps.Clear();
            ps.Play();
            Debug.Log($"[Particles] Activated '{effectName}' (key='{norm}').");
            return;
        }

        // Fallback: try loose contains match
        foreach (var kv in effectSystems)
        {
            if (kv.Key.Contains(norm))
            {
                kv.Value.Clear();
                kv.Value.Play();
                Debug.Log($"[Particles] Activated (fuzzy) '{effectName}' using key '{kv.Key}'.");
                return;
            }
        }

        Debug.LogWarning($"[Particles] No effect found for '{effectName}'.");
        LogAvailableEffects("Activation failed");
    }

    private void DeactivateEffect()
    {
        foreach (var ps in effectSystems.Values)
            ps.Stop();
    }

    public Script Script => luaScript;
    public TMP_Text btnLabel;

    public void PlayorStopBtn()
    {
        if (isRunning)
        {
            Stop();
            if (btnLabel != null) btnLabel.text = "Play";
        }
        else
        {
            Play();
            if (btnLabel != null) btnLabel.text = "Stop";
        }
    }

    public void Play()
    {
        if (!hasluaScript) return;

        // Before starting, try to push the latest UI values into Lua globals
        ApplyUIParamsToLua();

        // Now call start() so Lua runs with fresh params
        if (startFunction != null && startFunction.Type == DataType.Function)
        {
            luaScript.Call(startFunction);
            var gs = GetComponent<GenerateSpot>();
            if (gs != null && gs.physicToggle != null)
            {
                gs.physicToggle.isOn = true;
            }
        }
        isRunning = true;
    }

    public void Stop()
    {
        isRunning = false;

        var gs = GetComponent<GenerateSpot>();
        if (gs != null && gs.physicToggle != null) gs.physicToggle.isOn = false;

        transform.position = initialPosition;
        transform.rotation = initialRotation;
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.MovePosition(initialPosition);
            rb.MoveRotation(initialRotation);
        }

        DeactivateEffect();
    }

    // Context-menu test helpers
    [ContextMenu("Particles/Play All")]
    public void PlayAllEffects()
    {
        foreach (var ps in effectSystems.Values)
        {
            ps.Clear();
            ps.Play();
        }
        LogAvailableEffects("Play All");
    }

    [ContextMenu("Particles/Stop All")]
    public void StopAllEffects()
    {
        foreach (var ps in effectSystems.Values) ps.Stop();
    }

    // =========================
    // UI -> Lua param bridging
    // =========================

    /// <summary> 
    /// Pushes current Param UI values into Lua globals before starting or on demand.
    /// Tries several strategies so it works even if LuaParamUIBuilder shape changes:
    /// 1) Looks for a method "GetCurrentParams" that returns IEnumerable of { key, value }.
    /// 2) Looks for a method "CollectAllParamValues" that returns Dictionary<string, object>.
    /// 3) Looks for a field/property "luaParamValues" Dictionary<string, object/float>.
    /// If none found, logs a one-time warning.
    /// </summary>
    public void ApplyUIParamsToLua()
    {
        if (!hasluaScript || luaScript == null) return;

        bool appliedAny = false;

        if (LuaParamUIBuilder != null)
        {
            try
            {
                // Strategy 1: GetCurrentParams()
                var m1 = LuaParamUIBuilder.GetType().GetMethod("GetCurrentParams", Type.EmptyTypes);
                if (m1 != null)
                {
                    var enumerable = m1.Invoke(LuaParamUIBuilder, null) as System.Collections.IEnumerable;
                    if (enumerable != null)
                    {
                        foreach (var item in enumerable)
                        {
                            if (!TryExtractKeyValue(item, out var key, out var val)) continue;
                            SetLuaGlobalFromObject(key, val);
                            appliedAny = true;
                        }
                    }
                }
                else
                {
                    // Strategy 2: CollectAllParamValues()
                    var m2 = LuaParamUIBuilder.GetType().GetMethod("CollectAllParamValues", Type.EmptyTypes);
                    if (m2 != null)
                    {
                        var map = m2.Invoke(LuaParamUIBuilder, null) as System.Collections.IDictionary;
                        if (map != null)
                        {
                            foreach (var k in map.Keys)
                            {
                                var key = k as string;
                                var val = map[k];
                                if (string.IsNullOrEmpty(key)) continue;
                                SetLuaGlobalFromObject(key, val);
                                appliedAny = true;
                            }
                        }
                    }
                    else
                    {
                        // Strategy 3: field or prop "luaParamValues"
                        var f = LuaParamUIBuilder.GetType().GetField("luaParamValues");
                        object dictObj = null;
                        if (f != null) dictObj = f.GetValue(LuaParamUIBuilder);
                        if (dictObj == null)
                        {
                            var p = LuaParamUIBuilder.GetType().GetProperty("luaParamValues");
                            if (p != null) dictObj = p.GetValue(LuaParamUIBuilder, null);
                        }

                        var dict = dictObj as System.Collections.IDictionary;
                        if (dict != null)
                        {
                            foreach (var k in dict.Keys)
                            {
                                var key = k as string;
                                var val = dict[k];
                                if (string.IsNullOrEmpty(key)) continue;
                                SetLuaGlobalFromObject(key, val);
                                appliedAny = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LuaMonoBehavior] ApplyUIParamsToLua() reflection failed: {ex.Message}");
            }
        }

        if (!appliedAny)
        {
            Debug.Log("[LuaMonoBehavior] ApplyUIParamsToLua: no UI params found to apply (is LuaParamUIBuilder exposing values?).");
        }
    }

    private bool TryExtractKeyValue(object item, out string key, out object value)
    {
        key = null;
        value = null;
        if (item == null) return false;

        var t = item.GetType();

        // Try KeyValuePair<string, object>
        if (t.IsGenericType && t.Name.StartsWith("KeyValuePair"))
        {
            var kProp = t.GetProperty("Key");
            var vProp = t.GetProperty("Value");
            if (kProp != null && vProp != null)
            {
                key = kProp.GetValue(item) as string;
                value = vProp.GetValue(item);
                return !string.IsNullOrEmpty(key);
            }
        }

        // Try fields named key/value
        var keyField = t.GetField("key");
        var valField = t.GetField("value");
        if (keyField != null && valField != null)
        {
            key = keyField.GetValue(item) as string;
            value = valField.GetValue(item);
            return !string.IsNullOrEmpty(key);
        }

        // Try properties named Key/Value or key/value
        var keyProp = t.GetProperty("Key") ?? t.GetProperty("key");
        var valProp = t.GetProperty("Value") ?? t.GetProperty("value");
        if (keyProp != null && valProp != null)
        {
            key = keyProp.GetValue(item) as string;
            value = valProp.GetValue(item);
            return !string.IsNullOrEmpty(key);
        }

        return false;
    }

    private void SetLuaGlobalFromObject(string key, object val)
    {
        if (string.IsNullOrEmpty(key)) return;

        try
        {
            if (val == null)
            {
                luaScript.Globals[key] = DynValue.Nil;
                return;
            }

            switch (val)
            {
                case bool b:
                    luaScript.Globals[key] = b;
                    break;
                case int i:
                    luaScript.Globals[key] = (double)i;
                    break;
                case long l:
                    luaScript.Globals[key] = (double)l;
                    break;
                case float f:
                    luaScript.Globals[key] = (double)f;
                    break;
                case double d:
                    luaScript.Globals[key] = d;
                    break;
                case string s:
                    if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var dnum))
                        luaScript.Globals[key] = dnum;
                    else if (bool.TryParse(s, out var bval))
                        luaScript.Globals[key] = bval;
                    else
                        luaScript.Globals[key] = s;
                    break;
                default:
                    var str = val.ToString();
                    if (double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var dnum2))
                        luaScript.Globals[key] = dnum2;
                    else if (bool.TryParse(str, out var bval2))
                        luaScript.Globals[key] = bval2;
                    else
                        luaScript.Globals[key] = str;
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LuaMonoBehavior] Failed to set Lua global '{key}' from value '{val}': {ex.Message}");
        }
    }

    // --- Name stickiness helpers ---
    private void ApplyObjectName(string rawName, bool force = false)
    {
        string n = (rawName ?? "").Trim();
        if (string.IsNullOrEmpty(n)) return;

        if (!force && gameObject.name == n && (object_name_text == null || object_name_text.text == n))
            return;

        gameObject.name = n;
        if (object_name_text != null) object_name_text.text = n;
        _lastObjectNameFromJson = n;

        if (_applyNameCo != null) StopCoroutine(_applyNameCo);
        _applyNameCo = StartCoroutine(ApplyNameNextFrame(n));

        Debug.Log($"[LuaMonoBehavior] Applied object name: {n} (force={force})");
    }

    private IEnumerator ApplyNameNextFrame(string nameToApply)
    {
        yield return null;
        gameObject.name = nameToApply;
        if (object_name_text != null) object_name_text.text = nameToApply;
        yield return null;
        gameObject.name = nameToApply;
        if (object_name_text != null) object_name_text.text = nameToApply;
        Debug.Log($"[LuaMonoBehavior] Re-applied object name on next frames: {nameToApply}");
    }
}
