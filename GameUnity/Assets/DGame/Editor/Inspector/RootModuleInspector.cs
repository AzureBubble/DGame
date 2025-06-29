using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DGame.Editor
{
#if !ODIN_INSPECTOR || !ENABLE_ODIN_INSPECTOR

    [CustomEditor(typeof(RootModule))]
    internal sealed class RootModuleInspector : DGameInspector
    {
        private const string NONE_OPTION_NAME = "<None>";
        private static readonly float[] m_gameSpeed = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };
        private static readonly string[] m_gameSpeedForDisplay = new string[] { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_stringUtilHelperTypeName = null;
        // private SerializedProperty m_isShowGlobalHelperSetting;
        private bool m_isShowGlobalHelperSetting = true;

        private string[] m_stringUtilHelperTypeNames = null;
        private int m_stringUtilHelperTypeNameIndex = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            RootModule rootModule = (RootModule)target;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.BeginVertical("box");
                {
                    UnityEditorUtility.LayoutFrameBox(() =>
                    {
                        int textHelperSelectedIndex = EditorGUILayout.Popup("字符串辅助器", m_stringUtilHelperTypeNameIndex, m_stringUtilHelperTypeNames);
                        if (textHelperSelectedIndex != m_stringUtilHelperTypeNameIndex)
                        {
                            m_stringUtilHelperTypeNameIndex = textHelperSelectedIndex;
                            m_stringUtilHelperTypeName.stringValue = textHelperSelectedIndex <= 0 ? null : m_stringUtilHelperTypeNames[textHelperSelectedIndex];
                        }
                    }, "全局辅助器设置", ref m_isShowGlobalHelperSetting, true);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
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
            // m_isShowGlobalHelperSetting = serializedObject?.FindProperty("m_isShowGlobalHelperSetting");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            List<string> textHelperTypeNames = new List<string>
            {
                NONE_OPTION_NAME
            };

            textHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.StringUtil.IStringUtilHelper)));
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

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}