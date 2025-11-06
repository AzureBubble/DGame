using System;
using System.Collections;
using System.Collections.Generic;
using DGame;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public static class DGameUIGeneratorSettingsProvider
{
    [MenuItem("DGame Tools/Settings/UI代码生成器设置", priority = 1)]
    public static void OpenUIGeneratorSettings() => SettingsService.OpenProjectSettings(settingsPath);

    private const string settingsPath = "Project/DGame/UI代码生成器设置";
    private static bool m_show = true;
    private static ReorderableList m_reorderableList;

    [SettingsProvider]
    public static SettingsProvider CreateUIGeneratorSettingsProvider()
    {
        return new SettingsProvider(settingsPath, SettingsScope.Project)
        {
            label = "[DGame]UI代码生成器设置",
            guiHandler = (searchContext) =>
            {
                var uiScriptGeneratorSettings = UIScriptGeneratorSettings.Instance;
                var uiScriptGenerator = new SerializedObject(uiScriptGeneratorSettings);
                // uiScriptGenerator.Update();
                EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("uiRoot"));
                var useBindComponent = uiScriptGenerator.FindProperty("useBindComponent");
                EditorGUILayout.PropertyField(useBindComponent);

                // EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("codePath"));
                if (useBindComponent.boolValue)
                {
                    var codePath = uiScriptGenerator.FindProperty("codePath");
                    codePath.stringValue =
                        DrawFolderField("代码文件生成路径", String.Empty, codePath.stringValue); // "FolderOpened Icon"
                    var windowComponentSuffixName = uiScriptGenerator.FindProperty("windowComponentSuffixName");
                    windowComponentSuffixName.stringValue =
                        EditorGUILayout.TextField("窗体组件脚本后缀名", windowComponentSuffixName.stringValue);
                    var widgetComponentSuffixName = uiScriptGenerator.FindProperty("widgetComponentSuffixName");
                    widgetComponentSuffixName.stringValue =
                        EditorGUILayout.TextField("widget组件脚本后缀名", widgetComponentSuffixName.stringValue);
                }

                EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("nameSpace"));
                EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("widgetName"));
                EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("codeStyle"));
                // EditorGUILayout.PropertyField(uiScriptGenerator.FindProperty("scriptGenerateRulers"));
                DrawReorderableList(uiScriptGenerator);
                uiScriptGenerator.ApplyModifiedProperties();
            },
            keywords = new[] { "DGame", "Settings", "Custom" }
        };
    }

    private static string DrawFolderField(string label, string labelIcon, string path)
    {
        using var horizontalScope = new EditorGUILayout.HorizontalScope();

        var buttonGUIContent = new GUIContent("选择", EditorGUIUtility.IconContent("Folder Icon").image);

        if (!string.IsNullOrEmpty(labelIcon))
        {
            var labelGUIContent = new GUIContent(" " + label, EditorGUIUtility.IconContent(labelIcon).image);
            path = EditorGUILayout.TextField(labelGUIContent, path);
        }
        else
        {
            path = EditorGUILayout.TextField(label, path);
        }

        if (GUILayout.Button(buttonGUIContent, GUILayout.Width(60), GUILayout.Height(20)))
        {
            var newPath = EditorUtility.OpenFolderPanel(label, Application.dataPath, string.Empty);

            if (!string.IsNullOrEmpty(newPath) && newPath.StartsWith(Application.dataPath))
            {
                path = "Assets" + newPath.Substring(Application.dataPath.Length);
            }
            else
            {
                Debug.LogError("路径不在Unity项目内: " + newPath);
            }
        }

        return path;
    }

    private static void DrawReorderableList(SerializedObject serializedObject)
    {
        SerializedProperty ruleListProperty = serializedObject.FindProperty("scriptGenerateRulers");
        if (ruleListProperty == null)
        {
            return;
        }

        m_show = EditorGUILayout.BeginFoldoutHeaderGroup(m_show, "scriptGenerateRulers");
        if (m_show)
        {
            if (m_reorderableList == null)
            {
                m_reorderableList = new ReorderableList(serializedObject, ruleListProperty, true, true, true, true);

                m_reorderableList.drawHeaderCallback = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Script Generate Rules", EditorStyles.boldLabel);
                };

                m_reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    // 开始检查字段修改
                    EditorGUI.BeginChangeCheck();
                    SerializedProperty element = ruleListProperty.GetArrayElementAtIndex(index);

                    rect.y += 2;
                    float fieldHeight = EditorGUIUtility.singleLineHeight;

                    float fieldWidth = (rect.width - 10) / 4f;
                    SerializedProperty regexProperty = element.FindPropertyRelative("uiElementRegex");

                    Rect titleRect = new Rect(rect.x, rect.y, fieldWidth, fieldHeight);
                    EditorGUI.LabelField(titleRect, regexProperty.stringValue);
                    Rect regexRect = new Rect(rect.x + fieldWidth, rect.y, fieldWidth, fieldHeight);
                    EditorGUI.PropertyField(regexRect, regexProperty, GUIContent.none);
                    Rect componentRect = new Rect(rect.x + fieldWidth * 2 + 5, rect.y, fieldWidth, fieldHeight);
                    SerializedProperty componentProperty = element.FindPropertyRelative("componentName");
                    EditorGUI.PropertyField(componentRect, componentProperty, GUIContent.none);
                    Rect widgetRect = new Rect(rect.x + fieldWidth * 3 + 20, rect.y, fieldWidth, fieldHeight);
                    SerializedProperty widgetProperty = element.FindPropertyRelative("isUIWidget");
                    EditorGUI.PropertyField(widgetRect, widgetProperty, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(serializedObject.targetObject);
                        AssetDatabase.SaveAssets();
                    }
                };
                m_reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6;
                m_reorderableList.onChangedCallback = (ReorderableList list) =>
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
                m_reorderableList.onAddCallback = (ReorderableList list) =>
                {
                    list.serializedProperty.arraySize++;
                    list.index = list.serializedProperty.arraySize - 1;
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
                m_reorderableList.onRemoveCallback = (ReorderableList list) =>
                {
                    list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
            }

            m_reorderableList.DoLayoutList();

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
            }
        }

        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space();
    }
}