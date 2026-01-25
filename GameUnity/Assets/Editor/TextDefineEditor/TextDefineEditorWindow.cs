using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 文本配置编辑器窗口 - 提供文本提取、生成、绑定等功能
    /// </summary>
    public class TextDefineEditorWindow : EditorWindow
    {
        // 视图状态
        private enum ToolTab
        {
            CodeExtract,    // 代码文本提取
            PrefabExtract,  // 预制体提取
        }

        private const string m_title = "文本处理工具";
        private const string m_menuPath = "DGame Tools/文本处理工具";

        private static readonly Color m_headerColor = new(0.4f, 0.6f, 0.8f);

        // 路径配置
        private const string m_defaultScriptPath = "Assets/Scripts/HotFix/GameLogic";
        private const string m_defaultPrefabPath = "Assets/BundleAssets/UI";
        private const string m_textDefinePath = "Assets/Scripts/HotFix/GameLogic/Text/TextDefine.cs";
        private const string m_outputPath = "Assets/Editor/TextDefineEditor/Output/";
        private ToolTab m_currentTab = ToolTab.CodeExtract;

        // 代码提取选项
        private string m_scriptFolderPath = m_defaultScriptPath;
        private int m_startTextDefineId;
        private string m_tagPrefix = string.Empty;
        private bool m_useExistingText;
        private static int m_maxExistingId = 20000000;

        // 预制体提取选项
        private string m_prefabFolderPath = m_defaultPrefabPath;
        private int m_prefabStartId;
        private bool m_includeTextMeshPro = true;
        private bool m_overwriteExistingBinders = false;

        // 结果显示
        private Vector2 m_scrollPosition;

        // 缓存数据
        private static Dictionary<int, string> m_existingTextConfigs = new();

        [MenuItem(m_menuPath)]
        public static void ShowWindow()
        {
            var window = GetWindow<TextDefineEditorWindow>(m_title);
            window.minSize = new Vector2(600, 500);
            window.RefreshExistingData();
        }

        private void RefreshExistingData()
        {
            // m_existingTextConfigs = TextProcessorTool.LoadExistingTextConfigs();
            // m_maxExistingId = m_existingTextConfigs.Count > 0 ? m_existingTextConfigs.Keys.Max() : 0;
            var tempID = TextProcessorTool.GetLastTextDefineID(m_textDefinePath);
            m_maxExistingId = tempID > 0 ? tempID : m_maxExistingId;
            m_startTextDefineId = m_maxExistingId + 1;
            m_prefabStartId = m_maxExistingId + 1;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTabButtons();

            // 主内容区域 - 使用灵活高度
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));
            {
                switch (m_currentTab)
                {
                    case ToolTab.CodeExtract:
                        DrawCodeExtractTab();
                        break;
                    case ToolTab.PrefabExtract:
                        DrawPrefabExtractTab();
                        break;
                }
            }
            EditorGUILayout.EndScrollView();
        }

        #region UI绘制 - 头部和标签页

        private void DrawHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 50);
            var headerRect = new Rect(rect.x + 5, rect.y + 5, rect.width - 10, 40);
            EditorGUI.DrawRect(headerRect, m_headerColor);

            GUI.Label(headerRect, m_title, new GUIStyle
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            });

            EditorGUILayout.Space(5);
        }

        private void DrawTabButtons()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var tabs = Enum.GetValues(typeof(ToolTab)).Cast<ToolTab>().ToArray();
                for (int i = 0; i < tabs.Length; i++)
                {
                    var tab = tabs[i];
                    var tabName = GetTabDisplayName(tab);
                    var isSelected = m_currentTab == tab;

                    if (GUILayout.Toggle(isSelected, tabName, EditorStyles.toolbarButton, GUILayout.Width(120)))
                    {
                        if (!isSelected)
                        {
                            m_currentTab = tab;
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                // 刷新按钮
                if (GUILayout.Button("刷新数据", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    RefreshExistingData();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static string GetTabDisplayName(ToolTab tab) => tab switch
        {
            ToolTab.CodeExtract => "文本处理",
            ToolTab.PrefabExtract => "UITextIDBinder处理",
            _ => tab.ToString()
        };

        #endregion

        #region UI绘制 - 代码提取

        private void DrawCodeExtractTab()
        {
            DrawSectionHeader("从代码中提取文本并生成 TextDefine", "扫描C#代码中的 G.R(\"中文\") 模式，自动替换为 G.R(TextDefine.xxx) 枚举并生成配置表。");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // 文件夹选择
                DrawFolderSelector(ref m_scriptFolderPath, "代码文件夹", m_defaultScriptPath);

                EditorGUILayout.Space(5);

                // 起始ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("起始 ID", GUILayout.Width(100));
                m_startTextDefineId = EditorGUILayout.IntField(m_startTextDefineId);
                EditorGUILayout.EndHorizontal();

                // 标签前缀
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("标签前缀", GUILayout.Width(100));
                m_tagPrefix = EditorGUILayout.TextField(m_tagPrefix);
                EditorGUILayout.EndHorizontal();

                // 选项
                m_useExistingText = EditorGUILayout.ToggleLeft("使用现有文本配置（不重复创建）", m_useExistingText);

                EditorGUILayout.Space(10);

                // 统计信息
                DrawInfoBox($"当前最大ID: {m_maxExistingId}");

                EditorGUILayout.Space(10);

                // 执行按钮
                if (GUILayout.Button("开始提取并生成", GUILayout.Height(35)))
                {
                    if (m_startTextDefineId != 0)
                    {
                        if (!Directory.Exists(m_outputPath))
                        {
                            Directory.CreateDirectory(m_outputPath);
                        }
                        var outPath = Path.Combine(m_outputPath, $"CodeText_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                        var options = new GRCodeExtractOptions()
                        {
                            ScriptFolderPath = m_scriptFolderPath,
                            StartId = m_startTextDefineId,
                            Tag = m_tagPrefix,
                            UseExistingText = m_useExistingText,
                            TextDefinePath = m_textDefinePath,
                            OutputPath = outPath
                        };
                        TextProcessorTool.ExtractGRFromCode(options);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            DrawUsageTips("提示：此工具会扫描代码中的 G.R(\"中文\") 模式，自动替换为 G.R(TextDefine.xxx) 并更新枚举文件。");
        }

        #endregion

        #region UI绘制 - 预制体提取

        private void DrawPrefabExtractTab()
        {
            DrawSectionHeader("从预制体提取文本", "扫描UI预制体中的文本组件，提取内容并自动绑定UITextIDBinder。");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                DrawFolderSelector(ref m_prefabFolderPath, "预制体文件夹", m_defaultPrefabPath);

                EditorGUILayout.Space(5);

                // 起始ID
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("起始 ID", GUILayout.Width(100));
                m_prefabStartId = EditorGUILayout.IntField(m_prefabStartId);
                EditorGUILayout.EndHorizontal();

                // 选项
                m_includeTextMeshPro = EditorGUILayout.ToggleLeft("包含 TextMeshPro 组件", m_includeTextMeshPro);
                m_overwriteExistingBinders = EditorGUILayout.ToggleLeft("覆盖现有的 TextIDBinder", m_overwriteExistingBinders);

                EditorGUILayout.Space(10);

                if (GUILayout.Button("提取预制体文本", GUILayout.Height(35)))
                {
                    // ExecutePrefabExtract();
                }
            }
            EditorGUILayout.EndVertical();

            DrawUsageTips("提示：提取后的文本会自动添加 UITextIDBinder 组件，并生成配置文件。");
        }

        #endregion

        #region 辅助UI方法

        private void DrawSectionHeader(string title, string subtitle)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, EditorStyles.miniLabel);
            }
        }

        private void DrawFolderSelector(ref string path, string label, string defaultPath)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("选择文件夹", GUILayout.Width(100)))
            {
                var selected = EditorUtility.OpenFolderPanel(label, path, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    path = PathHelper.MakeRelativePath(selected);
                }
            }
            EditorGUILayout.LabelField($"{label}: {path}");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button($"重置为默认 ({defaultPath})", GUILayout.Height(20)))
            {
                path = defaultPath;
            }
        }

        private void DrawInfoBox(string info1) // , string info2
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(info1, MessageType.Info);
            // EditorGUILayout.HelpBox(info2, MessageType.Info);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawUsageTips(string tips)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(tips, MessageType.None);
        }

        #endregion
    }
}