using UnityEditor;
using UnityEngine;

namespace DGame
{
    public partial class UIScriptGenerator
    {
        public class GenerateUIComponentWindow : EditorWindow
        {
            private string m_csName;
            private string m_savePath;
            private bool m_isGenerateUIComponent;
            private bool m_isUniTask;

            // UI状态
            private Vector2 m_scrollPosition;
            private bool m_showAdvancedSettings = true;

            // 样式和颜色
            private GUIStyle m_headerStyle;
            private GUIStyle m_subHeaderStyle;
            private GUIStyle m_buttonStyle;
            private GUIStyle m_pathLabelStyle;
            private Color m_successColor = new Color(0.2f, 0.8f, 0.3f, 1f);
            private Color m_warningColor = new Color(1f, 0.6f, 0.2f, 1f);

            private static int MIN_WIDTH = 850;
            private static int MIN_HEIGHT = 515;
            private static Rect mainWindowPosition => EditorGUIUtility.GetMainWindowPosition();

            [MenuItem("GameObject/ScriptGenerator/绑定UI组件", priority = 81)]
            public static void GenerateUIComponent()
            {
                var window = CreateWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("生成UI组件");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = true;
                CenterWindow(window);
            }

            [MenuItem("GameObject/ScriptGenerator/绑定UI组件", true)]
            public static bool ValidateGenerateUIComponent()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            [MenuItem("GameObject/ScriptGenerator/生成窗口脚本", priority = 82)]
            public static void GenerateUIPropertyBindComponent()
            {
                var window = CreateWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("生成UI窗口");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = false;
                window.m_isUniTask = false;
                CenterWindow(window);
            }

            [MenuItem("GameObject/ScriptGenerator/生成窗口脚本", true)]
            public static bool ValidateGenerateUIPropertyBindComponent()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            [MenuItem("GameObject/ScriptGenerator/生成窗口脚本 (UniTask)", priority = 83)]
            public static void GenerateUIPropertyBindComponentUniTask()
            {
                var window = CreateWindow<GenerateUIComponentWindow>();
                window.titleContent = new GUIContent("生成UI窗口");
                window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
                window.m_isGenerateUIComponent = false;
                window.m_isUniTask = true;
                CenterWindow(window);
            }

            [MenuItem("GameObject/ScriptGenerator/生成窗口脚本 (UniTask)", true)]
            public static bool ValidateGenerateUIPropertyBindComponentUniTask()
            {
                return UIScriptGeneratorSettings.Instance.UseBindComponent;
            }

            private static void CenterWindow(EditorWindow window)
            {
                var center = new Vector2(
                    mainWindowPosition.x + mainWindowPosition.width / 2,
                    mainWindowPosition.y + mainWindowPosition.height / 2
                );
                window.position = new Rect(center.x - MIN_WIDTH / 2, center.y - MIN_HEIGHT / 2, MIN_WIDTH, MIN_HEIGHT);
            }

            private void OnEnable()
            {
                m_savePath = UIScriptGeneratorSettings.GetCodePath();
            }

            private void OnGUI()
            {
                m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);
                {
                    DrawHeader();
                    DrawConfigurationSection();
                    DrawActionButtons();
                    DrawAdvancedSettings();
                }
                EditorGUILayout.EndScrollView();
            }

