// LuaParamUIBuilder.cs (Prefab-Free Version)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using System;

public class LuaParamUIBuilder : MonoBehaviour
{
    public Transform uiParent; // World-space canvas parent
    public LuaMonoBehavior targetBehavior;
    public Dictionary<string, ParticleSystem> particleSystems = new();

    public void BuildUIFromComment(string comment)
    {
        List<ParamUIDef> parsedParams = ParseUIParamsFromComment(comment);
        foreach (Transform child in uiParent) Destroy(child.gameObject);
        foreach (var param in parsedParams)
        {
            BuildUIElement(param);
        }
    }

    void BuildUIElement(ParamUIDef param)
    {
        GameObject container = new GameObject(param.key + "_Container");
        container.transform.SetParent(uiParent);
        RectTransform rect = container.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 40);
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.childForceExpandHeight = false;

        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(container.transform);
        TextMeshProUGUI label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = param.label;
        label.fontSize = 18;

        switch (param.type)
        {
            case "Slider":
                GameObject sliderGO = new GameObject("Slider");
                sliderGO.transform.SetParent(container.transform);
                Slider slider = sliderGO.AddComponent<Slider>();
                slider.minValue = param.min;
                slider.maxValue = param.max;
                slider.onValueChanged.AddListener((val) => OnSliderChanged(param, val));
                break;

            case "Toggle":
                GameObject toggleGO = new GameObject("Toggle");
                toggleGO.transform.SetParent(container.transform);
                Toggle toggle = toggleGO.AddComponent<Toggle>();
                toggle.onValueChanged.AddListener((val) => OnToggleChanged(param, val));
                break;

            case "Dropdown":
                GameObject dropdownGO = new GameObject("Dropdown");
                dropdownGO.transform.SetParent(container.transform);
                TMP_Dropdown dropdown = dropdownGO.AddComponent<TMP_Dropdown>();
                dropdown.AddOptions(param.options);
                dropdown.onValueChanged.AddListener((index) => OnDropdownChanged(param, index));
                break;

            case "InputField":
                GameObject inputGO = new GameObject("InputField");
                inputGO.transform.SetParent(container.transform);
                TMP_InputField input = inputGO.AddComponent<TMP_InputField>();
                input.onEndEdit.AddListener((text) => OnInputFieldChanged(param, text));
                break;

            case "Button":
                GameObject buttonGO = new GameObject("Button");
                buttonGO.transform.SetParent(container.transform);
                Button button = buttonGO.AddComponent<Button>();
                TextMeshProUGUI btnText = buttonGO.AddComponent<TextMeshProUGUI>();
                btnText.text = param.label;
                button.onClick.AddListener(() => OnButtonPressed(param));
                break;
        }
    }

    void OnSliderChanged(ParamUIDef param, float value)
    {
        targetBehavior.Script.Globals[param.key] = DynValue.NewNumber(value);
    }

    void OnToggleChanged(ParamUIDef param, bool value)
    {
        targetBehavior.Script.Globals[param.key] = DynValue.NewBoolean(value);
    }

    void OnDropdownChanged(ParamUIDef param, int index)
    {
        if (param.options != null && index >= 0 && index < param.options.Count)
        {
            targetBehavior.Script.Globals[param.key] = DynValue.NewString(param.options[index]);
        }
    }

    void OnInputFieldChanged(ParamUIDef param, string text)
    {
        targetBehavior.Script.Globals[param.key] = DynValue.NewString(text);
    }

    void OnButtonPressed(ParamUIDef param)
    {
        var fn = targetBehavior.Script.Globals.Get(param.key);
        if (fn.Type == DataType.Function)
        {
            targetBehavior.Script.Call(fn);
        }
    }

    List<ParamUIDef> ParseUIParamsFromComment(string comment)
    {
        var list = new List<ParamUIDef>();
        var sliderRx = new System.Text.RegularExpressions.Regex(@"(\w+)\s*\(slider(?::\s*(\d+)-(\d+))?\)");
        var inputRx = new System.Text.RegularExpressions.Regex(@"(\w+)\s*\(input box\)");
        var dropdownRx = new System.Text.RegularExpressions.Regex(@"(\w+)\s*\(dropdown:\s*\[(.*?)\]\)");
        var toggleRx = new System.Text.RegularExpressions.Regex(@"(\w+)\s*\(toggle\)");
        var buttonRx = new System.Text.RegularExpressions.Regex(@"(\w+)\s*\(button\)");

        foreach (System.Text.RegularExpressions.Match m in sliderRx.Matches(comment))
        {
            float min = 0, max = 10;
            if (m.Groups[2].Success && m.Groups[3].Success)
            {
                float.TryParse(m.Groups[2].Value, out min);
                float.TryParse(m.Groups[3].Value, out max);
            }
            list.Add(new ParamUIDef { key = m.Groups[1].Value, label = m.Groups[1].Value, type = "Slider", min = min, max = max });
        }
        foreach (System.Text.RegularExpressions.Match m in inputRx.Matches(comment))
        {
            list.Add(new ParamUIDef { key = m.Groups[1].Value, label = m.Groups[1].Value, type = "InputField" });
        }
        foreach (System.Text.RegularExpressions.Match m in dropdownRx.Matches(comment))
        {
            var options = new List<string>(m.Groups[2].Value.Split(','));
            list.Add(new ParamUIDef { key = m.Groups[1].Value, label = m.Groups[1].Value, type = "Dropdown", options = options });
        }
        foreach (System.Text.RegularExpressions.Match m in toggleRx.Matches(comment))
        {
            list.Add(new ParamUIDef { key = m.Groups[1].Value, label = m.Groups[1].Value, type = "Toggle" });
        }
        foreach (System.Text.RegularExpressions.Match m in buttonRx.Matches(comment))
        {
            list.Add(new ParamUIDef { key = m.Groups[1].Value, label = m.Groups[1].Value, type = "Button" });
        }
        return list;
    }
}
