using System;
using UnityEngine;

namespace DGame
{
    public enum UIFieldCodeStyle
    {
        [InspectorName("_开头命名[e.g., _variable]")]
        UnderscorePrefix,

        [InspectorName("m_开头命名[e.g., m_variable]")]
        MPrefix,

        [InspectorName("占位符")]
        Max,
    }

    [Serializable]
    public class UIScriptGenerateRuler
    {
        public string uiElementRegex;
        public UIComponentName componentName;
        public bool isUIWidget;

        public UIScriptGenerateRuler(string uiElementRegex, UIComponentName componentName, bool isUIWidget = false)
        {
            this.uiElementRegex = uiElementRegex;
            this.componentName = componentName;
            this.isUIWidget = isUIWidget;
        }
    }

    public enum UIComponentName
    {
        GameObject,
        Transform,
        RectTransform,
        Text,
        RichTextItem,
        Button,
        Image,
        RawImage,
        ScrollRect,
        Scrollbar,
        InputField,
        GridLayoutGroup,
        HorizontalLayoutGroup,
        VerticalLayoutGroup,
        Slider,
        Toggle,
        ToggleGroup,
        AnimationCurve,
        CanvasGroup,
        TextMeshProUGUI,
        Canvas,
        Dropdown,
    }
}