            private void DrawHeader()
            {
                GUILayout.Space(5);

                // 主标题
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                string windowTitle = m_isGenerateUIComponent ? "生成UI组件脚本" :
                    m_isUniTask ? "生成UI窗口脚本 (UniTask)" : "生成UI窗口脚本";
                m_headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                EditorGUILayout.LabelField(new GUIContent(windowTitle, "自动生成UI组件绑定代码"),
                    m_headerStyle, GUILayout.Height(30));

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                // 副标题
                var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f, 1f) }
                };

                string subtitle = m_isGenerateUIComponent ? "为选中的Window生成组件绑定代码" : "为UI窗口生成完整的脚本框架";

                EditorGUILayout.LabelField(subtitle, subtitleStyle);
                GUILayout.Space(5);

                // 分隔线
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                GUILayout.Space(10);
            }

            private void DrawConfigurationSection()
            {
                var root = Selection.activeTransform;

                if (root != null)
                {
                    // CheckVariableNames();
                    var windowComSufName = UIScriptGeneratorSettings.Instance.WindowComponentSuffixName;
                    var widgetComSufName = UIScriptGeneratorSettings.Instance.WidgetComponentSuffixName;
                    var widgetPrefix = GetUIWidgetName();
                    var rootName = m_isGenerateUIComponent ? $"{root.name}{windowComSufName}" : root.name;

                    if (root.name.StartsWith(widgetPrefix))
                    {
                        rootName = m_isGenerateUIComponent
                            ? $"{root.name.Replace(GetUIWidgetName(), string.Empty)}{widgetComSufName}"
                            : $"{root.name.Replace(GetUIWidgetName(), string.Empty)}";
                    }
                    m_csName = $"{rootName}.cs";
                }
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    m_subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                    };
                    EditorGUILayout.LabelField("生成配置", m_subHeaderStyle);

                    // 当前路径显示
                    EditorGUILayout.BeginVertical("TextArea");
                    {
                        m_pathLabelStyle = new GUIStyle(EditorStyles.label)
                        {
                            wordWrap = true,
                            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
                        };
                        Texture2D texture = new Texture2D(1, 2);
                        texture.SetPixel(0, 0, new Color(0.3f, 0.5f, 0.9f, 0.4f));
                        texture.SetPixel(0, 1, new Color(0.2f, 0.4f, 0.8f, 0.4f));
                        texture.Apply();

                        m_pathLabelStyle.normal.background = texture;
                        m_pathLabelStyle.padding = new RectOffset(8, 8, 6, 6);
                        m_pathLabelStyle.margin = new RectOffset(0, 0, 3, 3);
                        m_pathLabelStyle.fontStyle = FontStyle.Bold;
                        EditorGUILayout.LabelField("当前生成路径:", EditorStyles.miniBoldLabel);
                        string displayPath = !string.IsNullOrEmpty(m_savePath)
                            ? m_savePath + "/" + m_csName
                            : UIScriptGeneratorSettings.Instance.CodePath + "/" + m_csName;
                        EditorGUILayout.LabelField(displayPath, m_pathLabelStyle);
                    }
                    EditorGUILayout.EndVertical();

                    GUILayout.Space(10);

                    // 路径选择
                    m_savePath = DrawEnhancedFolderField("生成脚本目录", "选择脚本生成的目标目录", m_savePath);
                    m_savePath = string.IsNullOrEmpty(m_savePath)
                        ? UIScriptGeneratorSettings.Instance.CodePath
                        : m_savePath;

                    GUILayout.Space(5);

                    // 路径验证
                    if (!string.IsNullOrEmpty(m_savePath) && !m_savePath.StartsWith("Assets/"))
                    {
                        EditorGUILayout.HelpBox("路径应该以 Assets/ 开头", MessageType.Warning);
                    }

                    // 生成类型说明
                    EditorGUILayout.BeginVertical("Box");
                    {
                        EditorGUILayout.LabelField("生成类型", EditorStyles.miniBoldLabel);
                        if (m_isGenerateUIComponent)
                        {
                            EditorGUILayout.HelpBox("将生成UI组件绑定脚本，包含选中UI元素的引用绑定 =====> 确认蓝色框内的路径", MessageType.Info);
                        }
                        else
                        {
                            string taskType = m_isUniTask ? "UniTask异步" : "标准";
                            EditorGUILayout.HelpBox($"将生成完整的UI窗口脚本，使用{taskType}实现方式 =====> 确认蓝色框内的路径", MessageType.Info);
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
                GUILayout.Space(10);
            }

            private void DrawActionButtons()
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    m_subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                    };
                    EditorGUILayout.LabelField("生成操作", m_subHeaderStyle);
                    bool canGenerate = false;
                    EditorGUILayout.BeginHorizontal();
                    {
                        m_buttonStyle = new GUIStyle(GUI.skin.button)
                        {
                            padding = new RectOffset(15, 15, 8, 8),
                            margin = new RectOffset(5, 5, 5, 5),
                            fontSize = 12
                        };
                        if (GUILayout.Button(new GUIContent("取消", "关闭窗口不进行任何操作"),
                                m_buttonStyle, GUILayout.Height(35)))
                        {
                            Close();
                        }
                        var originalColor = GUI.color;
                        canGenerate = !string.IsNullOrEmpty(m_savePath);

                        if (canGenerate)
                        {
                            GUI.color = m_successColor;
                        }
                        else
                        {
                            GUI.color = m_warningColor;
                        }

                        string generateButtonText = m_isGenerateUIComponent ? "生成组件脚本" :
                            m_isUniTask ? "生成窗口脚本 UniTask" : "生成窗口脚本";

                        if (GUILayout.Button(new GUIContent(generateButtonText, "开始生成脚本文件"),
                                m_buttonStyle, GUILayout.Height(35)))
                        {
                            GenerateScripts();
                        }

                        GUI.color = originalColor;
                    }
                    EditorGUILayout.EndHorizontal();

                    if (!canGenerate)
                    {
                        EditorGUILayout.HelpBox("请先选择有效的生成目录", MessageType.Warning);
                    }
                }
                EditorGUILayout.EndVertical();
            }

            private void DrawAdvancedSettings()
            {
                m_showAdvancedSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_showAdvancedSettings,
                    new GUIContent("高级设置", "高级生成选项"));

                if (m_showAdvancedSettings)
                {
                    EditorGUILayout.BeginVertical("HelpBox");
                    {
                        // 生成类型信息
                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("生成模式:", GUILayout.Width(80));
                            string modeName = m_isGenerateUIComponent ? "组件绑定" : "窗口脚本";
                            EditorGUILayout.LabelField(modeName, EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("任务类型:", GUILayout.Width(80));
                            string taskType = m_isUniTask ? "UniTask" : "标准";
                            EditorGUILayout.LabelField(taskType, EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.LabelField("脚本目录:", GUILayout.Width(80));
                            EditorGUILayout.LabelField(m_savePath + "/" + m_csName, EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(5);

                        // 快速操作
                        EditorGUILayout.LabelField("快速操作", EditorStyles.miniBoldLabel);
                        EditorGUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button("打开设置", GUILayout.Height(25)))
                            {
                                SettingsService.OpenProjectSettings("Project/DGame/UI代码生成器设置");
                            }

                            if (GUILayout.Button("验证路径", GUILayout.Height(25)))
                            {
                                ValidateSavePath();
                            }

                            if (GUILayout.Button("重置路径", GUILayout.Height(25)))
                            {
                                ResetSavePath();
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            private string DrawEnhancedFolderField(string label, string tooltip, string path)
            {
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        // 标签
                        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(120));

                        // 路径字段
                        path = EditorGUILayout.TextField(path, GUILayout.ExpandWidth(true));

                        // 选择按钮
                        if (GUILayout.Button(new GUIContent(" 选择", EditorGUIUtility.IconContent("Folder Icon").image), GUILayout.Width(60), GUILayout.Height(18)))
                        {
                            string newPath = EditorUtility.OpenFolderPanel(label,
                                string.IsNullOrEmpty(path) ? Application.dataPath : path,
                                string.Empty);

                            if (!string.IsNullOrEmpty(newPath))
                            {
                                newPath = newPath.Replace(Application.dataPath, "Assets");

                                if (newPath.StartsWith(UIScriptGeneratorSettings.GetCodePath()))
                                {
                                    path = newPath;
                                }
                                else
                                {
                                    Debug.LogError($"路径必须在代码生成目录内: {UIScriptGeneratorSettings.GetCodePath()}");
                                    EditorUtility.DisplayDialog("路径错误",
                                        $"选择的路径必须在代码生成目录内:\n{UIScriptGeneratorSettings.GetCodePath()}",
                                        "确定");
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    // 路径提示
                    EditorGUILayout.LabelField("路径应该位于代码生成设置的基础目录内", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndVertical();

                return path;
            }

            private void GenerateScripts()
            {
                if (string.IsNullOrEmpty(m_savePath))
                {
                    EditorUtility.DisplayDialog("生成错误", "请先选择有效的生成目录", "确定");
                    return;
                }

                bool success = false;
                string message = "";

                if (m_isGenerateUIComponent)
                {
                    success = GenerateUIComponentScript(m_savePath);
                    message = success ? "UI组件脚本生成成功！" : "UI组件脚本生成失败";
                }
                else
                {
                    success = GenerateCSharpScript(true, m_isUniTask, true, m_savePath);
                    message = $"UI窗口脚本生成成功！({(m_isUniTask ? "UniTask" : "标准")}版本)";
                }

                if (success)
                {
                    // 显示成功对话框
                    if (EditorUtility.DisplayDialog("生成成功", message, "打开目录", "确定"))
                    {
                        // 在Project窗口中高亮显示生成的目录
                        Object obj = AssetDatabase.LoadAssetAtPath<Object>(m_savePath);

                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                        }
                    }

                    Close();
                }
                else
                {
                    EditorUtility.DisplayDialog("生成失败", message, "确定");
                }
            }

            private void ValidateSavePath()
            {
                if (string.IsNullOrEmpty(m_savePath))
                {
                    EditorUtility.DisplayDialog("验证结果", "路径为空", "确定");
                    return;
                }

                if (!m_savePath.StartsWith(UIScriptGeneratorSettings.GetCodePath()))
                {
                    EditorUtility.DisplayDialog("验证结果",
                        $"路径不在代码生成目录内\n当前路径: {m_savePath}\n基础目录: {UIScriptGeneratorSettings.GetCodePath()}",
                        "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("验证结果", "路径有效", "确定");
                }
            }

            private void ResetSavePath()
            {
                m_savePath = UIScriptGeneratorSettings.GetCodePath();
                Debug.Log("已重置生成路径为默认设置");
            }

            private bool GenerateUIComponentScript(string savePath)
            {
                var succ = UIScriptGenerator.GenerateUIComponentScript(savePath);
                if(succ) Debug.Log($"生成UI组件脚本到: {savePath}");
                return succ;
            }

            private bool GenerateCSharpScript(bool includeProperties, bool useUniTask, bool includeEvents,
                string savePath)
            {
                var succ = UIScriptGenerator.GenerateCSharpScript(includeProperties, useUniTask, includeEvents, savePath);
                if(succ) Debug.Log($"生成C#脚本到: {savePath} (UniTask: {useUniTask})");
                return succ;
            }
        }
    }
}