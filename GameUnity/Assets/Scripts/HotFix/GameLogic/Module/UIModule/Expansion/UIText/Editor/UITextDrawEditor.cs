#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public static class UITextDrawEditor
    {
        [MenuItem("GameObject/UI/UIText", priority = 31)]
        public static void CreateUIText()
        {
            UIText uiText = new GameObject("UIText", typeof(RectTransform), typeof(UIText)).GetComponent<UIText>();
            UnityEditorUtil.ResetInCanvasFor(uiText.rectTransform);
            uiText.text = "UIText";
            uiText.color = Color.black;
            uiText.fontSize = 24;
            uiText.raycastTarget = false;
            uiText.rectTransform.sizeDelta = new Vector2(200, 50);
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.rectTransform.localPosition = Vector3.zero;
        }

        #region 字符间距

        public static void DrawTextSpacingGUI(string title, ref bool isPanelOpen, SerializedProperty isUseTextSpacing,
            SerializedProperty textSpacing)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseTextSpacing, new GUIContent("开启字符间距"));

                if (isUseTextSpacing.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(textSpacing, new GUIContent("字符间距"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 顶点颜色

        // 定义显示名称数组
        private static string[] colorFilterTypeNames = new[] { "相加模式", "叠加模式" };

        public static void DrawTextVertexColorGUI(string title, ref bool isPanelOpen, SerializedProperty isUseVertexColor,
            SerializedProperty colorFilterType, SerializedProperty vertexTopLeftColor, SerializedProperty vertexTopRightColor,
            SerializedProperty vertexBottomLeftColor, SerializedProperty vertexBottomRightColor, SerializedProperty vertexColorOffset)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseVertexColor, new GUIContent("开启顶点颜色"));

                if (isUseVertexColor.boolValue)
                {
                    EditorGUI.indentLevel++;
                    UnityEditorUtil.LayoutHorizontal(() =>
                    {
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), vertexTopLeftColor, new GUIContent());
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), vertexTopRightColor, new GUIContent());
                    });
                    UnityEditorUtil.LayoutHorizontal(() =>
                    {
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), vertexBottomLeftColor, new GUIContent());
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), vertexBottomRightColor, new GUIContent());
                    });
                    EditorGUILayout.PropertyField(colorFilterType, new GUIContent("颜色叠加模式"));
                    // colorFilterType.enumValueIndex =
                    //     (int)(UITextVertexColorExtend.ColorFilterType)EditorGUILayout.EnumPopup(
                    //         new GUIContent("颜色叠加模式"),
                    //         (UITextVertexColorExtend.ColorFilterType)colorFilterType.enumValueIndex
                    //     );
                    var newOffset = EditorGUILayout.Vector2Field("颜色偏移", vertexColorOffset.vector2Value);
                    newOffset.x = Mathf.Clamp(newOffset.x, -1f, 1f);
                    newOffset.y = Mathf.Clamp(newOffset.y, -1f, 1f);
                    vertexColorOffset.vector2Value = newOffset;
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 字体阴影

        public static void DrawTextShadowGUI(string title, ref bool isPanelOpen, SerializedProperty isUseTextShadow,
            SerializedProperty topLeftColor, SerializedProperty topRightColor, SerializedProperty bottomLeftColor,
            SerializedProperty bottomRightColor, SerializedProperty effectDistance)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseTextShadow, new GUIContent("开启字体阴影"));

                if (isUseTextShadow.boolValue)
                {
                    EditorGUI.indentLevel++;
                    UnityEditorUtil.LayoutHorizontal(() =>
                    {
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), topLeftColor, new GUIContent());
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), topRightColor, new GUIContent());
                    });
                    UnityEditorUtil.LayoutHorizontal(() =>
                    {
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), bottomLeftColor, new GUIContent());
                        EditorGUI.PropertyField(UnityEditorUtil.GetGUIRect(0, 18), bottomRightColor, new GUIContent());
                    });

                    EditorGUILayout.PropertyField(effectDistance, new GUIContent("EffectDistance"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 字体描边

        public static void DrawTextOutLineAndGradientGUI(string title, ref bool isPanelOpen,
            SerializedProperty isUseTextOutLine,
            SerializedProperty isOpenShaderOutLine, SerializedProperty outLineColor,
            SerializedProperty outLineWidth, SerializedProperty alpha,
            UITextOutlineEffect textEffect)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseTextOutLine, new GUIContent("开启字体描边"));
                textEffect?.SetUseOutLineColor(isUseTextOutLine.boolValue);

                if (isUseTextOutLine.boolValue)
                {
                    EditorGUI.indentLevel++;

                    EditorGUILayout.PropertyField(alpha, new GUIContent("透明度"));
                    EditorGUILayout.PropertyField(isOpenShaderOutLine, new GUIContent("开启Shader描边"));
                    textEffect?.SetShaderOutLine(isOpenShaderOutLine.boolValue);
                    EditorGUILayout.PropertyField(outLineWidth, new GUIContent("描边大小"));
                    textEffect?.SetOutLineWidth(outLineWidth.intValue);
                    EditorGUILayout.PropertyField(outLineColor, new GUIContent("描边颜色"));
                    textEffect?.SetOutLineColor(outLineColor.colorValue);
                    EditorGUI.indentLevel--;
                }

                textEffect?.SetAlpha(alpha.floatValue);
                textEffect?.UpdateOutLineInfos();
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 字体自适应

        public static void DrawTextBestFitGUI(string title, ref bool isPanelOpen, SerializedProperty isUseTextBestFit, SerializedProperty resizeTextForBestFit)
        {
            if (resizeTextForBestFit.boolValue)
            {
                UnityEditorUtil.LayoutFrameBox(() =>
                {
                    EditorGUILayout.PropertyField(isUseTextBestFit, new GUIContent("开启字体自适应大小"));
                }, title, ref isPanelOpen, true);
            }
        }

        #endregion

        #region 字体渐变

        public static void DrawTextGradientColorGUI(string title, ref bool isPanelOpen,
            SerializedProperty isUseGradientColor, GradientColorEditor gradientColorEditor,
            UITextGradientColorExtend gradientColorExtend)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(isUseGradientColor, new GUIContent("开启字体渐变"));
                UITextGradientColor gradientColor = null;
                if (EditorGUI.EndChangeCheck())
                {
                    gradientColorExtend?.SetUseGradientColor(isUseGradientColor.boolValue);
                }
                gradientColor = gradientColorExtend?.gradientColorEffect;
                // gradientColor?.SetUseGradientColor(isUseGradientColor.boolValue);

                if (isUseGradientColor.boolValue)
                {
                    EditorGUI.indentLevel++;
                    gradientColorEditor?.OnInspectorGUI(gradientColor);
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 环形字体

        public static void DrawTextCircleGUI(string title, ref bool isPanelOpen, SerializedProperty useTextCircle, SerializedProperty radius, SerializedProperty spaceCoff, SerializedProperty angleOffset)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(useTextCircle, new GUIContent("开启环形字体"));

                if (useTextCircle.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(radius, new GUIContent("半径"));
                    EditorGUILayout.PropertyField(spaceCoff, new GUIContent("字符间距"));
                    EditorGUILayout.PropertyField(angleOffset, new GUIContent("起始角度偏移"));
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 字体快捷按钮区域

        private static Vector2 m_fontScrollPosition;
        private static Color m_selectedTextColor =  new Color(0.2f, 0.8f, 0.3f);

        public static void DrawTextFontsGUI(string title, ref bool isPanelOpen, Text text, List<Font> fonts, Action<Font> onClick)
        {
            if (fonts == null || fonts.Count == 0)
            {
                return;
            }

            UnityEditorUtil.LayoutFrameBox(() =>
            {
                DrawFontGrid(fonts, onClick, text, 360);
            }, title, ref isPanelOpen, true);
        }

        private static void DrawFontGrid(List<Font> fonts, Action<Font> onClick, Text text, int panelWidth)
        {
            if (fonts.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到匹配的字体", MessageType.Info);
                return;
            }

            // 计算网格布局
            int columns = Mathf.Max(1, Mathf.FloorToInt(panelWidth / 120f));
            int rows = Mathf.CeilToInt((float)fonts.Count / columns);

            m_fontScrollPosition = EditorGUILayout.BeginScrollView(m_fontScrollPosition);

            int currentIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    for (int col = 0; col < columns; col++)
                    {
                        if (currentIndex >= fonts.Count)
                            break;

                        Font font = fonts[currentIndex];

                        if (DrawAutoSizeButton(font, text, 150))
                        {
                            onClick?.Invoke(font);
                        }

                        currentIndex++;
                    }

                    for (int col = currentIndex % columns; col < columns && col > 0; col++)
                    {
                        GUILayout.Label("", GUILayout.Width(120));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.Space(5);
            var tips = "悬停可以查看字体的名字，按钮上的文字和提示均为字体预览样式";
            if (text != null && text.font != null)
            {
                tips += $"\n当前选中字体：{text.font.name}";
            }
            DrawCustomStyledHelpBox(tips, text);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawCustomStyledHelpBox(string message, Text text, int fontSize = 12, FontStyle fontStyle = FontStyle.Normal)
        {
            GUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUIStyle iconStyle = new GUIStyle(EditorStyles.label);
                    iconStyle.padding = new RectOffset(0, 0, 0, 5);
                    iconStyle.alignment = TextAnchor.MiddleCenter;

                    GUIContent iconContent = EditorGUIUtility.IconContent("console.infoicon");
                    if (iconContent != null && iconContent.image != null)
                    {
                        GUILayout.Label(iconContent, iconStyle, GUILayout.Width(40), GUILayout.Height(40));
                    }
                    GUIStyle textStyle = new GUIStyle(EditorStyles.label);
                    textStyle.normal.textColor = EditorStyles.label.normal.textColor;
                    textStyle.wordWrap = true;
                    textStyle.padding = new RectOffset(0, 5, 5, 0);
                    textStyle.margin = new RectOffset(0, 0, 0, 0);
                    textStyle.fontSize = fontSize;
                    if (text != null)
                    {
                        textStyle.font = text.font;
                        textStyle.fontStyle = text.fontStyle;
                    }
                    else
                    {
                        textStyle.fontStyle = fontStyle;
                    }
                    EditorGUILayout.LabelField(message, textStyle);
                }
                EditorGUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static bool DrawAutoSizeButton(Font font, Text text, float width = 120f)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.font = font;
            var fontName = font.name;
            if (fontName.Length > 16)
            {
                fontName = fontName.Substring(0, 16) + "...";
            }
            GUIContent content = new GUIContent(fontName, $"字体名:{font.name}");

            var oriColor = GUI.color;
            if (text != null && text.font != null && text.font.name == font.name)
            {
                GUI.color = m_selectedTextColor;
            }

            bool isClick = GUILayout.Button(content, buttonStyle, GUILayout.Width(width));
            GUI.color = oriColor;
            return isClick;
        }

        #endregion
    }
}

#endif