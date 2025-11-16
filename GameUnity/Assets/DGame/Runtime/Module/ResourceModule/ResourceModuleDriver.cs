using System;
using System.Collections;
using System.Collections.Generic;
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

        public string PackageVersion { get; set; }

        [SerializeField]
        private string packageName = "DefaultPackage";

        public string PackageName { get => packageName; set => packageName = value; }

        [SerializeField]
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
        private EncryptionType encryptionType = EncryptionType.None;
        public EncryptionType EncryptionType => encryptionType;

        [SerializeField]
        private bool updatableWhilePlaying;
        public bool UpdatableWhilePlaying { get => m_resourceModule.UpdatableWhilePlaying; set => m_resourceModule.UpdatableWhilePlaying = updatableWhilePlaying = value; }

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
        public int DownloadingMaxNum { get => downloadingMaxNum; set => downloadingMaxNum = value; }

        [SerializeField]
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
        private float assetAutoReleaseInterval = 60f;
        public float AssetAutoReleaseInterval { get => m_resourceModule.AssetAutoReleaseInterval; set => m_resourceModule.AssetAutoReleaseInterval = assetAutoReleaseInterval = value; }
        [SerializeField]
        private int assetPoolCapacity = 64;
        public int AssetCapacity { get => m_resourceModule.AssetPoolCapacity; set => m_resourceModule.AssetPoolCapacity = assetPoolCapacity = value; }
        [SerializeField]
        private float assetExpireTime = 60f;
        public float AssetExpireTime { get => m_resourceModule.AssetExpireTime; set => m_resourceModule.AssetExpireTime = assetExpireTime = value; }
        [SerializeField]
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
            m_resourceModule.AutoUnloadBundleWhenUnused = autoUnloadBundleWhenUnused;
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
        }

        #endregion
    }
}