using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame
{
#if !ODIN_INSPECTOR || !ENABLE_ODIN_INSPECTOR

    [CustomEditor(typeof(RootModule))]
    internal sealed class RootModuleInspector : DGameInspector
    {
        private const string NONE_OPTION_NAME = "<None>";
        private static readonly float[] m_gameSpeedArr = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private static readonly string[] m_gameSpeedForDisplay = new string[] { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_stringUtilHelperTypeName = null;
        private SerializedProperty m_logHelperTypeName = null;
        private SerializedProperty m_jsonHelperTypeName = null;
        private SerializedProperty m_gameSpeed = null;
        private SerializedProperty m_frameRate = null;
        private SerializedProperty m_runInBackground = null;
        private SerializedProperty m_neverSleep = null;

        private string[] m_stringUtilHelperTypeNames = null;
        private int m_stringUtilHelperTypeNameIndex = 0;

        private string[] m_logHelperTypeNames = null;
        private int m_logHelperTypeNameIndex = 0;

        private string[] m_jsonHelperTypeNames = null;
        private int m_jsonHelperTypeNameIndex = 0;

        private bool m_isShowGlobalHelperSetting = true;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            RootModule rootModule = (RootModule)target;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.BeginVertical("box");
                {
                    UnityEditorUtil.LayoutFoldoutBox(() =>
                    {
                        int textHelperSelectedIndex = EditorGUILayout.Popup("字符串辅助器", m_stringUtilHelperTypeNameIndex, m_stringUtilHelperTypeNames);
                        if (textHelperSelectedIndex != m_stringUtilHelperTypeNameIndex)
                        {
                            m_stringUtilHelperTypeNameIndex = textHelperSelectedIndex;
                            m_stringUtilHelperTypeName.stringValue = textHelperSelectedIndex <= 0 ? null : m_stringUtilHelperTypeNames[textHelperSelectedIndex];
                        }

                        int logHelperSelectedIndex = EditorGUILayout.Popup("日志辅助器", m_logHelperTypeNameIndex, m_logHelperTypeNames);
                        if (logHelperSelectedIndex != m_logHelperTypeNameIndex)
                        {
                            m_logHelperTypeNameIndex = logHelperSelectedIndex;
                            m_logHelperTypeName.stringValue = logHelperSelectedIndex <= 0 ? null : m_logHelperTypeNames[logHelperSelectedIndex];
                        }

                        int jsonHelperSelectedIndex = EditorGUILayout.Popup("Json辅助器", m_jsonHelperTypeNameIndex, m_jsonHelperTypeNames);
                        if (jsonHelperSelectedIndex != m_jsonHelperTypeNameIndex)
                        {
                            m_jsonHelperTypeNameIndex = jsonHelperSelectedIndex;
                            m_jsonHelperTypeName.stringValue = jsonHelperSelectedIndex <= 0 ? null : m_jsonHelperTypeNames[jsonHelperSelectedIndex];
                        }
                    }, "全局辅助器设置", ref m_isShowGlobalHelperSetting, true);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();

            // 绘制游戏帧率
            int frameRate = EditorGUILayout.IntSlider("游戏帧率", m_frameRate.intValue, 1, 120);
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

            // 绘制游戏速度
            EditorGUILayout.BeginVertical("box");
            {
                float gameSpeed = EditorGUILayout.Slider("游戏速度", m_gameSpeed.floatValue, 0f, 8f);
                int selectedGameSpeed = GUILayout.SelectionGrid(GetSelectedGameSpeed(gameSpeed), m_gameSpeedForDisplay, 5);

                if (selectedGameSpeed >= 0)
                {
                    gameSpeed = GetGameSpeed(selectedGameSpeed);
                }

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
            }
            EditorGUILayout.EndVertical();

            bool runInBackground = EditorGUILayout.ToggleLeft("可在后台运行", m_runInBackground.boolValue);
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

            bool neverSleep = EditorGUILayout.ToggleLeft("从不休眠", m_neverSleep.boolValue);

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

            serializedObject.ApplyModifiedProperties();
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
            m_stringUtilHelperTypeName = serializedObject?.FindProperty("stringUtilHelperTypeName");
            m_logHelperTypeName = serializedObject?.FindProperty("logHelperTypeName");
            m_jsonHelperTypeName = serializedObject?.FindProperty("jsonHelperTypeName");
            m_gameSpeed = serializedObject?.FindProperty("gameSpeed");
            m_frameRate = serializedObject?.FindProperty("frameRate");
            m_runInBackground = serializedObject?.FindProperty("runInBackground");
            m_neverSleep = serializedObject?.FindProperty("neverSleep");
            // m_isShowGlobalHelperSetting = serializedObject?.FindProperty("m_isShowGlobalHelperSetting");
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

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}