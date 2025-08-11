using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using TMPro;

public class LuaParamUIBuilder : MonoBehaviour
{
    public Transform uiParent;
    public GameObject sliderPrefab;
    public GameObject togglePrefab;
    public GameObject dropdownPrefab;
    public GameObject inputFieldPrefab;
    public GameObject buttonPrefab;
    public GameObject textPrefab;
    public LuaMonoBehavior targetBehavior;
    public Dictionary<string, ParticleSystem> particleSystems = new();

    [Serializable]
    public class ParamUIDef
    {
        public string label;
        public string type;
        public string key;
        public float min;
        public float max;
        public float @default;
        public List<string> options;
    }

    public void BuildFromJSON(List<ParamUIDef> luaParams, List<ParamUIDef> particleParams)
    {
        foreach (Transform child in uiParent) Destroy(child.gameObject);
        foreach (var param in luaParams) BuildUIElement(param, true);
        foreach (var param in particleParams) BuildUIElement(param, false);
    }

    public void BuildUIFromComment(string comment)
    {
        foreach (Transform child in uiParent) Destroy(child.gameObject);

        var sliderRx = new Regex("(\\w+)\\s*\\(slider(?:\\:\\s*(-?\\d+(\\.\\d+)?)-(-?\\d+(\\.\\d+)?))?\\)");
        var toggleRx = new Regex("(\\w+)\\s*\\(toggle\\)");
        var inputRx = new Regex("(\\w+)\\s*\\(input box\\)");
        var dropdownRx = new Regex("(\\w+)\\s*\\(dropdown:\\s*\\[([^\\]]+)\\]\\)");

        int index = 0;
        while (index < comment.Length)
        {
            Match sliderMatch = sliderRx.Match(comment, index);
            Match toggleMatch = toggleRx.Match(comment, index);
            Match inputMatch = inputRx.Match(comment, index);
            Match dropdownMatch = dropdownRx.Match(comment, index);

            Match next = GetFirstMatch(sliderMatch, toggleMatch, inputMatch, dropdownMatch);

            if (next == null || !next.Success)
            {
                AddText(comment.Substring(index).Trim());
                break;
            }

            if (next.Index > index)
            {
                string before = comment.Substring(index, next.Index - index).Trim();
                if (!string.IsNullOrEmpty(before)) AddText(before);
            }

            string key = next.Groups[1].Value;
            string label = key;
            string type = "InputField";

            if (sliderRx.IsMatch(next.Value))
            {
                type = "Slider";
                float min = next.Groups[2].Success ? float.Parse(next.Groups[2].Value) : 0f;
                float max = next.Groups[4].Success ? float.Parse(next.Groups[4].Value) : 10f;
                BuildUIElement(new ParamUIDef { key = key, label = label, type = type, min = min, max = max, @default = (min + max) / 2 }, true);
            }
            else if (toggleRx.IsMatch(next.Value))
            {
                type = "Toggle";
                BuildUIElement(new ParamUIDef { key = key, label = label, type = type }, true);
            }
            else if (inputRx.IsMatch(next.Value))
            {
                type = "InputField";
                BuildUIElement(new ParamUIDef { key = key, label = label, type = type }, true);
            }
            else if (dropdownRx.IsMatch(next.Value))
            {
                type = "Dropdown";
                var options = new List<string>(next.Groups[2].Value.Split(','));
                for (int i = 0; i < options.Count; i++) options[i] = options[i].Trim();
                BuildUIElement(new ParamUIDef { key = key, label = label, type = type, options = options }, true);
            }

            index = next.Index + next.Length;
        }
    }

    Match GetFirstMatch(params Match[] matches)
    {
        Match first = null;
        foreach (var m in matches)
        {
            if (m.Success && (first == null || m.Index < first.Index))
                first = m;
        }
        return first;
    }

    void AddText(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        GameObject txt = Instantiate(textPrefab, uiParent);
        var text = txt.GetComponent<TMP_Text>();
        if (text == null) text = txt.GetComponentInChildren<TMP_Text>();
        if (text != null) text.text = content;
    }

    void BuildUIElement(ParamUIDef param, bool isLua)
    {
        GameObject element = null;

        switch (param.type.ToLower())
        {
            case "slider":
                element = Instantiate(sliderPrefab, uiParent);
                var slider = element.GetComponentInChildren<Slider>();
                slider.minValue = param.min;
                slider.maxValue = param.max;
                slider.value = param.@default;
                slider.onValueChanged.AddListener((val) => OnSliderChanged(param, val, isLua));
                break;

            case "toggle":
                element = Instantiate(togglePrefab, uiParent);
                var toggle = element.GetComponentInChildren<Toggle>();
                toggle.onValueChanged.AddListener((val) => OnToggleChanged(param, val, isLua));
                break;

            case "dropdown":
                element = Instantiate(dropdownPrefab, uiParent);
                var dropdown = element.GetComponentInChildren<TMP_Dropdown>();
                dropdown.ClearOptions();
                dropdown.AddOptions(param.options);
                dropdown.value = 0;
                dropdown.onValueChanged.AddListener((i) => OnDropdownChanged(param, i, isLua));
                break;

            case "inputfield":
                element = Instantiate(inputFieldPrefab, uiParent);
                var input = element.GetComponentInChildren<InputField>();
                input.onEndEdit.AddListener((text) => OnInputFieldChanged(param, text, isLua));
                break;

            case "button":
                element = Instantiate(buttonPrefab, uiParent);
                var button = element.GetComponentInChildren<Button>();
                button.onClick.AddListener(() => OnButtonPressed(param));
                break;
        }

        if (element != null)
        {
            var label = element.GetComponent<TMP_Text>();
            if (label == null) label = element.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = param.label;
        }
    }

    void OnSliderChanged(ParamUIDef param, float value, bool isLua)
    {
        if (isLua)
            targetBehavior.Script.Globals[param.key] = value;
        else if (particleSystems.TryGetValue(param.label, out var ps))
        {
            var main = ps.main;
            if (param.key == "startSize") main.startSize = value;
            if (param.key == "startSpeed") main.startSpeed = value;
            if (param.key == "lifetime") main.startLifetime = value;
        }
    }

    void OnToggleChanged(ParamUIDef param, bool value, bool isLua)
    {
        if (isLua)
            targetBehavior.Script.Globals[param.key] = value;
        else if (particleSystems.TryGetValue(param.label, out var ps))
            ps.gameObject.SetActive(value);
    }

    void OnDropdownChanged(ParamUIDef param, int index, bool isLua)
    {
        if (isLua && param.options != null && index >= 0 && index < param.options.Count)
            targetBehavior.Script.Globals[param.key] = param.options[index];
    }

    void OnInputFieldChanged(ParamUIDef param, string text, bool isLua)
    {
        if (isLua)
            targetBehavior.Script.Globals[param.key] = text;
    }

    void OnButtonPressed(ParamUIDef param)
    {
        var fn = targetBehavior.Script.Globals.Get(param.key);
        if (fn.Type == MoonSharp.Interpreter.DataType.Function)
            targetBehavior.Script.Call(fn);
    }
}
