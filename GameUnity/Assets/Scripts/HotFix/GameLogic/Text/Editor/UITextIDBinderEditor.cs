using GameProto;
using UnityEngine;
using UnityEditor;

namespace GameLogic
{
    [CustomEditor(typeof(UITextIDBinder))]
    [CanEditMultipleObjects]
    public class UITextIDBinderEditor : Editor
    {
        private SerializedProperty m_textIDProp;
        private SerializedProperty m_previewLanguageProp;
        private SerializedProperty m_previewTextProp;

        private void OnEnable()
        {
            m_textIDProp = serializedObject.FindProperty("m_textID");
            m_previewLanguageProp = serializedObject.FindProperty("m_previewLanguage");
            m_previewTextProp = serializedObject.FindProperty("m_previewText");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UITextIDBinder binder = (UITextIDBinder)target;

            EditorGUI.BeginChangeCheck();

            // 绘制默认字段
            EditorGUILayout.PropertyField(m_textIDProp, new GUIContent("文本配置ID"));
            EditorGUILayout.PropertyField(m_previewLanguageProp, new GUIContent("预览语言"));

            // 实时更新预览
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                UpdatePreview(binder);
                EditorUtility.SetDirty(target);
            }

            // 显示预览文本（只读）
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("预览文本", m_previewTextProp.stringValue);
            EditorGUI.EndDisabledGroup();

            // 添加刷新按钮
            if (GUILayout.Button("刷新预览"))
            {
                EditorConfigLoader.ReloadTables();
                UpdatePreview(binder);
                EditorUtility.SetDirty(target);
            }

            // 显示警告信息
            if (binder.TextID == 0)
            {
                EditorGUILayout.HelpBox("TextID 为 0，请设置有效的文本ID", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdatePreview(UITextIDBinder binder)
        {
            int textID = binder.TextID;
            LocalizationType lang = binder.PreviewLanguage;

            string previewText = EditorConfigLoader.GetTextContent(textID, lang);

            // 更新 Text 组件显示
            if (binder.TextBinder != null)
            {
                binder.TextBinder.text = previewText;
                EditorUtility.SetDirty(binder.TextBinder);
            }

            // 更新预览字段
            m_previewTextProp.stringValue = previewText;
        }
    }
}