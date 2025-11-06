using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HybridCLR.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace DGame
{
    [CustomEditor(typeof(UpdateSettings), true)]
    public class UpdateSettingsInspector : UnityEditor.Editor
    {
        private SerializedProperty m_projectName;
        private SerializedProperty m_hotUpdateAssemblies;
        private SerializedProperty m_aotMetaAssemblies;
        private SerializedProperty m_logicMainDllName;
        private SerializedProperty m_assemblyTextAssetExtension;
        private SerializedProperty m_assemblyTextAssetPath;
        private SerializedProperty m_updateStyle;
        private SerializedProperty m_updateNotice;
        private SerializedProperty m_resDownloadPath;
        private SerializedProperty m_fallbackResDownloadPath;
        private SerializedProperty m_loadResWayWebGL;
        private SerializedProperty m_isAutoAssetCopyToBuildAddress;
        private SerializedProperty m_buildAddress;

        private List<string> m_hotUpdateAssembliesList;
        private List<string> m_aotMetaAssembliesList;
        private int m_logicMainDllNameIndex;

        private void OnEnable()
        {
            m_projectName = serializedObject.FindProperty("projectName");
            m_hotUpdateAssemblies = serializedObject.FindProperty("HotUpdateAssemblies");
            m_aotMetaAssemblies = serializedObject.FindProperty("AOTMetaAssemblies");
            m_logicMainDllName = serializedObject.FindProperty("LogicMainDllName");
            m_assemblyTextAssetExtension = serializedObject.FindProperty("AssemblyTextAssetExtension");
            m_assemblyTextAssetPath = serializedObject.FindProperty("AssemblyTextAssetPath");
            m_updateStyle = serializedObject.FindProperty("UpdateStyle");
            m_updateNotice = serializedObject.FindProperty("UpdateNotice");
            m_resDownloadPath = serializedObject.FindProperty("m_resDownloadPath");
            m_fallbackResDownloadPath = serializedObject.FindProperty("m_fallbackResDownloadPath");
            m_loadResWayWebGL = serializedObject.FindProperty("m_loadResWayWebGL");
            m_isAutoAssetCopyToBuildAddress = serializedObject.FindProperty("m_isAutoAssetCopyToBuildAddress");
            m_buildAddress = serializedObject.FindProperty("m_buildAddress");

            UpdateSettings updateSettings = (UpdateSettings)target;
            if (updateSettings != null)
            {
                m_hotUpdateAssembliesList = new List<string>(updateSettings.HotUpdateAssemblies);
                m_aotMetaAssembliesList = new List<string>(updateSettings.AOTMetaAssemblies);
            }
        }

        public override void OnInspectorGUI()
        {
            // 记录修改前状态
            EditorGUI.BeginChangeCheck();
            // base.OnInspectorGUI();

            serializedObject.Update();
            UpdateSettings updateSettings = (UpdateSettings)target;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_projectName, new GUIContent("项目名称"));
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
                // UnityEditorUtil.DrawChineseEnumPopup<UpdateStyle>(m_updateStyle, "强制更新类型");
                // UnityEditorUtil.DrawChineseEnumPopup<UpdateNotice>(m_updateNotice, "更新是否有提示");
                EditorGUILayout.PropertyField(m_updateNotice, new GUIContent("是否有更新提示"));
                EditorGUILayout.PropertyField(m_resDownloadPath, new GUIContent("资源服务器地址"));
                EditorGUILayout.PropertyField(m_fallbackResDownloadPath, new GUIContent("资源服务器备用地址"));
                EditorGUILayout.PropertyField(m_loadResWayWebGL, new GUIContent("WebGL平台加载资源方式"));
                // UnityEditorUtil.DrawChineseEnumPopup<LoadResWayWebGL>(m_loadResWayWebGL, "WebGL平台加载资源方式");
                // EditorGUILayout.PropertyField(m_isAutoAssetCopyToBuildAddress, new GUIContent("自动Copy资源到StreamingAssets"));
                bool isAutoAssetCopyToBuildAddress = EditorGUILayout.ToggleLeft("自动Copy资源到StreamingAssets", m_isAutoAssetCopyToBuildAddress.boolValue);
                if (isAutoAssetCopyToBuildAddress != m_isAutoAssetCopyToBuildAddress.boolValue)
                {
                    m_isAutoAssetCopyToBuildAddress.boolValue = isAutoAssetCopyToBuildAddress;
                }
                EditorGUILayout.PropertyField(m_buildAddress, new GUIContent("打包程序资源地址"));
            }
            EditorGUI.EndDisabledGroup();
            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(updateSettings);
                bool isHotChanged = !m_hotUpdateAssembliesList.SequenceEqual(updateSettings.HotUpdateAssemblies);
                bool isAOTChanged = !m_aotMetaAssembliesList.SequenceEqual(updateSettings.AOTMetaAssemblies);

                if (isHotChanged)
                {
                    m_hotUpdateAssembliesList = new List<string>(updateSettings.HotUpdateAssemblies);
                    HybridCLRSettings.Instance.hotUpdateAssemblies = updateSettings.HotUpdateAssemblies.ToArray();
                    for (int i = 0; i < updateSettings.HotUpdateAssemblies.Count; i++)
                    {
                        var assemblyName = updateSettings.HotUpdateAssemblies[i];
                        string assemblyNameWithoutExtension = assemblyName.Substring(0, assemblyName.LastIndexOf('.'));
                        HybridCLRSettings.Instance.hotUpdateAssemblies[i] = assemblyNameWithoutExtension;
                    }
                    Debugger.Info("======== HybridCLR => 热更程序集发生变化 ========");
                }
                if (isAOTChanged)
                {
                    m_aotMetaAssembliesList = new List<string>(updateSettings.AOTMetaAssemblies);
                    HybridCLRSettings.Instance.patchAOTAssemblies = updateSettings.AOTMetaAssemblies.ToArray();
                    Debugger.Info("======== HybridCLR => AOT程序集发生变化 ========");
                }

                if (isAOTChanged || isHotChanged)
                {
                    EditorUtility.SetDirty(HybridCLRSettings.Instance);
                    AssetDatabase.SaveAssets();
                }
            }
        }

        public static void ForceUpdateAssemblies()
        {
            UpdateSettings updateSettings = null;
            string[] guids = AssetDatabase.FindAssets("t:UpdateSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                updateSettings = AssetDatabase.LoadAssetAtPath<UpdateSettings>(path);
            }

            if (updateSettings == null)
            {
                Debugger.Error("======== 没有找到 updateSettings SO 文件 ========");
                return;
            }

            HybridCLRSettings.Instance.hotUpdateAssemblies = updateSettings.HotUpdateAssemblies.ToArray();
            for (int i = 0; i < updateSettings.HotUpdateAssemblies.Count; i++)
            {
                var assemblyName = updateSettings.HotUpdateAssemblies[i];
                string assemblyNameWithoutExtension = assemblyName.Substring(0, assemblyName.LastIndexOf('.'));
                HybridCLRSettings.Instance.hotUpdateAssemblies[i] = assemblyNameWithoutExtension;
            }
            HybridCLRSettings.Instance.patchAOTAssemblies = updateSettings.AOTMetaAssemblies.ToArray();
            Debugger.Info("======== HybridCLR => AOT和热更程序集发生变化 ========");
            EditorUtility.SetDirty(HybridCLRSettings.Instance);
            EditorUtility.SetDirty(updateSettings);
            AssetDatabase.SaveAssets();
        }

        public static void ForceUpdateAssemblies2()
        {
            UpdateSettings updateSettings = null;
            string[] guids = AssetDatabase.FindAssets("t:UpdateSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                updateSettings = AssetDatabase.LoadAssetAtPath<UpdateSettings>(path);
            }

            if (updateSettings == null)
            {
                Debugger.Error("======== 没有找到 updateSettings SO 文件 ========");
                return;
            }

            updateSettings.HotUpdateAssemblies = HybridCLRSettings.Instance.hotUpdateAssemblies.ToList();
            for (int i = 0; i < HybridCLRSettings.Instance.hotUpdateAssemblies.Length; i++)
            {
                var assemblyName = HybridCLRSettings.Instance.hotUpdateAssemblies[i];
                string assemblyNameWithoutExtension = assemblyName + ".dll";
                updateSettings.HotUpdateAssemblies[i] = assemblyNameWithoutExtension;
            }
            updateSettings.AOTMetaAssemblies = HybridCLRSettings.Instance.patchAOTAssemblies.ToList();
            Debugger.Info("======== HybridCLR => AOT和热更程序集发生变化 ========");
            EditorUtility.SetDirty(HybridCLRSettings.Instance);
            EditorUtility.SetDirty(updateSettings);
            AssetDatabase.SaveAssets();
        }
    }

    [InitializeOnLoad]
    public static class HybridCLRSettingsSyncUpdateSettings
    {
        private static List<string> m_lastHotUpdateAssemblies;
        private static List<string> m_lastAOTAssemblies;

        static HybridCLRSettingsSyncUpdateSettings()
        {
            m_lastHotUpdateAssemblies = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies);
            m_lastAOTAssemblies = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (HybridCLRSettings.Instance == null)
            {
                return;
            }

            if (!m_lastHotUpdateAssemblies.SequenceEqual(HybridCLRSettings.Instance.hotUpdateAssemblies)
                || !m_lastAOTAssemblies.SequenceEqual(HybridCLRSettings.Instance.patchAOTAssemblies))
            {
                UpdateSettingsInspector.ForceUpdateAssemblies2();
                m_lastHotUpdateAssemblies = new List<string>(HybridCLRSettings.Instance.hotUpdateAssemblies) ;
                m_lastAOTAssemblies = new List<string>(HybridCLRSettings.Instance.patchAOTAssemblies);
            }
        }
    }
}