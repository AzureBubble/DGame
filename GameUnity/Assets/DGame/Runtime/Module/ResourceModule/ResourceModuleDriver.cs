using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

#if UNITY_EDITOR
using YooAsset.Editor;
using UnityEditor;
#endif

#if ODIN_INSPECTOR && UNITY_EDITOR
using Sirenix.OdinInspector;
#endif

namespace DGame
{
    public class ResourceModuleDriver : MonoBehaviour
    {
        private IResourceModule m_resourceModule;
        private bool m_forceUnloadUnusedAssets;
        private bool m_preorderUnloadUnusedAssets;
        private bool m_performGCCollect;
        private AsyncOperation m_asyncOperation;
        private float m_lastUnloadUnusedAssetsOperationElapsedSeconds;
        public float LastUnloadUnusedAssetsOperationElapsedSeconds => m_lastUnloadUnusedAssetsOperationElapsedSeconds;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(7), LabelText("最小回收资源间隔"), Range(0, 3600)]
#endif
        private float minUnloadUnusedAssetsInterval = 60f;
        public float MinUnloadUnusedAssetsInterval { get => minUnloadUnusedAssetsInterval; set => minUnloadUnusedAssetsInterval = value; }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(8), LabelText("最大回收资源间隔"), Range(0, 3600)]
#endif
        private float maxUnloadUnusedAssetsInterval = 300f;
        public float MaxUnloadUnusedAssetsInterval { get => maxUnloadUnusedAssetsInterval; set => maxUnloadUnusedAssetsInterval = value; }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(6), LabelText("使用资源模块卸载回收资源"), ToggleLeft]
#endif
        private bool useSystemUnloadUnusedAssets = true;
        public bool UseSystemUnloadUnusedAssets { get => useSystemUnloadUnusedAssets; set => useSystemUnloadUnusedAssets = value; }

        public string PackageVersion { get; set; }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(3), LabelText("资源包名"), ValueDropdown("GetPackageNameOptions")]
#endif
        private string packageName = "DefaultPackage";

        public string PackageName { get => packageName; set => packageName = value; }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(1), DisableInPlayMode, LabelText("资源运行模式"), ValueDropdown("GetPlayModeOptions")]
#endif
        private EPlayMode playMode = EPlayMode.EditorSimulateMode;

        public EPlayMode PlayMode
        {
            get
            {
#if UNITY_EDITOR

                return (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorPlayMode", (int)playMode);

#else

                if (playMode == EPlayMode.EditorSimulateMode)
                {
                    return EPlayMode.OfflinePlayMode;
                }
                return playMode;

#endif
            }
            set
            {
#if UNITY_EDITOR
                playMode = value;
#endif
            }
        }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(2), DisableInPlayMode, LabelText("资源加密模式")]
#endif
        private EncryptionType encryptionType = EncryptionType.None;
        public EncryptionType EncryptionType => encryptionType;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(5), DisableInPlayMode, LabelText("允许边玩边下"), ToggleLeft]
#endif
        private bool updatableWhilePlaying;
        public bool UpdatableWhilePlaying { get => m_resourceModule.UpdatableWhilePlaying; set => m_resourceModule.UpdatableWhilePlaying = updatableWhilePlaying = value; }

        /// <summary>
        /// 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(4)]
#endif
        public long milliseconds = 30;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(9), LabelText("最大下载数量"), ProgressBar(1, 48, Height = 18, DrawValueLabel = true)]
#endif
        private int downloadingMaxNum = 10;
        public int DownloadingMaxNum { get => downloadingMaxNum; set => downloadingMaxNum = value; }

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(10), LabelText("失败重试次数"), ProgressBar(1, 48, Height = 18, DrawValueLabel = true)]
#endif
        private int failedTryAgain = 3;
        public int FailedTryAgain { get => failedTryAgain; set => failedTryAgain = value; }

        /// <summary>
        /// 获取当前资源适用的游戏版本号。
        /// </summary>
        public string ApplicableGameVersion => m_resourceModule?.ApplicableGameVersion;

        /// <summary>
        /// 获取当前内部资源版本号。
        /// </summary>
        public int InternalResourceVersion => m_resourceModule.InternalResourceVersion;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(11), DisableInPlayMode, LabelText("资源对象池自动释放对象时间(秒)"), Range(0, 3600)]
#endif
        private float assetAutoReleaseInterval = 60f;
        public float AssetAutoReleaseInterval { get => m_resourceModule.AssetAutoReleaseInterval; set => m_resourceModule.AssetAutoReleaseInterval = assetAutoReleaseInterval = value; }
        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(12), DisableInPlayMode, LabelText("资源对象池容量"), ProgressBar(0, 128, DrawValueLabel = true, Height = 18)]
#endif
        private int assetPoolCapacity = 64;
        public int AssetCapacity { get => m_resourceModule.AssetPoolCapacity; set => m_resourceModule.AssetPoolCapacity = assetPoolCapacity = value; }
        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(14), DisableInPlayMode, LabelText("资源过期时间(秒)"), Range(0, 3600)]
#endif
        private float assetExpireTime = 60f;
        public float AssetExpireTime { get => m_resourceModule.AssetExpireTime; set => m_resourceModule.AssetExpireTime = assetExpireTime = value; }
        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [PropertyOrder(15), DisableInPlayMode, LabelText("资源对象池优先级"), ProgressBar(1, 48, Height = 18, DrawValueLabel = true)]
