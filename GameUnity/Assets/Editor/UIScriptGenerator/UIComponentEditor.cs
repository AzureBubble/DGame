#if UNITY_EDITOR

using DGame;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GameLogic
{
    [CustomEditor(typeof(UIBindComponent))]
    public class UIComponentEditor : Editor
    {
        private UIBindComponent m_uiBindComponent;
        private SerializedProperty m_componentsProperty;
        private ReorderableList m_reorderableList;

        private void OnEnable()
        {
            m_uiBindComponent = (UIBindComponent)target;
            m_componentsProperty = serializedObject.FindProperty("m_components");
            CreateReorderableList();
        }

        private void CreateReorderableList()
        {
            m_reorderableList = new ReorderableList(serializedObject, m_componentsProperty, true, true, true, true);
            m_reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                float width = rect.width - 20;
                float indexWidth = 50f;
                float nameWidth = 150f;
                float componentWidth = width - indexWidth - nameWidth - 15f;

                EditorGUI.LabelField(new Rect(rect.x, rect.y, indexWidth, rect.height), "序号");
                EditorGUI.LabelField(new Rect(rect.x + indexWidth, rect.y, nameWidth, rect.height), "对象名称");
                EditorGUI.LabelField(new Rect(rect.x + indexWidth + nameWidth, rect.y, componentWidth, rect.height),
                    "组件引用");
            };

            m_reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty element = m_componentsProperty.GetArrayElementAtIndex(index);
                Component component = element.objectReferenceValue as Component;

                float height = EditorGUIUtility.singleLineHeight;
                float padding = 2f;
                float indexWidth = 30f;
                float nameWidth = 150f;
                float componentWidth = rect.width - indexWidth - nameWidth - 10f;

                // 序号（不可编辑）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.LabelField(new Rect(rect.x, rect.y + padding, indexWidth, height), (index + 1).ToString());
                EditorGUI.EndDisabledGroup();

                // 对象名称（不可编辑）
                EditorGUI.BeginDisabledGroup(true);
                string objectName = component != null ? component.gameObject.name : "Null Reference";
                EditorGUI.TextField(new Rect(rect.x + indexWidth, rect.y + padding, nameWidth, height), objectName);
                EditorGUI.EndDisabledGroup();

                // 组件引用（可编辑）
                EditorGUI.PropertyField(
                    new Rect(rect.x + indexWidth + nameWidth + 8, rect.y + padding, componentWidth, height),
                    element, GUIContent.none);
            };

            m_reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 4f;
            m_reorderableList.onAddCallback = (ReorderableList list) =>
            {
                m_componentsProperty.arraySize++;
                serializedObject.ApplyModifiedProperties();
            };
            m_reorderableList.onRemoveCallback = (ReorderableList list) =>
            {
                if (list.index >= 0 && list.index < m_componentsProperty.arraySize)
                {
                    m_componentsProperty.DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                }
            };
            m_reorderableList.drawNoneElementCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "列表为空 - 点击上方重新绑定进行组件重绑");
            };
        }

        private void RemoveComponentAtIndex(int index)
        {
            if (index >= 0 && index < m_componentsProperty.arraySize)
            {
                m_componentsProperty.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();

                // 重新选择相邻的元素，避免选择丢失
                if (m_reorderableList.index >= m_componentsProperty.arraySize)
                {
                    m_reorderableList.index = m_componentsProperty.arraySize - 1;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawTopButtons();
            EditorGUILayout.Space();
            m_reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTopButtons()
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("重新绑定组件", GUILayout.Height(25)))
                {
                    RebindComponents();
                }
                if (GUILayout.Button("清除空引用", GUILayout.Height(25)))
                {
                    RemoveNullComponents();
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RebindComponents()
        {
            if (m_uiBindComponent == null) return;
            m_uiBindComponent.Clear();
            UIScriptGenerator.GenerateUIComponentScript();
            UIScriptGenerator.GenerateCSharpScript(false);
            Debug.Log("重新绑定组件，如需生成脚本请使用MenuItem里的方法");
        }

        private void RemoveNullComponents()
        {
            if (m_uiBindComponent == null) return;

            for (int i = m_componentsProperty.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = m_componentsProperty.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    m_componentsProperty.DeleteArrayElementAtIndex(i);
                }
            }
            serializedObject.ApplyModifiedProperties();
            Debug.Log($"已清除空引用，剩余组件数量: {m_componentsProperty.arraySize}");
        }
    }
#endif
}