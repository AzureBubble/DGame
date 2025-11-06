using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(ProcedureSettings))]
    internal sealed class ProcedureSettingsInspector : DGameInspector
    {
        private SerializedProperty m_availableProcedureTypeNames;
        private SerializedProperty m_startProcedureTypeName;

        private string[] m_procedureTypeNames;
        private List<string> m_curAvailableProcedureTypeNames;
        private int m_startProcedureIndex = -1;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            ProcedureSettings t = (ProcedureSettings)target;

            if (string.IsNullOrEmpty(m_startProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("入口流程无效", MessageType.Error);
            }
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                GUILayout.Label("流程状态数组", EditorStyles.boldLabel);

                if (m_procedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (string procedureName in m_procedureTypeNames)
                        {
                            bool selected = m_curAvailableProcedureTypeNames.Contains(procedureName);

                            if (selected != EditorGUILayout.ToggleLeft(procedureName, selected))
                            {
                                if (!selected)
                                {
                                    m_curAvailableProcedureTypeNames.Add(procedureName);
                                    WriteAvailableProcedureTypeNames();
                                }
                                else if(procedureName != m_startProcedureTypeName.stringValue)
                                {
                                    m_curAvailableProcedureTypeNames.Remove(procedureName);
                                    WriteAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("流程数组无效", MessageType.Warning);
                }

                if (m_curAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();
                    int selectedIndex = EditorGUILayout.Popup("流程入口", m_startProcedureIndex, m_curAvailableProcedureTypeNames.ToArray());

                    if (selectedIndex != m_startProcedureIndex)
                    {
                        m_startProcedureIndex = selectedIndex;
                        m_startProcedureTypeName.stringValue = m_curAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("需要先进行流程数组勾选", MessageType.Info);
                }
            }
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            m_availableProcedureTypeNames = serializedObject.FindProperty("availableProcedureTypeNames");
            m_startProcedureTypeName = serializedObject.FindProperty("startProcedureTypeName");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_procedureTypeNames = TypeUtil.GetRuntimeTypeNames(typeof(ProcedureBase));
            ReadAvailableProcedureTypeNames();
            int oldCount = m_curAvailableProcedureTypeNames.Count;
            m_curAvailableProcedureTypeNames = m_curAvailableProcedureTypeNames.Where(x => m_procedureTypeNames.Contains(x)).ToList();

            if (m_curAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if(!string.IsNullOrEmpty(m_startProcedureTypeName.stringValue))
            {
                m_startProcedureIndex = m_curAvailableProcedureTypeNames.IndexOf(m_startProcedureTypeName.stringValue);

                if (m_startProcedureIndex < 0)
                {
                    m_startProcedureTypeName.stringValue = null;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void WriteAvailableProcedureTypeNames()
        {
            m_availableProcedureTypeNames.ClearArray();

            if (m_curAvailableProcedureTypeNames == null)
            {
                return;
            }

            m_curAvailableProcedureTypeNames.Sort();
            int count = m_curAvailableProcedureTypeNames.Count;

            for (int i = 0; i < count; i++)
            {
                m_availableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue = m_curAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(m_startProcedureTypeName.stringValue))
            {
                m_startProcedureIndex = m_curAvailableProcedureTypeNames.IndexOf(m_startProcedureTypeName.stringValue);

                if (m_startProcedureIndex < 0)
                {
                    m_startProcedureTypeName.stringValue = null;
                }
            }
        }

        private void ReadAvailableProcedureTypeNames()
        {
            if (m_curAvailableProcedureTypeNames == null)
            {
                m_curAvailableProcedureTypeNames = new List<string>();
            }
            m_curAvailableProcedureTypeNames.Clear();
            int cnt = m_availableProcedureTypeNames.arraySize;

            for (int i = 0; i < cnt; i++)
            {
                m_curAvailableProcedureTypeNames.Add(m_availableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }
    }
}