#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using GameLogic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DGame
{
    [CustomEditor(typeof(UIDebugBehaviour))]
    public class UIDebugBehaviourInspector : Editor
    {
        private object m_targetBehaviour;
        private Dictionary<Component, bool> componentFoldouts = new Dictionary<Component, bool>();
        private Dictionary<int, bool> m_fieldInfos = new Dictionary<int, bool>();
        private static Type m_monoType;

        private static Type MonoType
        {
            get
            {
                if (m_monoType == null)
                {
                    m_monoType = typeof(MonoBehaviour);
                }
                return m_monoType;
            }
        }

        private static Type m_transformType;

        private static Type TransformType
        {
            get
            {
                if (m_transformType == null)
                {
                    m_transformType = typeof(Transform);
                }
                return m_transformType;
            }
        }

        private static Type m_uiWindowType;

        private static Type UIWindowType
        {
            get
            {
                if (m_uiWindowType == null)
                {
                    m_uiWindowType = typeof(UIWindow);
                }
                return m_uiWindowType;
            }
        }

        private void OnEnable()
        {
            m_targetBehaviour = FindObjectByName(target.name);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("UI Debug Information", EditorStyles.boldLabel);

            DisplayUIInfo(target.name, m_targetBehaviour, true);
        }

        private void DisplayUIInfo(string fieldName, object o, bool isFast = false)
        {
            if (o == null || o.GetType() == m_targetBehaviour.GetType() && !isFast)
            {
                return;
            }

            Type type = m_targetBehaviour.GetType();
            int id = m_targetBehaviour.GetHashCode();
            var isShow = m_fieldInfos.GetValueOrDefault(id, true);
            isShow = EditorGUILayout.Foldout(isShow, fieldName);
            m_fieldInfos[id] = isShow;

            if (isShow)
            {
                var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.FieldType.IsSubclassOf(MonoType)
                        || fieldInfo.FieldType.IsSubclassOf(TransformType)
                        || fieldInfo.FieldType == TransformType)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.ObjectField(fieldInfo.Name, (Object)fieldInfo.GetValue(o), fieldInfo.FieldType, true);
                        GUILayout.EndHorizontal();
                    }
                    else if (fieldInfo.FieldType.IsSubclassOf(UIWindowType))
                    {
                        DisplayUIInfo(fieldInfo.Name, fieldInfo.GetValue(o));
                    }
                }
            }
        }

        private object FindObjectByName(string objName)
        {
            if (EditorApplication.isPlaying)
            {
                return UIModule.Instance.GetWindowByName(objName);
            }

            return null;
        }
    }
}

#endif