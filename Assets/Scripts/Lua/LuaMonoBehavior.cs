using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;
using System;
using LuaProxies; // ✅ Import the namespace

public class LuaMonoBehaviour : MonoBehaviour
{
    public string ID;
    private Script luaScript;
    private DynValue startFunction;
    private DynValue updateFunction;
    private DynValue fixedUpdateFunction;
    private DynValue lateUpdateFunction;
    private DynValue onTriggerEnterFunction;
    private DynValue onTriggerExitFunction;
    private DynValue onCollisionEnterFunction;
    private DynValue onCollisionExitFunction;
    private DynValue onButtonClickFunction;

    public Text uiText;
    public Button uiButton;
    public Rigidbody rb;
    public string luaScriptText;

    private UnityEngine.Coroutine fileCheckCoroutine;
    private bool isDownloading = false;
    public float checkInterval = 10f;
    public event Action<bool> OnURLResponse = delegate { };

    TransformProxy transformProxy;
    GameObjectProxy gameObjectProxy;

    void Start()
    {
        // Register Lua types
        UserData.RegisterType<GameObject>();
        UserData.RegisterType<Vector3>();
        UserData.RegisterType<TransformProxy>(); // ✅ Now from LuaProxies namespace
        UserData.RegisterType<GameObjectProxy>(); // ✅ Now from LuaProxies namespace

        // Initialize proxies
        transformProxy = new TransformProxy(transform);
        gameObjectProxy = new GameObjectProxy(gameObject);

        // Default Lua script
        luaScriptText = @"
            function start()
                print('Lua Start() called!')
            end

            function update(deltaTime)
                print('Lua Update() called! DeltaTime:', deltaTime)
            end
        ";

        luaScript = new Script(); // ✅ Initialize Lua script instance
    }

    public void InitializeLuaScript(string code)
    {
        try
        {
            luaScript = new Script(); // ✅ Ensure Lua script is fresh before executing code

            luaScript.Globals["transformProxy"] = UserData.Create(transformProxy);
            luaScript.Globals["gameObjectProxy"] = UserData.Create(gameObjectProxy);
           // luaScript.Globals["Vector3"] = (Func<float, float, float, Vector3>)((x, y, z) => new Vector3(x, y, z));
            luaScript.Globals["Vector3"] = (Func<float, float, float, Vector3>)((x, y, z) => new Vector3(x, y, z));
            luaScript.Globals["Vector2"] = (Func<float, float, Vector2>)((x, y) => new Vector2(x, y));
            luaScript.Globals["Quaternion"] = (Func<float, float, float, float, Quaternion>)((x, y, z, w) => new Quaternion(x, y, z, w));
            luaScript.Globals["Color"] = (Func<float, float, float, float, Color>)((r, g, b, a) => new Color(r, g, b, a));

            luaScript.DoString(code); // ✅ Execute Lua code

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

            // Start function check
            if (startFunction != null && startFunction.Type == DataType.Function)
            {
                Debug.Log("✅ Lua Start() function found! Calling it...");
                luaScript.Call(startFunction);
            }

            // Button click binding
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
        // Debug: Press F1 to fetch a new script
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartFetchingCode("http://127.0.0.1:8000/", "20250217135917bfb490c1");
        }

        // Debug: Press F3 to manually reinitialize Lua script
        if (Input.GetKeyDown(KeyCode.F3))
        {
            InitializeLuaScript(luaScriptText);
        }

        // Ensure `updateFunction` is valid before calling it
        if (updateFunction == null || updateFunction.Type != DataType.Function)
        {
            Debug.LogWarning("⚠️ Lua update function is missing or not properly registered!");
            return; // Prevent further execution
        }

        luaScript.Call(updateFunction, Time.deltaTime);
    }

    public void StartFetchingCode(string downloadURL, string downloadID)
    {
        if (fileCheckCoroutine == null)
        {
            string urlToCheck = downloadURL + downloadID + ".lua";
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
                    Debug.Log("✅ Lua file is available! Downloading...");
                    isDownloading = true;
                    luaScriptText = www.downloadHandler.text;
                    Debug.Log("📜 Lua Script Content:\n" + luaScriptText);

                    OnURLResponse(true);
                    InitializeLuaScript(luaScriptText);
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
}
