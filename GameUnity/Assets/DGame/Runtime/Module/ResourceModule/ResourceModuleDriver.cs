using System;
using UnityEngine;
using YooAsset;

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
        private float minUnloadUnusedAssetsInterval = 60f;
        public float MinUnloadUnusedAssetsInterval { get => minUnloadUnusedAssetsInterval; set => minUnloadUnusedAssetsInterval = value; }

        [SerializeField]
        private float maxUnloadUnusedAssetsInterval = 300f;
        public float MaxUnloadUnusedAssetsInterval { get => maxUnloadUnusedAssetsInterval; set => maxUnloadUnusedAssetsInterval = value; }

        [SerializeField]
        private bool useSystemUnloadUnusedAssets = true;
        public bool UseSystemUnloadUnusedAssets { get => useSystemUnloadUnusedAssets; set => useSystemUnloadUnusedAssets = value; }

        /// <summary>
        /// 当前最新的包裹版本
        /// </summary>
        public string PackageVersion { get; set; }

        [SerializeField]
        private string packageName = "DefaultPackage";

        /// <summary>
        /// 资源包名称。
        /// </summary>
        public string PackageName { get => packageName; set => packageName = value; }

        [SerializeField]
        private EPlayMode playMode = EPlayMode.EditorSimulateMode;

        /// <summary>
        /// 资源系统运行模式
        /// <remarks>编辑器内优先使用</remarks>
        /// </summary>
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
        private EncryptionType encryptionType = EncryptionType.None;

        /// <summary>
        /// 资源模块的加密类型
        /// </summary>
        public EncryptionType EncryptionType => encryptionType;

        [SerializeField]
        public bool updatableWhilePlaying;

        /// <summary>
        /// 是否支持边玩边下载
        /// </summary>
        public bool UpdatableWhilePlaying => updatableWhilePlaying;

        /// <summary>
        /// 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        [SerializeField]
        public long milliseconds = 30;

        /// <summary>
        /// 自动释放资源引用计数为0的资源包
        /// </summary>
        [SerializeField]
        public bool autoUnloadBundleWhenUnused = false;

        [SerializeField]
        private int downloadingMaxNum = 10;

        /// <summary>
        /// 同时最大下载数目
        /// </summary>
        public int DownloadingMaxNum { get => downloadingMaxNum; set => downloadingMaxNum = value; }

        [SerializeField]
        private int failedTryAgain = 3;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int FailedTryAgain { get => failedTryAgain; set => failedTryAgain = value; }

        /// <summary>
        /// 获取当前资源适用的游戏版本号
        /// </summary>
        public string ApplicableGameVersion => m_resourceModule?.ApplicableGameVersion;

        /// <summary>
        /// 获取当前内部资源版本号
        /// </summary>
        public int InternalResourceVersion => m_resourceModule.InternalResourceVersion;

        [SerializeField]
        private float assetAutoReleaseInterval = 60f;

        /// <summary>
        /// 资源对象池自动释放可释放对象的间隔秒数
        /// </summary>
        public float AssetAutoReleaseInterval
        {
            get => m_resourceModule.AssetPoolAutoReleaseInterval;
            set => m_resourceModule.AssetPoolAutoReleaseInterval = assetAutoReleaseInterval = value;
        }
        [SerializeField]
        private int assetPoolCapacity = 64;

        /// <summary>
        /// 资源对象池的容量
        /// </summary>
        public int AssetCapacity
        {
            get => m_resourceModule.AssetPoolCapacity;
            set => m_resourceModule.AssetPoolCapacity = assetPoolCapacity = value;
        }
        [SerializeField]
        private float assetExpireTime = 60f;

        /// <summary>
        /// 资源对象池对象过期秒数
        /// </summary>
        public float AssetExpireTime
        {
            get => m_resourceModule.AssetExpireTime;
            set => m_resourceModule.AssetExpireTime = assetExpireTime = value;
        }
        [SerializeField]
        private int assetPoolPriority;

        /// <summary>
        /// 资源对象池的优先级
        /// </summary>
        public int AssetPriority
        {
            get => m_resourceModule.AssetPoolPriority;
            set => m_resourceModule.AssetPoolPriority = assetPoolPriority = value;
        }

        private void Start()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            if (m_resourceModule == null)
            {
                DLogger.Fatal("资源模块无效！");
                return;
            }

            if (PlayMode == EPlayMode.EditorSimulateMode)
            {
                DLogger.Info("在此模式下运行，资源模块会优先使用编辑器资源，应该首先验证这些资源是否有效");
#if !UNITY_EDITOR
                PlayMode = EPlayMode.OfflinePlayMode;
#endif
            }

            m_resourceModule.DefaultPackageName = packageName;
            m_resourceModule.PlayMode = PlayMode;
            m_resourceModule.EncryptionType = EncryptionType;
            m_resourceModule.Milliseconds = milliseconds;
            m_resourceModule.AutoUnloadBundleWhenUnused = autoUnloadBundleWhenUnused;
            m_resourceModule.SetRemoteServerURL(
                Settings.UpdateSettings.GetResDownloadPath(),
                Settings.UpdateSettings.GetFallbackResDownloadPath());
            // m_resourceModule.HostServerURL = Settings.UpdateSettings.GetResDownloadPath();
            // m_resourceModule.FallbackHostServerURL = Settings.UpdateSettings.GetFallbackResDownloadPath();
            m_resourceModule.LoadResWayWebGL = Settings.UpdateSettings.GetLoadResWayWebGL();
            m_resourceModule.DownloadingMaxNum = DownloadingMaxNum;
            m_resourceModule.FailedTryAgainNum = FailedTryAgain;
            m_resourceModule.UpdatableWhilePlaying = UpdatableWhilePlaying;
            m_resourceModule.Initialize();
            m_resourceModule.AssetPoolAutoReleaseInterval = assetAutoReleaseInterval;
            m_resourceModule.AssetPoolCapacity = assetPoolCapacity;
            m_resourceModule.AssetExpireTime = assetExpireTime;
            m_resourceModule.AssetPoolPriority = assetPoolPriority;
            m_resourceModule.SetForceUnloadUnusedAssetsAction(ForceUnloadUnusedAssets);
            DLogger.Info($"======== 资源加载模式: {PlayMode} ========");
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
                m_performGCCollect = true;
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
                DLogger.Info("======== 自动卸载释放没有使用的资源 ========");
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
                    DLogger.Info("======== GC.Collect ========");
                    m_performGCCollect = false;
                    GC.Collect();
                }
            }
        }

        #endregion
    }
}