#endif
        private int assetPoolPriority;
        public int AssetPriority { get => m_resourceModule.AssetPoolPriority; set => m_resourceModule.AssetPoolPriority = assetPoolPriority = value; }

        private void Start()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            if (m_resourceModule == null)
            {
                Debugger.Fatal("资源模块无效！");
                return;
            }

            if (PlayMode == EPlayMode.EditorSimulateMode)
            {
                Debugger.Info("在此模式下运行，资源模块会优先使用编辑器资源，应该首先验证这些资源是否有效");
#if !UNITY_EDITOR
                PlayMode = EPlayMode.OfflinePlayMode;
#endif
            }

            m_resourceModule.DefaultPackageName = packageName;
            m_resourceModule.PlayMode = PlayMode;
            m_resourceModule.EncryptionType = EncryptionType;
            m_resourceModule.Milliseconds = milliseconds;
            // TODO：设置资源热更地址
            // m_resourceModule.HostServerURl =
            // m_resourceModule.FallbackHostServerURL =
            // m_resourceModule.LoadResWayWebGL =
            m_resourceModule.DownloadingMaxNum = DownloadingMaxNum;
            m_resourceModule.FailedTryAgainCnt = FailedTryAgain;
            m_resourceModule.UpdatableWhilePlaying = UpdatableWhilePlaying;
            m_resourceModule.Initialize();
            m_resourceModule.AssetAutoReleaseInterval = assetAutoReleaseInterval;
            m_resourceModule.AssetPoolCapacity = assetPoolCapacity;
            m_resourceModule.AssetExpireTime = assetExpireTime;
            m_resourceModule.AssetPoolPriority = assetPoolPriority;
            m_resourceModule.SetForceUnloadUnusedAssetsAction(ForceUnloadUnusedAssets);
            Debugger.Info($"======== 资源加载模式: {PlayMode} ========");
        }

        #region 资源自动释放

        /// <summary>
        /// 强制执行释放未被使用的资源。
        /// </summary>
        /// <param name="performGCCollect">是否使用垃圾回收。</param>
        private void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            m_forceUnloadUnusedAssets = true;
            if (performGCCollect)
            {
                m_performGCCollect = performGCCollect;
            }
        }

        private void Update()
        {
            m_lastUnloadUnusedAssetsOperationElapsedSeconds += Time.unscaledDeltaTime;

            // 回收句柄为空
            // 强制回收 m_forceUnloadUnusedAssets 优先级最高
            // 周期回收 maxUnloadUnusedAssetsInterval
            if (m_asyncOperation == null &&
                (m_forceUnloadUnusedAssets ||
                 m_lastUnloadUnusedAssetsOperationElapsedSeconds >= maxUnloadUnusedAssetsInterval ||
                 m_preorderUnloadUnusedAssets &&
                 m_lastUnloadUnusedAssetsOperationElapsedSeconds >= minUnloadUnusedAssetsInterval))
            {
                Debugger.Info("======== 自动卸载释放没有使用的资源 ========");
                m_forceUnloadUnusedAssets = false;
                m_preorderUnloadUnusedAssets = false;
                m_lastUnloadUnusedAssetsOperationElapsedSeconds = 0f;
                m_asyncOperation = Resources.UnloadUnusedAssets();

                if (useSystemUnloadUnusedAssets)
                {
                    m_resourceModule?.UnloadUnusedAssets();
                }
            }

            if (m_asyncOperation != null && m_asyncOperation.isDone)
            {
                m_asyncOperation = null;

                if (m_performGCCollect)
                {
                    Debugger.Info("======== GC.Collect ========");
                    m_performGCCollect = false;
                    GC.Collect();
                }
            }

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
                unLoadUnusedAssetsInfo = Utility.StringUtil.Format("{0:F2} / {1:F2}", LastUnloadUnusedAssetsOperationElapsedSeconds, MaxUnloadUnusedAssetsInterval);
#endif
        }

        #endregion

        #region ODIN设置

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR

            [SerializeField, PropertyOrder(16), DisplayAsString, DisableInPlayMode, LabelText("卸载未使用资源"), ShowIf("ShowInEditorPlaying")]
            private string unLoadUnusedAssetsInfo;

            [ShowInInspector, PropertyOrder(17), DisplayAsString, DisableInPlayMode, LabelText("当前资源适用的游戏版本号"), ShowIf("ShowInEditorPlaying")]
            private string InspectorApplicableGameVersion => !string.IsNullOrEmpty(ApplicableGameVersion) ?
                    ApplicableGameVersion : "<Unknown>";

            private bool ShowInEditorPlaying()
            {
                    return EditorApplication.isPlaying;
            }

        private ValueDropdownList<EPlayMode> GetPlayModeOptions()
        {
                return new ValueDropdownList<EPlayMode>()
                {
                        { "编辑器下的模拟模式", EPlayMode.EditorSimulateMode },
                        { "单机模式", EPlayMode.OfflinePlayMode },
                        { "联机运行模式", EPlayMode.HostPlayMode },
                        { "WebGL运行模式", EPlayMode.WebPlayMode },
                };
        }

        private ValueDropdownList<string> GetPackageNameOptions()
        {
                ValueDropdownList<string> tempList = new ValueDropdownList<string>();
                foreach (var package in AssetBundleCollectorSettingData.Setting.Packages)
                {
                        tempList.Add(package.PackageName);
                }
                return tempList;
        }

#endif

        #endregion
    }
}