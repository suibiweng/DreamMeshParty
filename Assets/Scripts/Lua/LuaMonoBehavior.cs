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

    public string comment;
}

public class LuaMonoBehavior : MonoBehaviour
{
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

    private Script luaScript;
    private UnityEngine.Coroutine fileCheckCoroutine;
    private bool isDownloading = false;

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

    private Dictionary<string, ParticleSystem> effectSystems = new();

    void Start()
    {
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
        //   serverURL = manager.ServerURL;
    }

    string urlToCheck = "";

    public void StartFetchingCode(string downloadURL, string downloadID)
    {
        if (fileCheckCoroutine == null)
        {
            urlToCheck = downloadURL + "/objects/" + downloadID + "/" + downloadID + "_DynamicCoding.json";
            fileCheckCoroutine = StartCoroutine(CheckFileAvailability(urlToCheck));
        }
    }

    public void Fetchingforupdate()
    {
        if (fileCheckCoroutine == null)
        {
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
                yield return new WaitForSeconds(checkInterval);
        }

        fileCheckCoroutine = null;
    }

    void ProcessJsonData(string json)
    {
        DynamicObjectData data = JsonUtility.FromJson<DynamicObjectData>(json);
        luaScriptText = data.lua_code;

        if (CodeInfo != null) CodeInfo.text = data.lua_code;
        if (ExplanationsInfo != null) ExplanationsInfo.text = data.comment;

        InitializeLuaScript(data.lua_code);

        foreach (var effect in data.particle_json)
        {
            CreateOrUpdateParticleSystem(effect);
        }
    }

    void CreateOrUpdateParticleSystem(ParticleEffectConfig config)
    {
        ParticleSystem ps;
        if (effectSystems.ContainsKey(config.effectName))
        {
            ps = effectSystems[config.effectName];
        }
        else
        {
            GameObject psObject = new GameObject(config.effectName + "_Effect");
            psObject.transform.parent = SpwanPoint;
            psObject.transform.localPosition = Vector3.zero;
            psObject.transform.rotation = Quaternion.Euler(-90, 0, 0);
            psObject.transform.localScale = Vector3.one;

            ps = psObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = defaultParticleMaterial ?? new Material(Shader.Find("Particles/Standard Unlit"));
            effectSystems[config.effectName] = ps;
        }

        var main = ps.main;
        main.startColor = config.startColor;
        main.startSize = config.startSize;
        main.startSpeed = config.startSpeed;
        main.duration = config.duration;
        main.startLifetime = config.lifetime;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = ps.emission;
        emission.rateOverTime = config.emissionRate;

        ps.Stop();
        ps.Play();
    }

    public void InitializeLuaScript(string code)
    {
        try
        {
            luaScript = new Script();

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
            {
                luaScript.Call(startFunction);
            }

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

    void Update()
    {


        if (informationToggle != null)
        { InfoTab.SetActive(informationToggle.isOn); 
            

        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            luaScriptText = "function start()\n    print(\"Baseball initialized\")\nend\n\nfunction update(deltaTime)\n    -- optional animation\nend\n\nfunction onCollisionEnter(other)\n    print(\"Collided with \" .. other)\n    activateEffect(\"collisionSpark\")\n    if rigidbodyProxy then\n        rigidbodyProxy:SetUseGravity(true)\n        rigidbodyProxy:AddForce(Vector3(0, 300, 500))\n    end\nend\n\nfunction trigger()\n    print(\"Trigger called — no action needed for baseball.\")\nend";
            InitializeLuaScript(luaScriptText);
            Debug.Log("Lua script reloaded.");
        }

        if (updateFunction != null && updateFunction.Type == DataType.Function)
        {
            luaScript.Call(updateFunction, Time.deltaTime);
        }
    }

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

    public void Trigger()
    {
        var func = luaScript.Globals.Get("trigger");
        if (func != null && func.Type == DataType.Function)
        {
            luaScript.Call(func);
        }
    }

    void ActivateEffect(string effectName)
    {
        if (effectSystems.TryGetValue(effectName, out var ps))
        {
            ps.Play();
        }
        else
        {
            Debug.LogWarning($"No particle effect configuration found for: {effectName}");
        }
    }

    void DeactivateEffect()
    {
        foreach (var ps in effectSystems.Values)
        {
            ps.Stop();
        }
    }

    public Toggle informationToggle;
    public Toggle[] tabsToggles;
    public GameObject[] Tabs;

    public GameObject InfoTab;

    public TMP_Text CodeInfo, ExplanationsInfo;


    public void InformationTabmanager()
    {


        for (int i = 0; i < Tabs.Length; i++)
        {
            Tabs[i].SetActive(tabsToggles[i].isOn);

        }





    }
    



}
