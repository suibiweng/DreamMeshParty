using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using System.Linq;
using MoonSharp.Interpreter;

// NOTE: LuaParamType enum is defined in LuaParamFusionSync.cs.
// Make sure that script is in the project and compiled.

public class LuaParamUIBuilder : MonoBehaviour
{
    [Header("Layout (Left|Right)")]
    public TMP_Text descriptionText;      // Left side description text (TMP)
    public Transform luaPanel;            // Right side: Lua controls
    public Transform particlePanel;       // Right side: Particle controls

    [Header("Tabs (Optional visuals)")]
    public GameObject luaTabHeader;
    public GameObject particleTabHeader;

    [Header("Prefabs")]
    public GameObject sliderPrefab;
    public GameObject togglePrefab;
    public GameObject dropdownPrefab;
    public GameObject inputFieldPrefab;
    public GameObject buttonPrefab;
    public GameObject textPrefab;

    [Header("Highlight Colors")]
    public Color luaChangedColor = new Color(0.18f, 0.6f, 1f, 1f);
    public Color particleChangedColor = new Color(1f, 0.5f, 0.15f, 1f);

    [Header("Behavior")]
    public bool showBothPanels = true;                  // Show Lua & Particle panels simultaneously
    public bool requireDropdownEvenSingleOption = true; // Force dropdown UI even if only 1 option
    public string singleOptionFallback = "(only choice)";

    [Header("Context")]
    public LuaMonoBehavior targetBehavior;
    public Dictionary<string, ParticleSystem> particleSystems = new Dictionary<string, ParticleSystem>();

    [Header("Networking (Fusion)")]
    public LuaParamFusionSync netSync; // drag the object that has LuaParamFusionSync (with NetworkObject)

    [Header("State export (for LuaMonoBehavior.ApplyUIParamsToLua)")]
    public readonly Dictionary<string, object> luaParamValues = new Dictionary<string, object>(StringComparer.Ordinal);

    [Serializable]
    public class ParamUIDef
    {
        public string effectName;  // optional: particle system to target
        public string label;       // user-facing label
        public string type;        // Slider, Toggle, Dropdown, InputField, Button
        public string key;         // Lua global name or particle property (e.g., startSpeed)
        public float min;
        public float max;
        public float @default;
        public List<string> options;
    }

    // Internal state
    private string _baseDescription = "";
    private readonly HashSet<string> _changedLuaTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);       // keys + labels
    private readonly HashSet<string> _changedParticleTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);  // keys + labels
    private readonly HashSet<string> _changedParticleEffects = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // effect names (Particle color)
    private bool _showLua = true; // for optional tabs

    // Control index for quick updates from network
    private class ControlRefs
    {
        public GameObject root;
        public TMP_Text label;
        public Slider slider;
        public Toggle toggle;
        public TMP_Dropdown dropdown;
        public TMP_InputField inputTMP;
        public InputField inputUGUI;
        public string displayLabel;
    }
    private readonly Dictionary<string, ControlRefs> _controlIndex = new Dictionary<string, ControlRefs>(StringComparer.Ordinal);

    // -------- Tabs (optional visuals) --------
    public void SelectLuaTab()
    {
        _showLua = true;
        if (!showBothPanels)
        {
            if (luaPanel) luaPanel.gameObject.SetActive(true);
            if (particlePanel) particlePanel.gameObject.SetActive(false);
        }
        if (luaTabHeader) luaTabHeader.SetActive(true);
        if (particleTabHeader) particleTabHeader.SetActive(false);
        UpdateDescriptionHighlight();
    }

    public void SelectParticleTab()
    {
        _showLua = false;
        if (!showBothPanels)
        {
            if (luaPanel) luaPanel.gameObject.SetActive(false);
            if (particlePanel) particlePanel.gameObject.SetActive(true);
        }
        if (luaTabHeader) luaTabHeader.SetActive(false);
        if (particleTabHeader) particleTabHeader.SetActive(true);
        UpdateDescriptionHighlight();
    }

