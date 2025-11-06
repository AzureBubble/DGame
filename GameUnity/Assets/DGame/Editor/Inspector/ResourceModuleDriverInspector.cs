using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace DGame
{
#if !ODIN_INSPECTOR || !ENABLE_ODIN_INSPECTOR

    [CustomEditor(typeof(ResourceModuleDriver))]
    internal sealed class ResourceModuleDriverInspector : DGameInspector
    {
        private static readonly string[] m_playModeNames = new string[]
        {
            "EditorSimulateMode (编辑器下的模拟模式)",
            "OfflinePlayMode (单机模式)",
            "HostPlayMode (联机运行模式)",
            "WebGLPlayMode (WebGL运行模式)"
        };

        private static readonly string[] m_encryptionNames = new string[]
        {
            "无加密",
            "文件偏移加密",
            "文件流加密",
        };

        private SerializedProperty m_playMode;
        private SerializedProperty m_encryptionType;
        private SerializedProperty m_updatableWhilePlaying;
        private SerializedProperty m_milliseconds;
        private SerializedProperty m_minUnloadUnusedAssetsInterval;
        private SerializedProperty m_maxUnloadUnusedAssetsInterval;
        private SerializedProperty m_useSystemUnloadUnusedAssets;
        private SerializedProperty m_assetAutoReleaseInterval;
        private SerializedProperty m_assetPoolCapacity;
        private SerializedProperty m_assetExpireTime;
        private SerializedProperty m_assetPoolPriority;
        private SerializedProperty m_failedTryAgain;
        private SerializedProperty m_packageName;
        private SerializedProperty m_downloadingMaxNum;
        private int m_playModeIndex;
        private int m_packageNameIndex;
        private int m_encryptionNameIndex;
        private string[] m_packageNames;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            ResourceModuleDriver t = (ResourceModuleDriver)target;
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
                {
                    EditorGUILayout.EnumPopup("资源运行模式", t.PlayMode);
                }
                else
                {
                    int selectedIndex = EditorGUILayout.Popup("资源运行模式", m_playModeIndex, m_playModeNames);
                    if (selectedIndex != m_playModeIndex)
                    {
                        m_playModeIndex = selectedIndex;
                        m_playMode.enumValueIndex = selectedIndex;
                    }
                }

                if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
                {
                    EditorGUILayout.EnumPopup("资源加密模式", t.EncryptionType);
                }
                else
                {
                    int selectedIndex = EditorGUILayout.Popup("资源加密模式", m_encryptionNameIndex, m_encryptionNames);
                    if (selectedIndex != m_encryptionNameIndex)
                    {
                        m_encryptionNameIndex = selectedIndex;
                        m_encryptionType.enumValueIndex = selectedIndex;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            m_packageNames = GetBuildPackageNames().ToArray();
            m_packageNameIndex = Array.IndexOf(m_packageNames, m_packageName.stringValue);

            if (m_packageNameIndex < 0)
            {
                m_packageNameIndex = 0;
            }
            m_packageNameIndex = EditorGUILayout.Popup("资源包名", m_packageNameIndex, m_packageNames);
            if (m_packageName.stringValue != m_packageNames[m_packageNameIndex])
            {
                m_packageName.stringValue = m_packageNames[m_packageNameIndex];
            }

            int milliseconds = EditorGUILayout.DelayedIntField("Milliseconds", m_milliseconds.intValue);

            if (milliseconds != m_milliseconds.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.milliseconds = milliseconds;
                }
                else
                {
                    m_milliseconds.intValue = milliseconds;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                // EditorGUILayout.PropertyField(m_updatableWhilePlaying, new GUIContent("允许边玩边下"));
                bool updatableWhilePlaying = EditorGUILayout.ToggleLeft("允许边玩边下", m_updatableWhilePlaying.boolValue);
                if (updatableWhilePlaying != m_updatableWhilePlaying.boolValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.UpdatableWhilePlaying = updatableWhilePlaying;
                    }
                    else
                    {
                        m_updatableWhilePlaying.boolValue = updatableWhilePlaying;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
            // EditorGUILayout.PropertyField(m_useSystemUnloadUnusedAssets, new GUIContent("使用资源模块卸载回收资源"));
            bool useSystemUnloadUnusedAssets = EditorGUILayout.ToggleLeft("使用资源模块卸载回收资源", m_useSystemUnloadUnusedAssets.boolValue);
            if (useSystemUnloadUnusedAssets != m_useSystemUnloadUnusedAssets.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.UseSystemUnloadUnusedAssets = useSystemUnloadUnusedAssets;
                }
                else
                {
                    m_useSystemUnloadUnusedAssets.boolValue = useSystemUnloadUnusedAssets;
                }
            }

            float minUnloadUnusedAssetsInterval =
                EditorGUILayout.Slider("最小回收资源间隔", m_minUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);

            if (Mathf.Abs(minUnloadUnusedAssetsInterval - m_minUnloadUnusedAssetsInterval.floatValue) > 0.01f)
            {
                if (EditorApplication.isPlaying)
                {
                    t.MinUnloadUnusedAssetsInterval = minUnloadUnusedAssetsInterval;
                }
                else
                {
                    m_minUnloadUnusedAssetsInterval.floatValue = minUnloadUnusedAssetsInterval;
                }
            }
            float maxUnloadUnusedAssetsInterval =
                EditorGUILayout.Slider("最大回收资源间隔", m_maxUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);

            if (Mathf.Abs(maxUnloadUnusedAssetsInterval - m_maxUnloadUnusedAssetsInterval.floatValue) > 0.01f)
            {
                if (EditorApplication.isPlaying)
                {
                    t.MaxUnloadUnusedAssetsInterval = maxUnloadUnusedAssetsInterval;
                }
                else
                {
                    m_maxUnloadUnusedAssetsInterval.floatValue = maxUnloadUnusedAssetsInterval;
                }
            }

            int downloadingMaxNum =
                EditorGUILayout.IntSlider("最大下载数量", m_downloadingMaxNum.intValue, 1, 48);
            if (downloadingMaxNum != m_downloadingMaxNum.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.DownloadingMaxNum = downloadingMaxNum;
                }
                else
                {
                    m_downloadingMaxNum.intValue = downloadingMaxNum;
                }
            }

            int failedTryAgain =
                EditorGUILayout.IntSlider("失败重试次数", m_failedTryAgain.intValue, 1, 48);
            if (failedTryAgain != m_failedTryAgain.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    t.FailedTryAgain = failedTryAgain;
                }
                else
                {
                    m_failedTryAgain.intValue = failedTryAgain;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                float assetAutoReleaseInterval =
                    EditorGUILayout.DelayedFloatField("资源对象池自动释放对象时间(秒)", m_assetAutoReleaseInterval.floatValue);
                if (Mathf.Abs(assetAutoReleaseInterval - m_assetAutoReleaseInterval.floatValue) > 0.01f)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetAutoReleaseInterval = assetAutoReleaseInterval;
                    }
                    else
                    {
                        m_assetAutoReleaseInterval.floatValue = assetAutoReleaseInterval;
                    }
                }

                int assetCapacity =
                    EditorGUILayout.DelayedIntField("资源对象池容量", m_assetPoolCapacity.intValue);
                if (assetCapacity != m_assetPoolCapacity.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetCapacity = assetCapacity;
                    }
                    else
                    {
                        m_assetPoolCapacity.intValue = assetCapacity;
                    }
                }

                float assetExpireTime =
                    EditorGUILayout.DelayedFloatField("资源过期时间(秒)", m_assetExpireTime.floatValue);
                if (Mathf.Abs(assetExpireTime - m_assetExpireTime.floatValue) > 0.01f)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetExpireTime = assetExpireTime;
                    }
                    else
                    {
                        m_assetExpireTime.floatValue = assetExpireTime;
                    }
                }

                int assetPoolPriority =
                    EditorGUILayout.DelayedIntField("资源对象池优先级", m_assetPoolPriority.intValue);
                if (assetPoolPriority != m_assetPoolPriority.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        t.AssetPriority = assetPoolPriority;
                    }
                    else
                    {
                        m_assetPoolPriority.intValue = assetPoolPriority;
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("卸载未使用资源",
                    Utility.StringUtil.Format("{0:F2} / {1:F2}", t.LastUnloadUnusedAssetsOperationElapsedSeconds, t.MaxUnloadUnusedAssetsInterval));
                EditorGUILayout.LabelField("当前资源适用的游戏版本号", !string.IsNullOrEmpty(t.ApplicableGameVersion) ? t.ApplicableGameVersion : "<Unknown>" );
            }

            serializedObject.ApplyModifiedProperties();
            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private List<string> GetBuildPackageNames()
        {
            List<string> packageNames = new List<string>();

            foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
            {
                packageNames.Add(package.PackageName);
            }
            return packageNames;
        }

        private void OnEnable()
        {
            m_playMode = serializedObject.FindProperty("playMode");
            m_encryptionType = serializedObject.FindProperty("encryptionType");
            m_updatableWhilePlaying = serializedObject.FindProperty("updatableWhilePlaying");
            m_milliseconds = serializedObject.FindProperty("milliseconds");
            m_minUnloadUnusedAssetsInterval = serializedObject.FindProperty("minUnloadUnusedAssetsInterval");
            m_maxUnloadUnusedAssetsInterval = serializedObject.FindProperty("maxUnloadUnusedAssetsInterval");
            m_useSystemUnloadUnusedAssets = serializedObject.FindProperty("useSystemUnloadUnusedAssets");
            m_assetAutoReleaseInterval = serializedObject.FindProperty("assetAutoReleaseInterval");
            m_assetPoolCapacity = serializedObject.FindProperty("assetPoolCapacity");
            m_assetExpireTime = serializedObject.FindProperty("assetExpireTime");
            m_assetPoolPriority = serializedObject.FindProperty("assetPoolPriority");
            m_failedTryAgain = serializedObject.FindProperty("failedTryAgain");
            m_packageName = serializedObject.FindProperty("packageName");
            m_downloadingMaxNum = serializedObject.FindProperty("downloadingMaxNum");

            RefreshPlayModeNames();
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            serializedObject.ApplyModifiedProperties();
        }

        private void RefreshPlayModeNames()
        {
            m_playModeIndex = m_playMode.enumValueIndex > 0 ? m_playMode.enumValueIndex : 0;
        }
    }

#endif
}