using System;
using System.Collections;
using System.Collections.Generic;
using DGame;
using HybridCLR.Editor.Settings;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public static class DGameHotfixSettingsProvider
{
    private static SerializedProperty m_projectName;
    private static SerializedProperty m_hotUpdateAssemblies;
    private static SerializedProperty m_aotMetaAssemblies;
    private static SerializedProperty m_logicMainDllName;
    private static SerializedProperty m_assemblyTextAssetExtension;
    private static SerializedProperty m_assemblyTextAssetPath;
    private static SerializedProperty m_updateStyle;
    private static SerializedProperty m_updateNotice;
    private static SerializedProperty m_resDownloadPath;
    private static SerializedProperty m_fallbackResDownloadPath;
    private static SerializedProperty m_loadResWayWebGL;
    private static SerializedProperty m_isAutoAssetCopyToBuildAddress;
    private static SerializedProperty m_buildAddress;
    private static int m_logicMainDllNameIndex;
    private static SerializedObject m_serializedObject;

    private const string HOTFIX_SETTINGS_PATH = "Project/DGame/HotfixSettings";

    [MenuItem("DGame Tools/Settings/Hotfix Settings", priority = 0)]
    public static void OpenHotfixSettings() => SettingsService.OpenProjectSettings(HOTFIX_SETTINGS_PATH);

    [SettingsProvider]
    public static SettingsProvider CreateHotfixSettingsProvider()
    {
        return new SettingsProvider(HOTFIX_SETTINGS_PATH, SettingsScope.Project)
        {
            label = "[DGame]热更新设置",
            activateHandler = (searchContext, rootElement) =>
            {
                var settings = Settings.UpdateSettings;
                if (settings == null)
                {
                    return;
                }
                m_serializedObject = new SerializedObject(settings);
                if (m_serializedObject == null)
                {
                    return;
                }
                m_projectName = m_serializedObject.FindProperty("projectName");
                m_hotUpdateAssemblies = m_serializedObject.FindProperty("HotUpdateAssemblies");
                m_aotMetaAssemblies = m_serializedObject.FindProperty("AOTMetaAssemblies");
                m_logicMainDllName = m_serializedObject.FindProperty("LogicMainDllName");
                m_assemblyTextAssetExtension = m_serializedObject.FindProperty("AssemblyTextAssetExtension");
                m_assemblyTextAssetPath = m_serializedObject.FindProperty("AssemblyTextAssetPath");
                m_updateStyle = m_serializedObject.FindProperty("UpdateStyle");
                m_updateNotice = m_serializedObject.FindProperty("UpdateNotice");
                m_resDownloadPath = m_serializedObject.FindProperty("m_resDownloadPath");
                m_fallbackResDownloadPath = m_serializedObject.FindProperty("m_fallbackResDownloadPath");
                m_loadResWayWebGL = m_serializedObject.FindProperty("m_loadResWayWebGL");
                m_isAutoAssetCopyToBuildAddress = m_serializedObject.FindProperty("m_isAutoAssetCopyToBuildAddress");
                m_buildAddress = m_serializedObject.FindProperty("m_buildAddress");
            },
            guiHandler = (searchContext) =>
            {
                if (m_serializedObject == null)
                {
                    return;
                }
                var updateSettings = Settings.UpdateSettings;
                m_serializedObject?.Update();
                EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
                {
                    EditorGUILayout.PropertyField(m_projectName, new GUIContent("项目名称"));
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("启用HybridCLR", GUILayout.ExpandWidth(true)))
                        {
                            HybridCLRDefineSymbols.EnableHybridCLR();
                        }
                        if (GUILayout.Button("禁用HybridCLR", GUILayout.ExpandWidth(true)))
                        {
                            HybridCLRDefineSymbols.DisableHybridCLR();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.PropertyField(m_hotUpdateAssemblies, new GUIContent("热更程序集"));
                    EditorGUILayout.PropertyField(m_aotMetaAssemblies, new GUIContent("AOT程序集"));
                    // EditorGUILayout.PropertyField(m_logicMainDllName, new GUIContent("主业务逻辑DLL"));
                    m_logicMainDllNameIndex = updateSettings.HotUpdateAssemblies.IndexOf(m_logicMainDllName.stringValue);
                    if (m_logicMainDllNameIndex < 0)
                    {
                        m_logicMainDllNameIndex = 0;
                    }
                    m_logicMainDllNameIndex = EditorGUILayout.Popup("主业务逻辑DLL", m_logicMainDllNameIndex, updateSettings.HotUpdateAssemblies.ToArray());
                    if (m_logicMainDllName.stringValue != updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex])
                    {
                        m_logicMainDllName.stringValue = updateSettings.HotUpdateAssemblies[m_logicMainDllNameIndex];
                    }

                    EditorGUILayout.PropertyField(m_assemblyTextAssetExtension, new GUIContent("DLL文本资产打包后缀名"));
                    EditorGUILayout.PropertyField(m_assemblyTextAssetPath, new GUIContent("DLL文本资产路径"));
                    EditorGUILayout.PropertyField(m_updateStyle, new GUIContent("强制更新类型"));
                    EditorGUILayout.PropertyField(m_updateNotice, new GUIContent("是否有更新提示"));
                    // UnityEditorUtil.DrawChineseEnumPopup<UpdateStyle>(m_updateStyle, "强制更新类型");
                    // UnityEditorUtil.DrawChineseEnumPopup<UpdateNotice>(m_updateNotice, "更新是否有提示");
                    EditorGUILayout.PropertyField(m_resDownloadPath, new GUIContent("资源服务器地址"));
                    EditorGUILayout.PropertyField(m_fallbackResDownloadPath, new GUIContent("资源服务器备用地址"));
                    EditorGUILayout.PropertyField(m_loadResWayWebGL, new GUIContent("WebGL平台加载资源方式"));
                    // UnityEditorUtil.DrawChineseEnumPopup<LoadResWayWebGL>(m_loadResWayWebGL, "WebGL平台加载资源方式");
                    // EditorGUILayout.PropertyField(m_isAutoAssetCopyToBuildAddress, new GUIContent("自动Copy资源到StreamingAssets"));
                    bool isAutoAssetCopyToBuildAddress = EditorGUILayout.ToggleLeft("自动Copy资源到StreamingAssets",
                        m_isAutoAssetCopyToBuildAddress.boolValue);

                    if (isAutoAssetCopyToBuildAddress != m_isAutoAssetCopyToBuildAddress.boolValue)
                    {
                        m_isAutoAssetCopyToBuildAddress.boolValue = isAutoAssetCopyToBuildAddress;
                    }

                    EditorGUILayout.PropertyField(m_buildAddress, new GUIContent("打包程序资源地址"));
                    m_serializedObject?.ApplyModifiedProperties();
                }
                EditorGUI.EndDisabledGroup();
            },
            keywords = new string[] {"DGame", "Settings", "Custom"}
        };
    }
}