public void ShowBothPanels()
{
    if (luaPanel) luaPanel.gameObject.SetActive(true);
    if (particlePanel) particlePanel.gameObject.SetActive(true);
    if (luaTabHeader) luaTabHeader.SetActive(false);
    if (particleTabHeader) particleTabHeader.SetActive(false);
}


    public void SetDescription(string text)
    {
        _baseDescription = text ?? "";
        UpdateDescriptionHighlight();
    }

    // -------- Build from JSON (preferred) --------
    public void BuildFromJSON(List<ParamUIDef> luaParams, List<ParamUIDef> particleParams)
    {
        // Reset UI, index and state export map
        ClearPanel(luaPanel);
        ClearPanel(particlePanel);
        luaParamValues.Clear();
        _controlIndex.Clear();

        // Build Lua controls
        if (luaParams != null)
        {
            foreach (var p in luaParams)
            {
                BuildUIElement(luaPanel, p, isLua: true);
                // Auto-color on load
                MarkLuaToken(p.key);
                MarkLuaToken(p.label);
            }
        }

        // Build Particle controls
        if (particleParams != null)
        {
            foreach (var p in particleParams)
            {
                BuildUIElement(particlePanel, p, isLua: false);
                // Auto-color on load
                MarkParticleToken(p.key);
                MarkParticleToken(p.label);
                if (!string.IsNullOrEmpty(p.effectName))
                    _changedParticleEffects.Add(p.effectName);
            }
        }

        UpdateDescriptionHighlight();
        if (showBothPanels) ShowBothPanels(); else SelectLuaTab();
    }

    // -------- Build from prose (fallback) --------
    public void BuildUIFromComment(string comment)
    {
        ClearPanel(luaPanel);
        ClearPanel(particlePanel);
        luaParamValues.Clear();
        _controlIndex.Clear();

        if (string.IsNullOrWhiteSpace(comment))
        {
            if (showBothPanels) ShowBothPanels(); else SelectLuaTab();
            return;
        }

        // Slider with key BEFORE parens: "speed (slider: 0-10)"
        var sliderRx_before = new Regex(
            @"([\w\-]+)\s*\(slider(?:\s*:\s*(-?\d+(?:\.\d+)?)\s*-\s*(-?\d+(?:\.\d+)?))?\)",
            RegexOptions.IgnoreCase);

        // Slider with key INSIDE parens: "(slider: speed[, 0-10])"
        var sliderRx_inside = new Regex(
            @"\(slider\s*:\s*([\w\-]+)(?:\s*,\s*(-?\d+(?:\.\d+)?)\s*-\s*(-?\d+(?:\.\d+)?))?\)",
            RegexOptions.IgnoreCase);

        // Toggle key BEFORE parens: "isActive (toggle)"
        var toggleRx_before = new Regex(
            @"([\w\-]+)\s*\(toggle\)",
            RegexOptions.IgnoreCase);

        // Dropdown with key BEFORE parens: "target (dropdown: [A,B])"
        var dropdownRx_before = new Regex(
            @"([\w\-]+)\s*\((?:dropdown|dropbox)\s*:\s*\[([^\]]+)\]\)",
            RegexOptions.IgnoreCase);

        // Dropdown with NO key: "(dropdown: [A,B])"
        var dropdownRx_nokey = new Regex(
            @"\((?:dropdown|dropbox)\s*:\s*\[([^\]]+)\]\)",
            RegexOptions.IgnoreCase);

        // Input box: "name (input box)"
        var inputRx = new Regex(
            @"([\w\-]+)\s*\(input box\)",
            RegexOptions.IgnoreCase);

        int autoDropdownIndex = 0;
        int index = 0;

        while (index < comment.Length)
        {
            Match[] candidates = {
                sliderRx_before.Match(comment, index),
                sliderRx_inside.Match(comment, index),
                toggleRx_before.Match(comment, index),
                dropdownRx_before.Match(comment, index),
                dropdownRx_nokey.Match(comment, index),
                inputRx.Match(comment, index)
            };

            Match next = null;
            foreach (var m in candidates)
                if (m.Success && (next == null || m.Index < next.Index)) next = m;

            if (next == null || !next.Success)
            {
                AddFreeText(luaPanel, comment.Substring(index).Trim());
                break;
            }

            if (next.Index > index)
            {
                string before = comment.Substring(index, next.Index - index).Trim();
                if (!string.IsNullOrEmpty(before)) AddFreeText(luaPanel, before);
            }

            if (sliderRx_before.Match(next.Value).Success)
            {
                string key = next.Groups[1].Value;
                float min = next.Groups[2].Success ? float.Parse(next.Groups[2].Value) : 0f;
                float max = next.Groups[3].Success ? float.Parse(next.Groups[3].Value) : 10f;

                var def = new ParamUIDef { key = key, label = key, type = "Slider", min = min, max = max, @default = (min + max) / 2f };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }
            else if (sliderRx_inside.Match(next.Value).Success)
            {
                string key = next.Groups[1].Value;
                float min = next.Groups[2].Success ? float.Parse(next.Groups[2].Value) : 0f;
                float max = next.Groups[3].Success ? float.Parse(next.Groups[3].Value) : 10f;

                var def = new ParamUIDef { key = key, label = key, type = "Slider", min = min, max = max, @default = (min + max) / 2f };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }
            else if (toggleRx_before.Match(next.Value).Success)
            {
                string key = next.Groups[1].Value;
                var def = new ParamUIDef { key = key, label = key, type = "Toggle" };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }
            else if (dropdownRx_before.Match(next.Value).Success)
            {
                string key = next.Groups[1].Value;
                var options = new List<string>(next.Groups[2].Value.Split(',').Select(o => o.Trim()));
                if (options.Count == 0) options.Add(singleOptionFallback);
                var def = new ParamUIDef { key = key, label = key, type = "Dropdown", options = options };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }
            else if (dropdownRx_nokey.Match(next.Value).Success)
            {
                string key = $"dropdown_{++autoDropdownIndex}";
                var options = new List<string>(next.Groups[1].Value.Split(',').Select(o => o.Trim()));
                if (options.Count == 0) options.Add(singleOptionFallback);
                var def = new ParamUIDef { key = key, label = key, type = "Dropdown", options = options };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }
            else if (inputRx.Match(next.Value).Success)
            {
                string key = next.Groups[1].Value;
                var def = new ParamUIDef { key = key, label = key, type = "InputField" };
                BuildUIElement(luaPanel, def, true);
                MarkLuaToken(def.key); MarkLuaToken(def.label);
            }

            index = next.Index + next.Length;
        }

        UpdateDescriptionHighlight();
        if (showBothPanels) ShowBothPanels(); else SelectLuaTab();
    }

    // --- NEW: apply booleans to particle modules via dot-keys ---
