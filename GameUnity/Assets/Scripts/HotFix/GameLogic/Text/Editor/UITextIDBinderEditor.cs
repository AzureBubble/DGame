using System;
using System.IO;
using GameProto;
using Luban;
using UnityEngine;
using UnityEditor;

namespace GameLogic
{
    [CustomEditor(typeof(UITextIDBinder))]
    [CanEditMultipleObjects]
    public class UITextIDBinderEditor : Editor
    {
        /// <summary>
        /// 文本配置表文件路径（相对于项目根目录）
        /// </summary>
        private const string ConfigBytesPath = "Assets/BundleAssets/Configs/Bytes";

        private SerializedProperty m_textIDProp;
        private SerializedProperty m_previewLanguageProp;
        private SerializedProperty m_previewTextProp;

        private static Tables s_editorTables;

        private void OnEnable()
        {
            m_textIDProp = serializedObject.FindProperty("m_textID");
            m_previewLanguageProp = serializedObject.FindProperty("m_previewLanguage");
            m_previewTextProp = serializedObject.FindProperty("m_previewText");

            // 确保编辑器配置已加载
            LoadEditorTables();
        }

        private static void LoadEditorTables()
        {
            if (s_editorTables != null)
            {
                return;
            }

            try
            {
                // 从本地加载 .bytes 文件
                s_editorTables = new Tables(file =>
                {
                    string filePath = $"{ConfigBytesPath}/{file}.bytes";
                    if (!File.Exists(filePath))
                    {
                        Debug.LogWarning($"[UITextIDBinder] Config file not found: {filePath}");
                        return new ByteBuf(Array.Empty<byte>());
                    }
                    byte[] bytes = File.ReadAllBytes(filePath);
                    return new ByteBuf(bytes);
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UITextIDBinder] Failed to load editor tables: {e.Message}");
            }
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
                LoadEditorTables();
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
            LoadEditorTables();

            int textID = binder.TextID;
            LocalizationType lang = binder.PreviewLanguage;

            // 清空预览
            string previewText = string.Empty;

            if (textID == 0)
            {
                previewText = "[TextID 为 0]";
            }
            else if (s_editorTables == null)
            {
                previewText = "[配置表未加载]";
            }
            else
            {
                try
                {
                    TextConfig textConfig = s_editorTables.TbTextConfig.Get(textID);
                    if (textConfig != null)
                    {
                        int langIndex = (int)lang;
                        if (langIndex >= 0 && langIndex < textConfig.Content.Length)
                        {
                            previewText = textConfig.Content[langIndex];
                        }
                        else
                        {
                            previewText = $"[语言索引 {langIndex} 超出范围]";
                        }
                    }
                    else
                    {
                        previewText = $"[未找到 TextID: {textID}]";
                    }
                }
                catch (System.Exception e)
                {
                    previewText = $"[加载失败: {e.Message}]";
                }
            }

            // 更新 Text 组件显示
            if (binder.TextBinder != null)
            {
                binder.TextBinder.text = previewText;
                EditorUtility.SetDirty(binder.TextBinder);
            }

            // 更新预览字段
            m_previewTextProp.stringValue = previewText;
        }

        /// <summary>
        /// 当场景加载或组件状态变化时自动刷新
        /// </summary>
        private void OnSceneGUI()
        {
            if (s_editorTables == null)
            {
                LoadEditorTables();
            }
        }
    }
}