#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        public static void DrawTextGradientColorGUI(string title, ref bool isPanelOpen, SerializedProperty isUseGradientColor, UITextGradientColor gradientColor, GradientColorEditor gradientColorEditor)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.PropertyField(isUseGradientColor, new GUIContent("开启字体渐变"));
                gradientColor?.SetUseGradientColor(isUseGradientColor.boolValue);

                if (isUseGradientColor.boolValue)
                {
                    EditorGUI.indentLevel++;

                    gradientColorEditor?.OnInspectorGUI(gradientColor);
                    EditorGUI.indentLevel--;
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion

        #region 字体快捷按钮区域

        public static void DrawTextFontsGUI(string title, ref bool isPanelOpen, List<Font> fonts, Action<Font> onClick)
        {
            if (fonts == null || fonts.Count == 0)
            {
                return;
            }

            UnityEditorUtil.LayoutFrameBox(() =>
            {
                for (int i = 0; i < fonts.Count; i++)
                {
                    if (UnityEditorUtil.DrawAutoSizeButton(fonts[i].name))
                    {
                        onClick?.Invoke(fonts[i]);
                    }
                }
            }, title, ref isPanelOpen, true);
        }

        #endregion
    }
}

#endif