private void ApplyParticleBool(string effectName, string propKey, bool value)
{
    if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey)) return;
    var norm = NormalizeEffectKey(effectName);
    if (!particleSystems.TryGetValue(norm, out var ps) || ps == null) return;

    var main   = ps.main;
    var noise  = ps.noise;
    var trails = ps.trails;

    switch (propKey)
    {
        case "main.prewarm":
            main.prewarm = value;
            break;
        case "main.playOnAwake":
            main.playOnAwake = value;
            break;
        case "noise.enabled":
            noise.enabled = value;
            break;
        case "trails.enabled":
            trails.enabled = value;
            break;
        case "trails.dieWithParticles":
            trails.dieWithParticles = value;
            break;
        default:
            // no-op for unknown bool keys
            break;
    }
}

// --- NEW: apply floats to V2 scalar or "constant" curve dot-keys ---
private void ApplyParticleFloatV2(string effectName, string propKey, float value)
{
    if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey)) return;
    var norm = NormalizeEffectKey(effectName);
    if (!particleSystems.TryGetValue(norm, out var ps) || ps == null) return;

    var main   = ps.main;
    var emission = ps.emission;
    var noise  = ps.noise;
    var trails = ps.trails;
    var rend   = ps.GetComponent<ParticleSystemRenderer>();
    var vol    = ps.velocityOverLifetime;
    var sol    = ps.sizeOverLifetime;
    var rol    = ps.rotationOverLifetime;

    // --- scalar/simple V2 paths ---
    switch (propKey)
    {
        case "main.gravityModifier":
            main.gravityModifier = value;
            return;

        case "emission.rateOverTime":
            emission.enabled = true;
            emission.rateOverTime = value;
            return;

        case "emission.rateOverDistance":
            emission.enabled = true;
            emission.rateOverDistance = value;
            return;

        case "noise.strength":
            noise.enabled = true;
            noise.strength = value;
            return;

        case "noise.frequency":
            noise.enabled = true;
            noise.frequency = value;
            return;

        case "noise.scrollSpeed":
            noise.enabled = true;
            noise.scrollSpeed = value;
            return;

        case "noise.octaveCount":
            noise.enabled = true;
            noise.octaveCount = Mathf.RoundToInt(Mathf.Clamp(value, 1, 3));
            return;

        case "trails.lifetime":
            trails.enabled = true;
            trails.lifetime = value;
            return;

        case "trails.ratio":
            trails.enabled = true;
            trails.ratio = Mathf.Clamp01(value);
            return;

        case "renderer.sortingFudge":
            if (rend != null) rend.sortingFudge = value;
            return;
    }

    // --- "constant" curve knobs (UI simplifies MinMaxCurve) ---
    switch (propKey)
    {
        case "sizeOverLifetime.size.constant":
            sol.enabled = true;
            sol.separateAxes = false;
            sol.size = new ParticleSystem.MinMaxCurve(value);
            return;

        case "rotationOverLifetime.z.constant":
            rol.enabled = true;
            rol.separateAxes = false;
            // NOTE: Unity expects radians; if your UI is in degrees, multiply by Mathf.Deg2Rad
            rol.z = new ParticleSystem.MinMaxCurve(value);
            return;

        case "velocityOverLifetime.x.constant":
            vol.enabled = true;
            vol.x = new ParticleSystem.MinMaxCurve(value);
            return;

        case "velocityOverLifetime.y.constant":
            vol.enabled = true;
            vol.y = new ParticleSystem.MinMaxCurve(value);
            return;

        case "velocityOverLifetime.z.constant":
            vol.enabled = true;
            vol.z = new ParticleSystem.MinMaxCurve(value);
            return;

        case "velocityOverLifetime.speedModifier.constant":
            vol.enabled = true;
            vol.speedModifier = new ParticleSystem.MinMaxCurve(value);
            return;
    }

    // --- bursts[n].count (we only support first burst index for UI) ---
    if (propKey == "emission.bursts[0].count")
    {
        emission.enabled = true;
        int burstsCount = emission.burstCount;
        if (burstsCount == 0)
        {
            var b = new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(value));
            emission.SetBurst(0, b);
        }
        else
        {
            var b = emission.GetBurst(0);
            // convert to MinMaxCurve with constant
            b.count = new ParticleSystem.MinMaxCurve(value);
            emission.SetBurst(0, b);
        }
        return;
    }
}

