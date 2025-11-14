#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    // [CustomEditor(typeof(UITextGradientColor))]
    public class GradientColorEditor
    {
        private SerializedProperty m_colorTop;
        private SerializedProperty m_colorBottom;
        private SerializedProperty m_colorLeft;
        private SerializedProperty m_colorRight;
        private SerializedProperty m_gradientOffsetVertical;
        private SerializedProperty m_gradientOffsetHorizontal;
        private SerializedProperty m_splitTextGradient;

        // UI状态
        private bool m_showVerticalSettings = true;
        private bool m_showHorizontalSettings = true;
        private bool m_showAdvancedSettings = true;
        private bool m_showPreview = true;

        // 预览相关
        private Texture2D m_verticalPreview;
        private Texture2D m_horizontalPreview;
        private Texture2D m_combinedPreview;
        private const int PREVIEW_SIZE = 80;

        // 颜色定义
        private Color m_headerColor = new Color(0.1f, 0.5f, 0.8f, 1f);
        private Color m_successColor = new Color(0.2f, 0.8f, 0.3f, 1f);

        private SerializedObject serializedObject;
        private UITextGradientColor m_gradientColor;

        public void OnEnable(SerializedObject _serializedObject, UIText uiText)
        {
            serializedObject = _serializedObject;
            uiText?.TryGetComponent(out m_gradientColor);
            m_colorTop = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_colorTop");
            m_colorBottom = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_colorBottom");
            m_colorLeft = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_colorLeft");
            m_colorRight = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_colorRight");
            m_gradientOffsetVertical =
                serializedObject.FindProperty("m_uiTextGradientColorExtend.m_gradientOffsetVertical");
            m_gradientOffsetHorizontal =
                serializedObject.FindProperty("m_uiTextGradientColorExtend.m_gradientOffsetHorizontal");
            m_splitTextGradient = serializedObject.FindProperty("m_uiTextGradientColorExtend.m_splitTextGradient");

            CreatePreviewTextures();
        }

        public void OnDisable()
        {
            DestroyPreviewTextures();
        }

        public void OnInspectorGUI(UITextGradientColor gradientColor)
        {
            // serializedObject.Update();
            // 绘制标题区域
            // DrawInspectorHeader();

            // 绘制预览区域
            // DrawPreviewSection(gradientColor);

            // 绘制垂直渐变设置
            DrawVerticalGradientSettings(gradientColor);

            // 绘制水平渐变设置
            DrawHorizontalGradientSettings(gradientColor);

            // 绘制高级设置
            DrawAdvancedSettings(gradientColor);

            // 绘制操作按钮
            DrawActionButtons(gradientColor);

            // serializedObject.ApplyModifiedProperties();
        }

        private void DrawInspectorHeader()
        {
            // 标题背景
            // Rect headerRect = EditorGUILayout.GetControlRect(false, 50);
            // EditorGUI.DrawRect(new Rect(headerRect.x, headerRect.y, headerRect.width, 50),
            //     new Color(0.1f, 0.1f, 0.1f, 0.8f));

            GUILayout.Space(5);

            // 主标题
            EditorGUILayout.BeginHorizontal();
            // GUILayout.FlexibleSpace();

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            EditorGUILayout.LabelField(new GUIContent("渐变颜色效果", "Gradient Color Effect"),
                titleStyle, GUILayout.Height(30));

            // GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            // 副标题
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
            };

            EditorGUILayout.LabelField("为UI元素添加垂直和水平渐变颜色效果", subtitleStyle);
            GUILayout.Space(5);

            // 分隔线
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            GUILayout.Space(10);
        }

        private void DrawPreviewSection(UITextGradientColor gradientColor)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    UpdatePreviewTextures();

                    EditorGUILayout.BeginHorizontal();
                    {
                        // GUILayout.FlexibleSpace();

                        // 垂直渐变预览
                        EditorGUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField("垂直渐变", EditorStyles.miniLabel);
                            GUILayout.Box(m_verticalPreview, GUILayout.Width(PREVIEW_SIZE),
                                GUILayout.Height(PREVIEW_SIZE));
                        }
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(20);

                        // 水平渐变预览
                        EditorGUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField("水平渐变", EditorStyles.miniLabel);
                            GUILayout.Box(m_horizontalPreview, GUILayout.Width(PREVIEW_SIZE),
                                GUILayout.Height(PREVIEW_SIZE));
                        }
                        EditorGUILayout.EndVertical();

                        GUILayout.Space(20);

                        // 组合渐变预览
                        EditorGUILayout.BeginVertical();
                        {
                            EditorGUILayout.LabelField("组合效果", EditorStyles.miniLabel);
                            GUILayout.Box(m_combinedPreview, GUILayout.Width(PREVIEW_SIZE),
                                GUILayout.Height(PREVIEW_SIZE));
                        }
                        EditorGUILayout.EndVertical();

                        // GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 预览说明
                    EditorGUILayout.HelpBox("预览显示当前设置的渐变效果\n组合效果 = 垂直渐变 × 水平渐变", MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }, "渐变预览", ref m_showPreview, true);

            GUILayout.Space(10);
        }

        private void DrawVerticalGradientSettings(UITextGradientColor gradientColor)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 顶部颜色
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("顶部颜色", GUILayout.Width(80));
                        m_colorTop.colorValue = EditorGUILayout.ColorField(m_colorTop.colorValue);
                        gradientColor.colorTop = m_colorTop.colorValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_colorTop.colorValue = Color.white;
                            gradientColor.colorTop = m_colorTop.colorValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 底部颜色
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("底部颜色", GUILayout.Width(80));
                        m_colorBottom.colorValue = EditorGUILayout.ColorField(m_colorBottom.colorValue);
                        gradientColor.colorBottom = m_colorBottom.colorValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_colorBottom.colorValue = Color.white;
                            gradientColor.colorBottom = m_colorBottom.colorValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 垂直偏移
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("垂直偏移", GUILayout.Width(80));
                        m_gradientOffsetVertical.floatValue =
                            EditorGUILayout.Slider(m_gradientOffsetVertical.floatValue, -1f, 1f);
                        gradientColor.gradientOffsetVertical = m_gradientOffsetVertical.floatValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_gradientOffsetVertical.floatValue = 0f;
                            gradientColor.gradientOffsetVertical = m_gradientOffsetVertical.floatValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 偏移说明
                    string offsetDescription = GetOffsetDescription(m_gradientOffsetVertical.floatValue);
                    EditorGUILayout.HelpBox(offsetDescription, MessageType.Info);

                    EditorGUILayout.Space(3);

                    // 快速操作
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("交换颜色"))
                        {
                            SwapColors(m_colorTop, m_colorBottom);
                            gradientColor.colorTop = m_colorTop.colorValue;
                            gradientColor.colorBottom = m_colorBottom.colorValue;
                        }

                        if (GUILayout.Button("复制到水平"))
                        {
                            CopyToHorizontal();
                            gradientColor.colorLeft = m_colorLeft.colorValue;
                            gradientColor.colorRight = m_colorRight.colorValue;
                            gradientColor.gradientOffsetHorizontal = m_gradientOffsetVertical.floatValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }, "垂直渐变设置", ref m_showVerticalSettings, true);

            GUILayout.Space(8);
        }

        private void DrawHorizontalGradientSettings(UITextGradientColor gradientColor)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 左侧颜色
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("左侧颜色", GUILayout.Width(80));
                        m_colorLeft.colorValue = EditorGUILayout.ColorField(m_colorLeft.colorValue);
                        gradientColor.colorLeft = m_colorLeft.colorValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_colorLeft.colorValue = Color.white;
                            gradientColor.colorLeft = m_colorLeft.colorValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 右侧颜色
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("右侧颜色", GUILayout.Width(80));
                        m_colorRight.colorValue = EditorGUILayout.ColorField(m_colorRight.colorValue);
                        gradientColor.colorRight = m_colorRight.colorValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_colorRight.colorValue = Color.white;
                            gradientColor.colorRight = m_colorRight.colorValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);

                    // 水平偏移
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("水平偏移", GUILayout.Width(80));
                        m_gradientOffsetHorizontal.floatValue =
                            EditorGUILayout.Slider(m_gradientOffsetHorizontal.floatValue, -1f, 1f);
                        gradientColor.gradientOffsetHorizontal = m_gradientOffsetHorizontal.floatValue;

                        if (GUILayout.Button("重置", GUILayout.Width(50)))
                        {
                            m_gradientOffsetHorizontal.floatValue = 0f;
                            gradientColor.gradientOffsetHorizontal = m_gradientOffsetHorizontal.floatValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 偏移说明
                    string offsetDescription = GetOffsetDescription(m_gradientOffsetHorizontal.floatValue);
                    EditorGUILayout.HelpBox(offsetDescription, MessageType.Info);

                    EditorGUILayout.Space(3);

                    // 快速操作
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("交换颜色"))
                        {
                            SwapColors(m_colorLeft, m_colorRight);
                            gradientColor.colorRight = m_colorLeft.colorValue;
                            gradientColor.colorLeft = m_colorRight.colorValue;
                        }

                        if (GUILayout.Button("复制到垂直"))
                        {
                            CopyToVertical();
                            gradientColor.colorTop = m_colorLeft.colorValue;
                            gradientColor.colorBottom = m_colorRight.colorValue;
                            gradientColor.gradientOffsetVertical = m_gradientOffsetHorizontal.floatValue;
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }, "水平渐变设置", ref m_showHorizontalSettings, true);
            GUILayout.Space(8);
        }

        private void DrawAdvancedSettings(UITextGradientColor gradientColor)
        {
            UnityEditorUtil.LayoutFrameBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    // 文本分割渐变
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField(new GUIContent("文本分割渐变", "为每个字符单独计算渐变"),
                            GUILayout.Width(120));
                        m_splitTextGradient.boolValue = EditorGUILayout.Toggle(m_splitTextGradient.boolValue);
                        gradientColor.splitTextGradient = m_splitTextGradient.boolValue;
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(3);

                    if (m_splitTextGradient.boolValue)
                    {
                        EditorGUILayout.HelpBox("已启用文本分割渐变\n每个字符将独立计算渐变效果，适用于文本元素",
                            MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("未启用文本分割渐变\n整个UI元素使用统一的渐变计算",
                            MessageType.Info);
                    }

                    EditorGUILayout.Space(5);

                    // 性能说明
                    EditorGUILayout.LabelField("性能说明", EditorStyles.miniBoldLabel);
                    EditorGUILayout.HelpBox(
                        "• 文本分割渐变: 每个字符独立计算，效果更好但性能稍低\n" +
                        "• 统一渐变: 整个元素统一计算，性能更好但效果较简单",
                        MessageType.None);
                }
                EditorGUILayout.EndVertical();
            }, "高级设置", ref m_showAdvancedSettings, true);
            GUILayout.Space(8);
        }

        private void DrawActionButtons(UITextGradientColor gradientColor)
        {
            EditorGUILayout.BeginVertical("HelpBox");
            {
                EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    var originalColor = GUI.color;

                    // 重置所有按钮
                    GUI.color = m_successColor;

                    if (GUILayout.Button(new GUIContent("重置所有", "重置所有设置为默认值"),
                            GUILayout.Height(30)))
                    {
                        ResetAllSettings();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                        gradientColor.gradientOffsetVertical = m_gradientOffsetVertical.floatValue;
                        gradientColor.gradientOffsetHorizontal = m_gradientOffsetHorizontal.floatValue;
                        gradientColor.splitTextGradient = m_splitTextGradient.boolValue;
                    }

                    // 应用效果按钮
                    GUI.color = m_headerColor;

                    if (GUILayout.Button(new GUIContent("应用效果", "立即刷新渐变效果"),
                            GUILayout.Height(30)))
                    {
                        RefreshGradient();
                        gradientColor.Refresh();
                    }

                    GUI.color = originalColor;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // 预设按钮
                EditorGUILayout.LabelField("渐变预设", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("蓝天白云"))
                    {
                        SetSkyGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }

                    if (GUILayout.Button("火焰效果"))
                    {
                        SetFireGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }

                    if (GUILayout.Button("霓虹灯"))
                    {
                        SetNeonGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("金属质感"))
                    {
                        SetMetalGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }

                    if (GUILayout.Button("自然渐变"))
                    {
                        SetNatureGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }

                    if (GUILayout.Button("单色渐变"))
                    {
                        SetMonoGradientPreset();
                        gradientColor.colorTop = m_colorTop.colorValue;
                        gradientColor.colorBottom = m_colorBottom.colorValue;
                        gradientColor.colorLeft = m_colorLeft.colorValue;
                        gradientColor.colorRight = m_colorRight.colorValue;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private string GetOffsetDescription(float offset)
        {
            if (offset < -0.5f) return "偏移: 强反向 (渐变起点大幅提前)";
            if (offset < 0f) return "偏移: 轻反向 (渐变起点提前)";
            if (offset == 0f) return "偏移: 居中 (标准渐变)";
            if (offset <= 0.5f) return "偏移: 轻正向 (渐变起点延后)";
            return "偏移: 强正向 (渐变起点大幅延后)";
        }

        private void SwapColors(SerializedProperty color1, SerializedProperty color2)
        {
            (color1.colorValue, color2.colorValue) = (color2.colorValue, color1.colorValue);
        }

        private void CopyToHorizontal()
        {
            m_colorLeft.colorValue = m_colorBottom.colorValue;
            m_colorRight.colorValue = m_colorTop.colorValue;
            m_gradientOffsetHorizontal.floatValue = m_gradientOffsetVertical.floatValue;
        }

        private void CopyToVertical()
        {
            m_colorTop.colorValue = m_colorLeft.colorValue;
            m_colorBottom.colorValue = m_colorRight.colorValue;
            m_gradientOffsetVertical.floatValue = m_gradientOffsetHorizontal.floatValue;
        }

        private void ResetAllSettings()
        {
            m_colorTop.colorValue = Color.white;
            m_colorBottom.colorValue = Color.white;
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
            m_gradientOffsetVertical.floatValue = 0f;
            m_gradientOffsetHorizontal.floatValue = 0f;
            m_splitTextGradient.boolValue = false;
        }

        private void RefreshGradient()
        {
            if (m_gradientColor != null && m_gradientColor._Graphic != null)
            {
                m_gradientColor._Graphic.SetVerticesDirty();
            }
        }

        // 预设方法
        private void SetSkyGradientPreset()
        {
            m_colorTop.colorValue = new Color(0.4f, 0.7f, 1.0f); // 天蓝色
            m_colorBottom.colorValue = new Color(0.8f, 0.9f, 1.0f); // 淡蓝色
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        private void SetFireGradientPreset()
        {
            m_colorTop.colorValue = Color.yellow;
            m_colorBottom.colorValue = Color.red;
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        private void SetNeonGradientPreset()
        {
            m_colorTop.colorValue = new Color(0.0f, 1.0f, 1.0f); // 青色
            m_colorBottom.colorValue = new Color(1.0f, 0.0f, 1.0f); // 洋红色
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        private void SetMetalGradientPreset()
        {
            m_colorTop.colorValue = new Color(0.8f, 0.8f, 0.9f); // 亮银色
            m_colorBottom.colorValue = new Color(0.3f, 0.3f, 0.4f); // 暗灰色
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        private void SetNatureGradientPreset()
        {
            m_colorTop.colorValue = new Color(0.2f, 0.8f, 0.3f); // 亮绿色
            m_colorBottom.colorValue = new Color(0.1f, 0.4f, 0.2f); // 深绿色
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        private void SetMonoGradientPreset()
        {
            Color baseColor = new Color(0.2f, 0.5f, 0.8f); // 基础蓝色
            m_colorTop.colorValue = baseColor;
            m_colorBottom.colorValue = baseColor * 0.6f; // 暗化
            m_colorLeft.colorValue = Color.white;
            m_colorRight.colorValue = Color.white;
        }

        // 预览纹理相关方法
        private void CreatePreviewTextures()
        {
            m_verticalPreview = new Texture2D(PREVIEW_SIZE, PREVIEW_SIZE);
            m_horizontalPreview = new Texture2D(PREVIEW_SIZE, PREVIEW_SIZE);
            m_combinedPreview = new Texture2D(PREVIEW_SIZE, PREVIEW_SIZE);
        }

        private void DestroyPreviewTextures()
        {
            if (m_verticalPreview != null) GameObject.DestroyImmediate(m_verticalPreview);
            if (m_horizontalPreview != null) GameObject.DestroyImmediate(m_horizontalPreview);
            if (m_combinedPreview != null) GameObject.DestroyImmediate(m_combinedPreview);
        }

        private void UpdatePreviewTextures()
        {
            UpdateVerticalPreview();
            UpdateHorizontalPreview();
            UpdateCombinedPreview();
        }

        private void UpdateVerticalPreview()
        {
            for (int y = 0; y < PREVIEW_SIZE; y++)
            {
                float t = (float)y / (PREVIEW_SIZE - 1);
                t = Mathf.Clamp01(t + m_gradientOffsetVertical.floatValue);
                Color color = Color.Lerp(m_colorBottom.colorValue, m_colorTop.colorValue, t);

                for (int x = 0; x < PREVIEW_SIZE; x++)
                {
                    m_verticalPreview.SetPixel(x, y, color);
                }
            }

            m_verticalPreview.Apply();
        }

        private void UpdateHorizontalPreview()
        {
            for (int x = 0; x < PREVIEW_SIZE; x++)
            {
                float t = (float)x / (PREVIEW_SIZE - 1);
                t = Mathf.Clamp01(t + m_gradientOffsetHorizontal.floatValue);
                Color color = Color.Lerp(m_colorLeft.colorValue, m_colorRight.colorValue, t);

                for (int y = 0; y < PREVIEW_SIZE; y++)
                {
                    m_horizontalPreview.SetPixel(x, y, color);
                }
            }

            m_horizontalPreview.Apply();
        }

        private void UpdateCombinedPreview()
        {
            for (int y = 0; y < PREVIEW_SIZE; y++)
            {
                float verticalT = (float)y / (PREVIEW_SIZE - 1);
                verticalT = Mathf.Clamp01(verticalT + m_gradientOffsetVertical.floatValue);
                Color verticalColor = Color.Lerp(m_colorBottom.colorValue, m_colorTop.colorValue, verticalT);

                for (int x = 0; x < PREVIEW_SIZE; x++)
                {
                    float horizontalT = (float)x / (PREVIEW_SIZE - 1);
                    horizontalT = Mathf.Clamp01(horizontalT + m_gradientOffsetHorizontal.floatValue);
                    Color horizontalColor = Color.Lerp(m_colorLeft.colorValue, m_colorRight.colorValue, horizontalT);

                    Color combinedColor = verticalColor * horizontalColor;
                    m_combinedPreview.SetPixel(x, y, combinedColor);
                }
            }

            m_combinedPreview.Apply();
        }
    }
}

#endif