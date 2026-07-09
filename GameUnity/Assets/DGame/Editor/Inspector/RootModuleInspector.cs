using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(RootModule))]
    internal sealed class RootModuleInspector : DGameInspector
    {
        private const string NONE_OPTION_NAME = "<None>";
        private const string GLOBAL_HELPER_PANEL_KEY = "RootModule.GlobalHelperPanelOpen";
        private const string MEMORY_PANEL_KEY = "RootModule.MemoryPanelOpen";
        private const string PERFORMANCE_PANEL_KEY = "RootModule.PerformancePanelOpen";
        private const string SYSTEM_PANEL_KEY = "RootModule.SystemPanelOpen";
        private const string OVERVIEW_PANEL_KEY = "RootModule.OverviewPanelOpen";
        private const float GAME_SPEED_LABEL_WIDTH = 70f;
        private const float GAME_SPEED_BUTTON_MIN_WIDTH = 48f;
        private const float GAME_SPEED_ROW_GAP = 4f;

        private static readonly float[] m_gameSpeedArr = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private static readonly string[] m_gameSpeedForDisplay = new string[] { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private static readonly ScreenOrientation[] m_screenOrientationArr = new ScreenOrientation[]
        {
            ScreenOrientation.Portrait,
            ScreenOrientation.PortraitUpsideDown,
            ScreenOrientation.LandscapeLeft,
            ScreenOrientation.LandscapeRight,
            ScreenOrientation.AutoRotation,
        };
        private static readonly string[] m_screenOrientationForDisplay = new string[]
        {
            "竖屏", "竖屏（倒置）", "横屏（左）", "横屏（右）", "自动旋转",
        };

        private SerializedProperty m_stringUtilHelperTypeName = null;
        private SerializedProperty m_logHelperTypeName = null;
        private SerializedProperty m_jsonHelperTypeName = null;
        private SerializedProperty m_gameSpeed = null;
        private SerializedProperty m_frameRate = null;
        private SerializedProperty m_runInBackground = null;
        private SerializedProperty m_neverSleep = null;
        private SerializedProperty m_screenOrientation = null;
        private SerializedProperty m_memoryStrictCheckType = null;
        private SerializedProperty m_editorLanguage = null;

        private string[] m_stringUtilHelperTypeNames = null;
        private int m_stringUtilHelperTypeNameIndex = 0;

        private string[] m_logHelperTypeNames = null;
        private int m_logHelperTypeNameIndex = 0;

        private string[] m_jsonHelperTypeNames = null;
        private int m_jsonHelperTypeNameIndex = 0;

        // UI状态
        private Vector2 m_scrollPosition;
        private bool m_showGlobalHelperSetting = true;
        private bool m_showMemorySetting = true;
        private bool m_showPerformanceSetting = true;
        private bool m_showSystemSetting = true;
        private bool m_showOverview = true;

        private string[] m_memoryStrictCheckTypeNames = null;
        private int m_memoryStrictCheckTypeIndex = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            RootModule rootModule = (RootModule)target;

            // 绘制标题区域
            DrawInspectorHeader();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                // DrawEditorLanguageSettings(rootModule);
                DrawGlobalHelperSettings(rootModule);
                DrawMemoryPoolSettings(rootModule);
                DrawPerformanceSettings(rootModule);
                DrawSystemSettings(rootModule);
                DrawStatistics(rootModule);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                SavePanelStates();
            }
        }

        private void DrawEditorLanguageSettings(RootModule rootModule)
        {
            // EditorGUILayout.BeginVertical("HelpBox");
            // {
            //     EditorGUILayout.LabelField("编辑器模式语言设置");
            //     EditorGUILayout.BeginHorizontal();
            //     EditorGUILayout.LabelField(new GUIContent("Editor Language"), GUILayout.Width(120));
            //     EditorGUILayout.PropertyField(m_editorLanguage, GUIContent.none);
            //
            //     EditorGUILayout.EndHorizontal();
            //
            //     EditorGUILayout.Space(3);
            //
            //     string helperStatus = "编辑器模式下运行的语言类型: " + LocalizationUtil.GetLanguage(rootModule.EditorLanguage);
            //     EditorGUILayout.HelpBox(helperStatus, MessageType.Info);
            // }
            // EditorGUILayout.EndVertical();
        }

        private void DrawMemoryPoolSettings(RootModule rootModule)
        {
            UnityEditorUtil.LayoutFoldoutBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    int memoryStrictCheckTypeIndex = EditorGUILayout.Popup("内存池强制检查开启模式", m_memoryStrictCheckTypeIndex, m_memoryStrictCheckTypeNames);
                    if (memoryStrictCheckTypeIndex != m_memoryStrictCheckTypeIndex)
                    {
                        m_memoryStrictCheckTypeIndex = memoryStrictCheckTypeIndex;
                        m_memoryStrictCheckType.enumValueIndex = memoryStrictCheckTypeIndex <= 0 ? 0 : memoryStrictCheckTypeIndex;
                    }

                    EditorGUILayout.Space(3);

                    string helperStatus = GetMemorySettingCheckStatus((MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex);
                    EditorGUILayout.HelpBox(helperStatus,
                        IsMemorySettingWarning() ? MessageType.Warning : MessageType.Info);
                }
                EditorGUILayout.EndVertical();
            }, "内存池设置", ref m_showMemorySetting, true);

            EditorGUILayout.Space(5);
        }

        private void DrawInspectorHeader()
        {
            GUILayout.Space(5);

            var titleStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            var icon = EditorGUIUtility.IconContent("GameManager Icon").image;
            EditorGUILayout.LabelField(new GUIContent(" 游戏根模块配置", icon, "Root Module Configuration"),
                titleStyle);

            var descStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("配置游戏核心系统和辅助器", descStyle);
            EditorGUILayout.Space(6);
        }

        private void DrawGlobalHelperSettings(RootModule rootModule)
        {
            UnityEditorUtil.LayoutFoldoutBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    DrawHelperPopup(new GUIContent("字符串辅助器", "字符串处理工具"),
                        ref m_stringUtilHelperTypeNameIndex, m_stringUtilHelperTypeNames, m_stringUtilHelperTypeName);
                    DrawHelperPopup(new GUIContent("日志辅助器", "日志输出工具"),
                        ref m_logHelperTypeNameIndex, m_logHelperTypeNames, m_logHelperTypeName);
                    DrawHelperPopup(new GUIContent("JSON辅助器", "JSON序列化工具"),
                        ref m_jsonHelperTypeNameIndex, m_jsonHelperTypeNames, m_jsonHelperTypeName);

                    EditorGUILayout.Space(3);

                    string helperStatus = GetHelperStatus();
                    EditorGUILayout.HelpBox(helperStatus,
                        IsAllHelpersConfigured() ? MessageType.Info : MessageType.Warning);
                }
                EditorGUILayout.EndVertical();
            }, "全局辅助器设置", ref m_showGlobalHelperSetting, true);

            EditorGUILayout.Space(5);
        }

        private void DrawPerformanceSettings(RootModule rootModule)
        {
            UnityEditorUtil.LayoutFoldoutBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    int frameRate = EditorGUILayout.IntSlider(new GUIContent("游戏帧率", "目标帧率设置"), m_frameRate.intValue, 1, 120);

                    if (frameRate != m_frameRate.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.FrameRate = frameRate;
                        }
                        else
                        {
                            m_frameRate.intValue = frameRate;
                        }
                    }

                    string frameRateAdvice = GetFrameRateAdvice(frameRate);
                    EditorGUILayout.HelpBox(frameRateAdvice, MessageType.Info);

                    EditorGUILayout.Space(5);

                    float gameSpeed = EditorGUILayout.Slider("速度倍率", m_gameSpeed.floatValue, 0f, 8f);
                    gameSpeed = DrawGameSpeedQuickSettings(gameSpeed);

                    if (Mathf.Abs(gameSpeed - m_gameSpeed.floatValue) > 0.001f)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.GameSpeed = gameSpeed;
                        }
                        else
                        {
                            m_gameSpeed.floatValue = gameSpeed;
                        }
                    }

                    if (Mathf.Approximately(gameSpeed, 0f))
                    {
                        EditorGUILayout.HelpBox("游戏暂停", MessageType.Warning);
                    }
                    else if (gameSpeed > 1f)
                    {
                        EditorGUILayout.HelpBox($"加速模式: {gameSpeed}x", MessageType.Info);
                    }
                }
                EditorGUILayout.EndVertical();
            }, "性能设置", ref m_showPerformanceSetting, true);

            EditorGUILayout.Space(5);
        }

        private void DrawSystemSettings(RootModule rootModule)
        {
            UnityEditorUtil.LayoutFoldoutBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    bool runInBackground = EditorGUILayout.ToggleLeft(
                        new GUIContent("可在后台运行", "游戏窗口失去焦点时继续运行"),
                        m_runInBackground.boolValue);

                    if (runInBackground != m_runInBackground.boolValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.RunInBackground = runInBackground;
                        }
                        else
                        {
                            m_runInBackground.boolValue = runInBackground;
                        }
                    }

                    bool neverSleep = EditorGUILayout.ToggleLeft(
                        new GUIContent("从不休眠", "防止系统进入睡眠模式"),
                        m_neverSleep.boolValue);

                    if (neverSleep != m_neverSleep.boolValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.NeverSleep = neverSleep;
                        }
                        else
                        {
                            m_neverSleep.boolValue = neverSleep;
                        }
                    }

                    int orientationIndex = GetScreenOrientationIndex((ScreenOrientation)m_screenOrientation.enumValueIndex);
                    int newOrientationIndex = EditorGUILayout.Popup(
                        new GUIContent("屏幕朝向", "游戏运行时锁定的屏幕朝向（横竖屏）"),
                        orientationIndex, m_screenOrientationForDisplay);

                    if (newOrientationIndex != orientationIndex)
                    {
                        ScreenOrientation screenOrientation = m_screenOrientationArr[newOrientationIndex];
                        if (EditorApplication.isPlaying)
                        {
                            rootModule.ScreenOrientation = screenOrientation;
                        }
                        else
                        {
                            m_screenOrientation.enumValueIndex = (int)screenOrientation;
                        }
                    }

                    EditorGUILayout.Space(3);

                    string systemStatus = GetSystemStatus(runInBackground, neverSleep);
                    MessageType messageType = (runInBackground || neverSleep) ? MessageType.Info : MessageType.None;
                    EditorGUILayout.HelpBox(systemStatus, messageType);
                }
                EditorGUILayout.EndVertical();
            }, "系统设置", ref m_showSystemSetting, true);

            EditorGUILayout.Space(5);
        }

        private void DrawStatistics(RootModule rootModule)
        {
            UnityEditorUtil.LayoutFoldoutBox(() =>
            {
                EditorGUILayout.BeginVertical("HelpBox");
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("辅助器配置", GUILayout.Width(120));
                        string helperStatus = IsAllHelpersConfigured() ? "完整" : "不完整";
                        EditorGUILayout.LabelField(helperStatus, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("当前帧率", GUILayout.Width(120));
                        EditorGUILayout.LabelField($"{m_frameRate.intValue} FPS", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("游戏速度", GUILayout.Width(120));
                        EditorGUILayout.LabelField($"{m_gameSpeed.floatValue}x", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("系统状态", GUILayout.Width(120));
                        string systemStatus = GetSystemStatusSummary();
                        EditorGUILayout.LabelField(systemStatus, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.LabelField("内存池检查", GUILayout.Width(120));
                        string memoryStatus = GetMemorySettingStatusSummary();
                        EditorGUILayout.LabelField(memoryStatus, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("刷新类型", GUILayout.Height(24)))
                        {
                            RefreshTypeNames();
                        }

                        if (GUILayout.Button("保存配置", GUILayout.Height(24)))
                        {
                            serializedObject.ApplyModifiedProperties();
                            Debug.Log("根模块配置已保存");
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }, "配置概览", ref m_showOverview, true);
        }

        private void DrawHelperPopup(GUIContent label, ref int selectedIndex, string[] typeNames, SerializedProperty property)
        {
            if (typeNames == null || typeNames.Length <= 0 || property == null)
            {
                EditorGUILayout.HelpBox($"{label.text} 类型列表未初始化。", MessageType.Warning);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(90));
            int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, typeNames);
            EditorGUILayout.EndHorizontal();

            if (newSelectedIndex == selectedIndex)
            {
                return;
            }

            selectedIndex = newSelectedIndex;
            property.stringValue = selectedIndex <= 0 ? null : typeNames[selectedIndex];
        }

        private float DrawGameSpeedQuickSettings(float gameSpeed)
        {
            int selectedGameSpeed = DrawGameSpeedToolbar(GetSelectedGameSpeed(gameSpeed));

            if (selectedGameSpeed >= 0)
            {
                gameSpeed = GetGameSpeed(selectedGameSpeed);
            }

            return gameSpeed;
        }

        private int DrawGameSpeedToolbar(int currentSelected)
        {
            int countPerRow = GetGameSpeedButtonCountPerRow();
            int selectedIndex = currentSelected;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("快速设置", "选择常用游戏速度倍率"), GUILayout.Width(GAME_SPEED_LABEL_WIDTH));
            for (int i = 0; i < m_gameSpeedForDisplay.Length; i++)
            {
                if (i > 0 && i % countPerRow == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(GAME_SPEED_LABEL_WIDTH + GAME_SPEED_ROW_GAP);
                }

                selectedIndex = DrawGameSpeedToggle(selectedIndex, i);
            }
            EditorGUILayout.EndHorizontal();

            return selectedIndex;
        }

        private int GetGameSpeedButtonCountPerRow()
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - GAME_SPEED_LABEL_WIDTH - 45f;
            int countPerRow = Mathf.FloorToInt(availableWidth / GAME_SPEED_BUTTON_MIN_WIDTH);
            return Mathf.Clamp(countPerRow, 1, m_gameSpeedForDisplay.Length);
        }

        private int DrawGameSpeedToggle(int currentSelected, int index)
        {
            bool selected = GUILayout.Toggle(currentSelected == index, m_gameSpeedForDisplay[index],
                EditorStyles.miniButton, GUILayout.MinWidth(GAME_SPEED_BUTTON_MIN_WIDTH));
            return selected ? index : currentSelected;
        }

        private void SavePanelStates()
        {
            EditorPrefs.SetBool(GLOBAL_HELPER_PANEL_KEY, m_showGlobalHelperSetting);
            EditorPrefs.SetBool(MEMORY_PANEL_KEY, m_showMemorySetting);
            EditorPrefs.SetBool(PERFORMANCE_PANEL_KEY, m_showPerformanceSetting);
            EditorPrefs.SetBool(SYSTEM_PANEL_KEY, m_showSystemSetting);
            EditorPrefs.SetBool(OVERVIEW_PANEL_KEY, m_showOverview);
        }

        private string GetHelperStatus()
        {
            int configuredCount = 0;
            int totalCount = 3;

            if (!string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue)) configuredCount++;
            if (!string.IsNullOrEmpty(m_logHelperTypeName.stringValue)) configuredCount++;
            if (!string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue)) configuredCount++;

            return $"辅助器配置: {configuredCount}/{totalCount} 已配置" +
                   (configuredCount < totalCount ? "\n建议配置所有辅助器以获得完整功能" : "");
        }

        private string GetMemorySettingCheckStatus(MemoryStrictCheckType type)
        {
            string tips1 = "";
            string tips2 = "内存池已启用严格检查，这将极大程序影响性能。";
            switch (type)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    tips1 = "总是开启";
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    tips1 = "仅在开发模式启用";
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    tips1 = "仅在编辑器中启用";
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    tips1 = "总是禁用";
                    tips2 = "禁用状态，不影响性能。";
                    break;
            }

            return $"内存池强制检查开启状态: {tips1}\n{tips2}";
        }

        private bool IsAllHelpersConfigured()
        {
            return !string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue) &&
                   !string.IsNullOrEmpty(m_logHelperTypeName.stringValue) &&
                   !string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue);
        }

        private bool IsMemorySettingWarning()
        {
            return (MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex != MemoryStrictCheckType.AlwaysDisable;
        }

        private string GetFrameRateAdvice(int frameRate)
        {
            if (frameRate <= 30) return "低帧率模式 - 适合性能要求低的设备";
            if (frameRate <= 60) return "标准帧率 - 适合大多数游戏";
            if (frameRate <= 90) return "高帧率模式 - 适合动作游戏";
            return "超高帧率 - 适合竞技游戏，消耗更多资源";
        }

        private string GetSystemStatus(bool runInBackground, bool neverSleep)
        {
            List<string> features = new List<string>();

            if (runInBackground) features.Add("后台运行");
            if (neverSleep) features.Add("不休眠");

            if (features.Count == 0) return "标准系统模式";
            return "启用功能: " + string.Join("，", features);
        }

        private string GetMemorySettingStatusSummary()
        {
            string tips1 = "未配置";
            switch ((MemoryStrictCheckType)m_memoryStrictCheckType.enumValueIndex)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    tips1 = "总是开启";
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    tips1 = "仅在开发模式启用";
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    tips1 = "仅在编辑器中启用";
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    tips1 = "总是禁用";
                    break;
            }
            return tips1;
        }

        private string GetSystemStatusSummary()
        {
            List<string> status = new List<string>();

            if (m_runInBackground.boolValue) status.Add("后台");
            if (m_neverSleep.boolValue) status.Add("不休眠");

            return status.Count > 0 ? string.Join("+", status) : "标准";
        }

        private int GetScreenOrientationIndex(ScreenOrientation screenOrientation)
        {
            for (int i = 0; i < m_screenOrientationArr.Length; i++)
            {
                if (m_screenOrientationArr[i] == screenOrientation)
                {
                    return i;
                }
            }
            // 兜底：字段值不在候选列表（如默认 Unknown）时，落到横屏（左）
            return 2;
        }

        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (int i = 0; i < m_gameSpeedArr.Length; i++)
            {
                if (Mathf.Approximately(gameSpeed, m_gameSpeedArr[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0)
            {
                return m_gameSpeedArr[0];
            }

            if (selectedGameSpeed >= m_gameSpeedArr.Length)
            {
                return m_gameSpeedArr[^1];
            }
            return m_gameSpeedArr[selectedGameSpeed];
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            if (target == null || serializedObject == null || serializedObject.targetObject == null)
            {
                return;
            }

            m_showGlobalHelperSetting = EditorPrefs.GetBool(GLOBAL_HELPER_PANEL_KEY, m_showGlobalHelperSetting);
            m_showMemorySetting = EditorPrefs.GetBool(MEMORY_PANEL_KEY, m_showMemorySetting);
            m_showPerformanceSetting = EditorPrefs.GetBool(PERFORMANCE_PANEL_KEY, m_showPerformanceSetting);
            m_showSystemSetting = EditorPrefs.GetBool(SYSTEM_PANEL_KEY, m_showSystemSetting);
            m_showOverview = EditorPrefs.GetBool(OVERVIEW_PANEL_KEY, m_showOverview);

            m_stringUtilHelperTypeName = serializedObject?.FindProperty("stringUtilHelperTypeName");
            m_logHelperTypeName = serializedObject?.FindProperty("logHelperTypeName");
            m_jsonHelperTypeName = serializedObject?.FindProperty("jsonHelperTypeName");
            m_gameSpeed = serializedObject?.FindProperty("gameSpeed");
            m_frameRate = serializedObject?.FindProperty("frameRate");
            m_runInBackground = serializedObject?.FindProperty("runInBackground");
            m_neverSleep = serializedObject?.FindProperty("neverSleep");
            m_screenOrientation = serializedObject?.FindProperty("screenOrientation");
            m_memoryStrictCheckType = serializedObject?.FindProperty("m_memoryStrictCheckType");
            m_editorLanguage = serializedObject?.FindProperty("editorLanguage");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            List<string> textHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            textHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(Utility.StringUtil.IStringUtilHelper)));
            m_stringUtilHelperTypeNames = textHelperTypeNames.ToArray();
            m_stringUtilHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_stringUtilHelperTypeName.stringValue))
            {
                m_stringUtilHelperTypeNameIndex = textHelperTypeNames.IndexOf(m_stringUtilHelperTypeName.stringValue);
                if (m_stringUtilHelperTypeNameIndex <= 0)
                {
                    m_stringUtilHelperTypeNameIndex = 0;
                    m_stringUtilHelperTypeName.stringValue = null;
                }
            }

            List<string> logHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            logHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(DGameLog.ILogHelper)));
            m_logHelperTypeNames = logHelperTypeNames.ToArray();
            m_logHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_logHelperTypeName.stringValue))
            {
                m_logHelperTypeNameIndex = logHelperTypeNames.IndexOf(m_logHelperTypeName.stringValue);
                if (m_logHelperTypeNameIndex <= 0)
                {
                    m_logHelperTypeNameIndex = 0;
                    m_logHelperTypeName.stringValue = null;
                }
            }

            List<string> jsonHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            jsonHelperTypeNames.AddRange(TypeUtil.GetRuntimeTypeNames(typeof(Utility.IJsonHelper)));
            m_jsonHelperTypeNames = jsonHelperTypeNames.ToArray();
            m_jsonHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_jsonHelperTypeName.stringValue))
            {
                m_jsonHelperTypeNameIndex = jsonHelperTypeNames.IndexOf(m_jsonHelperTypeName.stringValue);
                if (m_jsonHelperTypeNameIndex <= 0)
                {
                    m_jsonHelperTypeNameIndex = 0;
                    m_jsonHelperTypeName.stringValue = null;
                }
            }

            List<string> tempList = new List<string>();
            System.Type enumType = typeof(MemoryStrictCheckType);
            // Array enumValues = Enum.GetValues(typeof(MemoryStrictCheckType));
            string[] enumNames = Enum.GetNames(enumType);
            for (int i = 0; i < enumNames.Length; i++)
            {
                var enumName = enumNames[i];

                if (enumName == "AlwaysEnable")
                {
                    tempList.Add("总是启用");
                }
                else if (enumName == "OnlyEnableWhenDevelopment")
                {
                    tempList.Add("仅在开发模式启用");
                }
                else if (enumName == "OnlyEnableInEditor")
                {
                    tempList.Add("仅在编辑器中启用");
                }
                else if (enumName == "AlwaysDisable")
                {
                    tempList.Add("总是禁用");
                }
            }

            m_memoryStrictCheckTypeNames = tempList.ToArray();
            m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex;
            if (m_memoryStrictCheckType.enumValueIndex <= 0)
            {
                m_memoryStrictCheckTypeIndex = m_memoryStrictCheckType.enumValueIndex  = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