// --- NEW: extend string handling beyond shape (e.g., renderer.renderMode) ---
private void ApplyParticleStringV2(string effectName, string propKey, string s)
{
    if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey)) return;
    var norm = NormalizeEffectKey(effectName);
    if (!particleSystems.TryGetValue(norm, out var ps) || ps == null) return;

    if (propKey == "renderer.renderMode")
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        if (!r) return;
        var t = (s ?? "").Trim().ToLowerInvariant();
        switch (t)
        {
            case "billboard":             r.renderMode = ParticleSystemRenderMode.Billboard; break;
            case "stretchedbillboard":
            case "stretched":             r.renderMode = ParticleSystemRenderMode.Stretch;   break;
            case "horizontalbillboard":
            case "horizontal":            r.renderMode = ParticleSystemRenderMode.HorizontalBillboard; break;
            case "verticalbillboard":
            case "vertical":              r.renderMode = ParticleSystemRenderMode.VerticalBillboard;   break;
            case "mesh":                  r.renderMode = ParticleSystemRenderMode.Mesh;       break;
        }
        return;
    }

}


    // -------- Helpers --------
    private void ClearPanel(Transform panel)
    {
        if (!panel) return;
        for (int i = panel.childCount - 1; i >= 0; i--)
            Destroy(panel.GetChild(i).gameObject);
    }

    private void AddFreeText(Transform panel, string content)
    {
        if (!panel || string.IsNullOrWhiteSpace(content)) return;
        GameObject txt = Instantiate(textPrefab, panel);
        var text = txt.GetComponent<TMP_Text>() ?? txt.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = content;
    }

    private string FormatSliderLabel(string baseLabel, float value) => $"{baseLabel}: {value:0.##}";

    // ============== PARTICLE HELPERS (NEW) ==============
    private static string NormalizeEffectKey(string name)
    {
        return string.IsNullOrEmpty(name) ? "" : name.Trim().ToLowerInvariant();
    }

    private void ApplyParticleFloat(string effectName, string propKey, float value)
    {
        if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey)) return;
        var norm = NormalizeEffectKey(effectName);
        if (!particleSystems.TryGetValue(norm, out var ps) || ps == null) return;

        var main = ps.main;
        var emission = ps.emission;

        switch (propKey)
        {
            case "duration":
                main.duration = Mathf.Max(0.01f, value);
                break;
            case "startSpeed":
                main.startSpeed = value;
                break;
            case "startSize":
                main.startSize = value;
                break;
            case "lifetime":
                main.startLifetime = Mathf.Max(0.01f, value);
                break;
            case "emissionRate":
                emission.enabled = true;
                emission.rateOverTime = value;
                break;
            case "maxParticles":
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(value));
                break;
                // startColor would require a color UI, not implemented here
            default:
            // Fallback: handle V2 dot-paths and constant-curves
                ApplyParticleFloatV2(effectName, propKey, value);
            break;
        }
    }

    private void ApplyParticleString(string effectName, string propKey, string s)
    {
        if (string.IsNullOrEmpty(effectName) || string.IsNullOrEmpty(propKey)) return;
        var norm = NormalizeEffectKey(effectName);
        if (!particleSystems.TryGetValue(norm, out var ps) || ps == null) return;

        var shape = ps.shape;
        if (propKey == "shape")
        {
            shape.enabled = true;
            var t = (s ?? "").Trim().ToLowerInvariant();
            if (t == "cone")        shape.shapeType = ParticleSystemShapeType.Cone;
            else if (t == "sphere") shape.shapeType = ParticleSystemShapeType.Sphere;
            else if (t == "box")    shape.shapeType = ParticleSystemShapeType.Box;
        }else
    {
        // Fallback: handle V2 string props like renderer.renderMode
        ApplyParticleStringV2(effectName, propKey, s);
    }
    }

    private static string ParticleIndexKey(string effectName, string key)
    {
        return string.IsNullOrEmpty(effectName) ? $"__particle__::{key}" : $"__particle__::{effectName}::{key}";
    }
    // ====================================================

    private void BuildUIElement(Transform panel, ParamUIDef param, bool isLua)
    {
        if (!panel || param == null || string.IsNullOrEmpty(param.type)) return;

        GameObject element = null;
        string t = param.type.ToLower();

        switch (t)
        {
            case "slider":
            {
                element = Instantiate(sliderPrefab, panel);
                var slider = element.GetComponentInChildren<Slider>();
                var label  = element.GetComponent<TMP_Text>() ?? element.GetComponentInChildren<TMP_Text>();
                if (slider != null)
                {
                    slider.minValue = param.min;
                    slider.maxValue = param.max;
                    slider.value    = param.@default;
                    if (label != null) label.text = FormatSliderLabel(string.IsNullOrEmpty(param.label) ? param.key : param.label, slider.value);

                    // Use composite index for particle controls to avoid clashes
                    var indexKey = isLua ? param.key : ParticleIndexKey(param.effectName, param.key);
                    IndexControl(indexKey, element, label, slider: slider);

                    // Seed
                    if (isLua)
                    {
                        SeedValue(param, slider.value, true);
                    }
                    else
                    {
                        ApplyParticleFloat(param.effectName, param.key, slider.value);
                    }

                    slider.onValueChanged.AddListener(val =>
                    {
                        if (label != null) label.text = FormatSliderLabel(string.IsNullOrEmpty(param.label) ? param.key : param.label, val);
                        OnSliderChanged(param, val, isLua);
                    });
                }
                break;
            }

            case "toggle":
            {
                element = Instantiate(togglePrefab, panel);
                var toggle = element.GetComponentInChildren<Toggle>();
                var label  = element.GetComponent<TMP_Text>() ?? element.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = string.IsNullOrEmpty(param.label) ? param.key : param.label;

                var indexKey = isLua ? param.key : ParticleIndexKey(param.effectName, param.key);
                IndexControl(indexKey, element, label, toggle: toggle);

                if (toggle != null)
                {
                    toggle.isOn = false;
                    if (isLua) { SeedValue(param, false, true); OnToggleChanged(param, false, true); }
                    toggle.onValueChanged.AddListener(val => OnToggleChanged(param, val, isLua));
                }
                return;
            }

            case "dropdown":
            {
                element = Instantiate(dropdownPrefab, panel);
                var dropdown = element.GetComponentInChildren<TMP_Dropdown>();
                var label    = element.GetComponent<TMP_Text>() ?? element.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = string.IsNullOrEmpty(param.label) ? param.key : param.label;
                if (dropdown != null)
                {
                    var opts = (param.options != null) ? new List<string>(param.options) : new List<string>();
                    if (opts.Count == 0) opts.Add(singleOptionFallback);
                    dropdown.ClearOptions();
                    dropdown.AddOptions(opts);
                    dropdown.interactable = requireDropdownEvenSingleOption || opts.Count > 1;
                    dropdown.value = 0;

                    var indexKey = isLua ? param.key : ParticleIndexKey(param.effectName, param.key);
                    IndexControl(indexKey, element, label, dropdown: dropdown);

                    // Seed
                    if (opts.Count > 0)
                    {
                        if (isLua) SeedValue(param, opts[0], true);
                        else       ApplyParticleString(param.effectName, param.key, opts[0]);
                    }

                    dropdown.onValueChanged.AddListener(i => OnDropdownChanged(param, i, isLua));
                    OnDropdownChanged(param, dropdown.value, isLua);
                }
                return;
            }

            case "inputfield":
            {
                element = Instantiate(inputFieldPrefab, panel);
                var tmpInput = element.GetComponentInChildren<TMP_InputField>();
                var uguiIn   = element.GetComponentInChildren<InputField>();
                var label    = element.GetComponent<TMP_Text>() ?? element.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = string.IsNullOrEmpty(param.label) ? param.key : param.label;

                var indexKey = isLua ? param.key : ParticleIndexKey(param.effectName, param.key);
                IndexControl(indexKey, element, label, inputTMP: tmpInput, inputUGUI: uguiIn);

                if (isLua) SeedValue(param, "", true);

                if (tmpInput != null) tmpInput.onEndEdit.AddListener(text => OnInputFieldChanged(param, text, isLua));
                else if (uguiIn != null) uguiIn.onEndEdit.AddListener(text => OnInputFieldChanged(param, text, isLua));
                return;
            }

            case "button":
            {
                element = Instantiate(buttonPrefab, panel);
                var button = element.GetComponentInChildren<Button>();
                var label  = element.GetComponent<TMP_Text>() ?? element.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = string.IsNullOrEmpty(param.label) ? param.key : param.label;
                // Button index uses key always
                IndexControl(param.key, element, label);
                if (button != null) button.onClick.AddListener(() => OnButtonPressed(param));
                return;
            }
        }
    }

    private void IndexControl(string key, GameObject root, TMP_Text label, Slider slider = null, Toggle toggle = null,
                              TMP_Dropdown dropdown = null, TMP_InputField inputTMP = null, InputField inputUGUI = null)
    {
        if (string.IsNullOrEmpty(key)) return;
        var cref = new ControlRefs
        {
            root = root,
            label = label,
            slider = slider,
            toggle = toggle,
            dropdown = dropdown,
            inputTMP = inputTMP,
            inputUGUI = inputUGUI,
            displayLabel = label != null ? label.text : key
        };
        _controlIndex[key] = cref;
    }

    private void SeedValue(ParamUIDef param, object value, bool isLua)
    {
        if (param == null || string.IsNullOrEmpty(param.key)) return;

        if (isLua)
        {
            luaParamValues[param.key] = value;
            if (targetBehavior?.Script != null)
                targetBehavior.Script.Globals[param.key] = value;
        }
    }

    // -------- Change handlers (mark both KEY and LABEL) + Fusion broadcast for Lua controls --------
    private void OnSliderChanged(ParamUIDef param, float value, bool isLua)
    {
        if (isLua)
        {
            luaParamValues[param.key] = value;
            if (targetBehavior?.Script != null)
                targetBehavior.Script.Globals[param.key] = value;

            // Broadcast over Fusion when local user changes the slider
            if (netSync != null && netSync.HasInputAuthority && !netSync.SuppressUIEvents)
                netSync.SendLocalChange(param.key, value);

            MarkLuaToken(param.key);
            MarkLuaToken(param.label);
        }
        else
        {
            ApplyParticleFloat(param.effectName, param.key, value);
            MarkParticleToken(param.key);
            MarkParticleToken(param.label);
            if (!string.IsNullOrEmpty(param.effectName))
                _changedParticleEffects.Add(param.effectName);
        }
        UpdateDescriptionHighlight();
    }

    private void OnToggleChanged(ParamUIDef param, bool value, bool isLua)
    {
        if (isLua)
        {
            luaParamValues[param.key] = value;
            if (targetBehavior?.Script != null)
                targetBehavior.Script.Globals[param.key] = value;

            if (netSync != null && netSync.HasInputAuthority && !netSync.SuppressUIEvents)
                netSync.SendLocalChange(param.key, value);

            MarkLuaToken(param.key);
            MarkLuaToken(param.label);
        }
        else
        {
        ApplyParticleBool(param.effectName, param.key, value);

         MarkParticleToken(param.key);
         MarkParticleToken(param.label);
         if (!string.IsNullOrEmpty(param.effectName))
          _changedParticleEffects.Add(param.effectName);
        }
        UpdateDescriptionHighlight();
    }

    private void OnDropdownChanged(ParamUIDef param, int index, bool isLua)
    {
        if (isLua)
        {
            string chosen = null;
            if (param.options != null && index >= 0 && index < param.options.Count)
                chosen = param.options[index];

            if (chosen != null)
            {
                luaParamValues[param.key] = chosen;
                if (targetBehavior?.Script != null)
                    targetBehavior.Script.Globals[param.key] = chosen;

                if (netSync != null && netSync.HasInputAuthority && !netSync.SuppressUIEvents)
                    netSync.SendLocalChange(param.key, chosen);
            }

            MarkLuaToken(param.key);
            MarkLuaToken(param.label);
        }
        else
        {
            string chosen = null;
            if (param.options != null && index >= 0 && index < param.options.Count)
                chosen = param.options[index];

            ApplyParticleString(param.effectName, param.key, chosen);
            MarkParticleToken(param.key);
            MarkParticleToken(param.label);
            if (!string.IsNullOrEmpty(param.effectName))
                _changedParticleEffects.Add(param.effectName);
        }
        UpdateDescriptionHighlight();
    }

    private void OnInputFieldChanged(ParamUIDef param, string text, bool isLua)
    {
        if (isLua)
        {
            luaParamValues[param.key] = text;
            if (targetBehavior?.Script != null)
                targetBehavior.Script.Globals[param.key] = text;

            if (netSync != null && netSync.HasInputAuthority && !netSync.SuppressUIEvents)
                netSync.SendLocalChange(param.key, text);

            MarkLuaToken(param.key);
            MarkLuaToken(param.label);
        }
        else
        {
            MarkParticleToken(param.key);
            MarkParticleToken(param.label);
            if (!string.IsNullOrEmpty(param.effectName))
                _changedParticleEffects.Add(param.effectName);
        }
        UpdateDescriptionHighlight();
    }

    private void OnButtonPressed(ParamUIDef param)
    {
        if (targetBehavior?.Script == null) return;
        var fn = targetBehavior.Script.Globals.Get(param.key);
        if (fn.Type == DataType.Function) targetBehavior.Script.Call(fn);
    }

    private void MarkLuaToken(string token)
    {
        if (!string.IsNullOrEmpty(token)) _changedLuaTokens.Add(token);
    }

    private void MarkParticleToken(string token)
    {
        if (!string.IsNullOrEmpty(token)) _changedParticleTokens.Add(token);
    }

    // -------- Description highlighting (one control → one color) --------
    private void UpdateDescriptionHighlight()
    {
        if (descriptionText == null) return;
        string source = _baseDescription ?? "";
        if (string.IsNullOrEmpty(source)) { descriptionText.text = ""; return; }

        string luaHex = ColorUtility.ToHtmlStringRGB(luaChangedColor);
        string particleHex = ColorUtility.ToHtmlStringRGB(particleChangedColor);

        // Build token → color map (0 = Lua, 1 = Particle). First writer wins; we don't override.
        var tokenMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tok in _changedLuaTokens)
            if (!tokenMap.ContainsKey(tok)) tokenMap[tok] = 0;

        foreach (var tok in _changedParticleTokens)
            if (!tokenMap.ContainsKey(tok)) tokenMap[tok] = 1;

        foreach (var eff in _changedParticleEffects)
            if (!tokenMap.ContainsKey(eff)) tokenMap[eff] = 1;

        // Replace longer tokens first to avoid partial overlaps
        string result = source;
        foreach (var token in tokenMap.Keys.OrderByDescending(s => s.Length))
        {
            if (string.IsNullOrWhiteSpace(token)) continue;

            string pattern = $@"\b{Regex.Escape(token)}\b";
            var re = new Regex(pattern, RegexOptions.IgnoreCase);
            string hex = tokenMap[token] == 0 ? luaHex : particleHex;
            string colored = $"<b><color=#{hex}>{token}</color></b>";

            result = re.Replace(result, colored);
        }

        descriptionText.text = result;
    }

    // -------- Value export APIs (used by LuaMonoBehavior.ApplyUIParamsToLua) --------
    public IEnumerable<KeyValuePair<string, object>> GetCurrentParams()
    {
        foreach (var kv in luaParamValues)
            yield return kv;
    }

    public Dictionary<string, object> CollectAllParamValues()
    {
        return new Dictionary<string, object>(luaParamValues, StringComparer.Ordinal);
    }

    // -------- Called by LuaParamFusionSync to reflect remote changes without looping --------
    // NOTE: This is currently used for LUA params only (index key = plain 'key').
    // Particle controls use composite keys and are not network-synced by default.
    public void SetControlVisual(string key, LuaParamType type, float fVal, bool bVal, string sVal)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (!_controlIndex.TryGetValue(key, out var c) || c == null) return;

        switch (type)
        {
            case LuaParamType.Float:
                if (c.slider != null)
                {
                    c.slider.SetValueWithoutNotify(fVal);
                    if (c.label != null) c.label.text = FormatSliderLabel(c.displayLabel ?? key, fVal);
                }
                else if (c.inputTMP != null)
                {
                    c.inputTMP.SetTextWithoutNotify(fVal.ToString("0.##"));
                }
                else if (c.inputUGUI != null)
                {
                    c.inputUGUI.SetTextWithoutNotify(fVal.ToString("0.##"));
                }
                break;

            case LuaParamType.Bool:
                if (c.toggle != null)
                    c.toggle.SetIsOnWithoutNotify(bVal);
                break;

            case LuaParamType.String:
                if (c.dropdown != null && c.dropdown.options != null && c.dropdown.options.Count > 0)
                {
                    int idx = c.dropdown.options.FindIndex(o => string.Equals(o.text, sVal, StringComparison.Ordinal));
                    if (idx < 0) idx = 0;
                    c.dropdown.SetValueWithoutNotify(idx);
                }
                else if (c.inputTMP != null)
                    c.inputTMP.SetTextWithoutNotify(sVal ?? "");
                else if (c.inputUGUI != null)
                    c.inputUGUI.SetTextWithoutNotify(sVal ?? "");
                break;
        }
    }
}
