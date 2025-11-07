using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace DGame
{
    internal partial class ResourceModule : Module, IResourceModule
    {
        private string m_applicableGameVersion;
        public string ApplicableGameVersion => m_applicableGameVersion;
        private int m_internalResourceVersion;
        public int InternalResourceVersion => m_internalResourceVersion;
        public EPlayMode PlayMode { get; set; }  = EPlayMode.OfflinePlayMode;
        public EncryptionType EncryptionType { get; set; } = EncryptionType.None;
        public bool UpdatableWhilePlaying { get; set; }
        public int DownloadingMaxNum { get; set; }
        public int FailedTryAgainCnt { get; set; }
        public string DefaultPackageName { get; set; } = "DefaultPackage";
        public long Milliseconds { get; set; } = 30;
        public string HostServerURl { get; set; }
        public string FallbackHostServerURL { get; set; }
        public LoadResWayWebGL LoadResWayWebGL { get; set; }
        public string PackageVersion { get; set; }
        public ResourceDownloaderOperation Downloader { get; set; }

        #region internal

        /// <summary>
        /// 默认资源包
        /// </summary>
        internal ResourcePackage DefaultPackage { get; private set; }

        /// <summary>
        /// 资源包Dict key->packageName
        /// </summary>
        private Dictionary<string, ResourcePackage> m_packagesMap { get; } = new Dictionary<string, ResourcePackage>();

        /// <summary>
        /// 资源信息Dict key->资源名称
        /// </summary>
        private readonly Dictionary<string, AssetInfo> m_assetInfosMap = new Dictionary<string, AssetInfo>();

        /// <summary>
        /// 正在加载的资源列表
        /// </summary>
        private readonly HashSet<string> m_loadingAssetList = new HashSet<string>();

        #endregion

        public override int Priority => 4;

        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
        }

        public void Initialize()
        {
            // YooAsset初始化
            YooAssets.Initialize(new ResourceLogger());
            YooAssets.SetOperationSystemMaxTimeSlice(Milliseconds);

            // 创建默认资源包
            DefaultPackage = YooAssets.TryGetPackage(DefaultPackageName);
            if (DefaultPackage == null)
            {
                DefaultPackage = YooAssets.CreatePackage(DefaultPackageName);
                YooAssets.SetDefaultPackage(DefaultPackage);
            }

            IObjectPoolModule objectPoolManager = ModuleSystem.GetModule<IObjectPoolModule>();
            SetObjectPoolModule(objectPoolManager);
        }

        public async UniTask<InitializationOperation> InitPackage(string customPackageName)
        {
#if UNITY_EDITOR

            // 编辑器模式下
            EPlayMode playMode = (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorPlayMode");
            Debugger.Warning($"======== 编辑器模式下使用的资源加载模式：{playMode} ========");

#else
            EPlayMode playMode = (EPlayMode)PlayMode;
#endif
            if (m_packagesMap.TryGetValue(customPackageName, out var resourcePackage))
            {
                if (resourcePackage.InitializeStatus is EOperationStatus.Processing or EOperationStatus.Succeed)
                {
                    Debugger.Error($"资源系统已经初始化过资源包：{customPackageName}");
                    return null;
                }
                else
                {
                    m_packagesMap.Remove(customPackageName);
                }
            }

            var package = YooAssets.TryGetPackage(customPackageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(customPackageName);
            }
            m_packagesMap.Remove(customPackageName);
            // 编辑器模式下
            InitializationOperation initOperation = null;
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                var buildResult = EditorSimulateModeHelper.SimulateBuild(customPackageName);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParam = new EditorSimulateModeParameters();
                createParam.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                initOperation = package.InitializeAsync(createParam);
            }

            IDecryptionServices decryptionServices = CreateDecryptionServices();

            // 单机运行模式
            if (playMode == EPlayMode.OfflinePlayMode)
            {
                var createParam = new OfflinePlayModeParameters();
                createParam.BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices);
                initOperation = package.InitializeAsync(createParam);
            }

            // 联机运行模式
            if (playMode == EPlayMode.HostPlayMode)
            {
                IRemoteServices remoteServices = new RemoteServices(HostServerURl, FallbackHostServerURL);
                var createParam = new HostPlayModeParameters();
                createParam.BuildinFileSystemParameters =
                    FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices);
                createParam.CacheFileSystemParameters =
                    FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices, decryptionServices);
                initOperation = package.InitializeAsync(createParam);
            }

            // WebGL运行模式
            if (playMode == EPlayMode.WebPlayMode)
            {
                var createParam = new WebPlayModeParameters();
                IWebDecryptionServices webDecryptionServices = CreateWebDecryptionServices();
                IRemoteServices remoteServices = new RemoteServices(HostServerURl, FallbackHostServerURL);

#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                Debugger.Info("======================= WEIXINMINIGAME =======================");
                // 注意：如果有子目录，请修改此处！
                string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE";
                createParam.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices, webDecryptionServices);
#else
                Debugger.Info("======================= UNITY_WEBGL =======================");
                if (LoadResWayWebGL == LoadResWayWebGL.Remote)
                {
                    createParam.WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(remoteServices, webDecryptionServices);
                }
                createParam.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(webDecryptionServices);
#endif
                initOperation = package.InitializeAsync(createParam);
            }

            await initOperation.ToUniTask();
            Debugger.Info($"======== 初始化资源包版本：{initOperation?.Status} ========");
            return initOperation;
        }

        /// <summary>
        /// 创建解密服务。
        /// </summary>
        public IDecryptionServices CreateDecryptionServices()
        {
            return EncryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetDecryption(),
                EncryptionType.FileStream => new FileStreamDecryption(),
                _ => null
            };
        }

        /// <summary>
        /// 创建WebGL解密服务。
        /// </summary>
        private IWebDecryptionServices CreateWebDecryptionServices()
        {
            return EncryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetWebDecryption(),
                EncryptionType.FileStream => new FileStreamWebDecryption(),
                _ => null
            };
        }

        #region GetPacketVersion

        public string GetPacketVersion(string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);

            return package == null ? string.Empty : package.GetPackageVersion();
        }

        public RequestPackageVersionOperation RequestPackageVersionAsync(bool appendTimeTicks = false, int timeout = 60,
            string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);
            return package?.RequestPackageVersionAsync(appendTimeTicks, timeout);
        }

        #endregion

        public void SetRemoteServerURL(string defaultRemoteServerURL, string fallbackHostServerURL)
        {
            HostServerURl = defaultRemoteServerURL;
            FallbackHostServerURL = fallbackHostServerURL;
        }

        public UpdatePackageManifestOperation UpdatePackageManifestAsync(string packageVersion, int timeout = 60,
            string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);
            return package?.UpdatePackageManifestAsync(packageVersion, timeout);
        }

        public ResourceDownloaderOperation CreateResourceDownloader(string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);
            Downloader = package?.CreateResourceDownloader(DownloadingMaxNum, FailedTryAgainCnt);
            return Downloader;
        }

        public ClearCacheFilesOperation ClearCacheFilesAsync(EFileClearMode clearMode = EFileClearMode.ClearUnusedBundleFiles,
            string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);
            return package?.ClearCacheFilesAsync(clearMode);
        }

        public void ClearAllBundleFiles(string customPackageName = "")
        {
            var package = string.IsNullOrEmpty(customPackageName)
                ? YooAssets.GetPackage(DefaultPackageName)
                : YooAssets.GetPackage(customPackageName);
            package?.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
        }

        #region 资源回收

        private Action<bool> m_forceUnloadUnusedAssetsAction;

        public void OnLowMemory()
        {
            Debugger.Warning("Low memory reported...");
            m_forceUnloadUnusedAssetsAction?.Invoke(true);
        }

        public void SetForceUnloadUnusedAssetsAction(Action<bool> action)
        {
            m_forceUnloadUnusedAssetsAction = action;
        }

        public void UnloadUnusedAssets()
        {
            m_assetObjectPool.ReleaseAllUnusedToMemoryPool();

            foreach (var package in m_packagesMap.Values)
            {
                if (package != null && package.InitializeStatus == EOperationStatus.Succeed)
                {
                    package.UnloadUnusedAssetsAsync();
                }
            }
        }

        public void ForceUnloadAllAssets()
        {
            
#if UNITY_WEBGL
            Debugger.Warning($"WebGL 不支持 {nameof(ForceUnloadAllAssets)}");
			return;
#else
            foreach (var package in m_packagesMap.Values)
            {
                if (package != null && package.InitializeStatus == EOperationStatus.Succeed)
                {
                    package.UnloadAllAssetsAsync();
                }
            }
#endif
        }

        public void ForceUnloadUnusedAssets(bool performGCCollect)
        {
            m_forceUnloadUnusedAssetsAction?.Invoke(performGCCollect);
        }

        #endregion

        #region GetAssetInfos

        public AssetInfo[] GetAssetInfos(string resTag, string packageName = "")
        {
            return string.IsNullOrEmpty(packageName)
                ? YooAssets.GetAssetInfos(resTag)
                :  YooAssets.GetPackage(packageName)?.GetAssetInfos(resTag);
        }

        public AssetInfo[] GetAssetInfos(string[] tags, string packageName = "")
        {
            return string.IsNullOrEmpty(packageName)
                ? YooAssets.GetAssetInfos(tags)
                :  YooAssets.GetPackage(packageName)?.GetAssetInfos(tags);
        }

        public AssetInfo GetAssetInfo(string location, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效的");
            }

            if (string.IsNullOrEmpty(packageName))
            {
                if (m_assetInfosMap.TryGetValue(location, out AssetInfo assetInfo))
                {
                    return assetInfo;
                }

                assetInfo = YooAssets.GetAssetInfo(location);
                m_assetInfosMap[location] = assetInfo;
                return assetInfo;
            }
            else
            {
                string key = $"{packageName}/{location}";

                if (m_assetInfosMap.TryGetValue(key, out AssetInfo assetInfo))
                {
                    return assetInfo;
                }
                var package = YooAssets.GetPackage(packageName);

                if (package == null)
                {
                    throw new DataException($"资源包不存在 资源包名: {packageName}");
                }
                assetInfo = package.GetAssetInfo(location);
                m_assetInfosMap[location] = assetInfo;
                return assetInfo;
            }
        }

        #endregion

        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="packageName">资源包名称</param>
        public bool IsNeedDownloadFromRemote(string location, string packageName = "")
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return YooAssets.IsNeedDownloadFromRemote(location);
            }
            else
            {
                return YooAssets.GetPackage(packageName).IsNeedDownloadFromRemote(location);
            }
        }

        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="packageName">资源包名称</param>
        public bool IsNeedDownloadFromRemote(AssetInfo assetInfo, string packageName = "")
        {
            if (string.IsNullOrEmpty(packageName))
            {
                return YooAssets.IsNeedDownloadFromRemote(assetInfo);
            }
            else
            {
                return YooAssets.GetPackage(packageName).IsNeedDownloadFromRemote(assetInfo);
            }
        }

        public CheckAssetStatus CheckAssetIsExist(string location, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效的");
            }
            AssetInfo assetInfo = GetAssetInfo(location, packageName);

            if (!CheckLocationValid(location))
            {
                return CheckAssetStatus.Invalid;
            }

            if (assetInfo == null)
            {
                return CheckAssetStatus.NotExist;
            }

            if (IsNeedDownloadFromRemote(assetInfo))
            {
                return CheckAssetStatus.AssetOnline;
            }

            return CheckAssetStatus.AssetOnDisk;
        }

        public bool CheckLocationValid(string location, string packageName = "")
        {
            return string.IsNullOrEmpty(packageName)
                ? YooAssets.CheckLocationValid(location)
                : YooAssets.GetPackage(packageName).CheckLocationValid(location);
        }

        #region LoadAssetAsync

        public async void LoadAssetAsync(string location, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData,
            string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                Debugger.Error("资源地址无效");
                return;
            }
            if (loadAssetCallbacks == null)
            {
                throw new DGameException("资源加载回调函数无效");
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            float duration = Time.time;
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                await UniTask.Yield();
                loadAssetCallbacks.LoadAssetSuccessCallback(location, assetObject.Target, Time.time - duration, userData);
                return;
            }

            m_loadingAssetList.Add(assetObjectKey);
            AssetInfo assetInfo = GetAssetInfo(location, packageName);

            if (!string.IsNullOrEmpty(assetInfo.Error))
            {
                m_loadingAssetList.Remove(assetObjectKey);
                string errorMsg = Utility.StringUtil.Format("无法加载资源{0}，因为：{1}", location, assetInfo.Error);

                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotExist, errorMsg, userData);
                    return;
                }

                throw new DGameException(errorMsg);
            }

            AssetHandle handle = GetAssetHandleAsync(location, assetInfo.AssetType, packageName);

            if (loadAssetCallbacks.LoadAssetUpdateCallback != null)
            {
                InvokeProgress(location, handle, loadAssetCallbacks.LoadAssetUpdateCallback, userData).Forget();
            }

            await handle.ToUniTask();

            if (handle.AssetObject == null || handle.Status == EOperationStatus.Failed)
            {
                m_loadingAssetList.Remove(assetObjectKey);
                string errorMsg = Utility.StringUtil.Format("无法加载资源对象：{0}", location);

                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                    return;
                }
                throw new DataException(errorMsg);
            }
            else
            {
                assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                m_loadingAssetList.Remove(assetObjectKey);

                if (loadAssetCallbacks.LoadAssetSuccessCallback != null)
                {
                    duration = Time.time - duration;
                    loadAssetCallbacks.LoadAssetSuccessCallback?.Invoke(location, handle.AssetObject, duration, userData);
                }
            }
        }

        public async void LoadAssetAsync(string location, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                Debugger.Error("资源地址无效");
                return;
            }
            if (loadAssetCallbacks == null)
            {
                throw new DGameException("资源加载回调函数无效");
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            float duration = Time.time;
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                await UniTask.Yield();
                loadAssetCallbacks.LoadAssetSuccessCallback(location, assetObject.Target, Time.time - duration, userData);
                return;
            }

            m_loadingAssetList.Add(assetObjectKey);
            AssetInfo assetInfo = GetAssetInfo(location, packageName);

            if (!string.IsNullOrEmpty(assetInfo.Error))
            {
                m_loadingAssetList.Remove(assetObjectKey);
                string errorMsg = Utility.StringUtil.Format("无法加载资源{0}，因为：{1}", location, assetInfo.Error);

                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotExist, errorMsg, userData);
                    return;
                }

                throw new DGameException(errorMsg);
            }

            AssetHandle handle = GetAssetHandleAsync(location, assetType, packageName);

            if (loadAssetCallbacks.LoadAssetUpdateCallback != null)
            {
                InvokeProgress(location, handle, loadAssetCallbacks.LoadAssetUpdateCallback, userData).Forget();
            }

            await handle.ToUniTask();

            if (handle.AssetObject == null || handle.Status == EOperationStatus.Failed)
            {
                m_loadingAssetList.Remove(assetObjectKey);
                string errorMsg = Utility.StringUtil.Format("无法加载资源对象：{0}", location);

                if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                    return;
                }
                throw new DataException(errorMsg);
            }
            else
            {
                assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                m_loadingAssetList.Remove(assetObjectKey);

                if (loadAssetCallbacks.LoadAssetSuccessCallback != null)
                {
                    duration = Time.time - duration;
                    loadAssetCallbacks.LoadAssetSuccessCallback?.Invoke(location, handle.AssetObject, duration, userData);
                }
            }
        }

        private async UniTaskVoid InvokeProgress(string location, AssetHandle handle, LoadAssetUpdateCallback loadAssetUpdateCallback, object userData)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DataException("资源地址无效");
            }

            if (loadAssetUpdateCallback != null)
            {
                while (handle != null && handle.IsValid && !handle.IsDone)
                {
                    await UniTask.Yield();
                    loadAssetUpdateCallback?.Invoke(location, handle.Progress, userData);
                }
            }
        }

        public async UniTaskVoid LoadAssetAsync<T>(string location, Action<T> callback, string packageName = "") where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                Debugger.Error("资源地址无效");
                return;
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                await UniTask.Yield();
                callback?.Invoke(assetObject.Target as T);
                return;
            }
            m_loadingAssetList.Add(assetObjectKey);
            AssetHandle handle = GetAssetHandleAsync<T>(location, packageName);
            handle.Completed += assetHandle =>
            {
                m_loadingAssetList.Remove(assetObjectKey);

                if (assetHandle.AssetObject != null)
                {
                    assetObject = AssetObject.Spawn(assetObjectKey, assetHandle.AssetObject, assetHandle, this);
                    m_assetObjectPool.Register(assetObject, true);
                    callback?.Invoke(assetObject.Target as T);
                }
                else
                {
                    callback?.Invoke(null);
                }
            };
        }

        public async UniTask<T> LoadAssetAsync<T>(string location, CancellationToken cancellationToken = default, string packageName = "") where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效");
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                await UniTask.Yield();
                return assetObject.Target as T;
            }
            m_loadingAssetList.Add(assetObjectKey);
            AssetHandle handle = GetAssetHandleAsync<T>(location, packageName);
            bool cancelOrError = await handle.ToUniTask().AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (cancelOrError)
            {
                m_loadingAssetList.Remove(assetObjectKey);
                return null;
            }
            assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            m_loadingAssetList.Remove(assetObjectKey);
            return handle.AssetObject as T;
        }

        public async UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null, CancellationToken cancellationToken = default,
            string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效");
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                await UniTask.Yield();
                return AssetReference.Instantiate(assetObject.Target as GameObject, parent, this).gameObject;
            }
            m_loadingAssetList.Add(assetObjectKey);
            AssetHandle handle = GetAssetHandleAsync<GameObject>(location, packageName);
            bool cancelOrFailed = await handle.ToUniTask().AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();

            if (cancelOrFailed)
            {
                m_loadingAssetList.Remove(assetObjectKey);
                return null;
            }
            GameObject go = AssetReference.Instantiate(handle.AssetObject as GameObject, parent, this).gameObject;
            assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            m_loadingAssetList.Remove(assetObjectKey);
            return go;
        }

        private readonly TimeoutController m_timeoutController = new TimeoutController();

        private async UniTask TryWaitingLoading(string assetObjectKey)
        {
            if (m_loadingAssetList.Contains(assetObjectKey))
            {
                try
                {
                    await UniTask.WaitUntil(() => !m_loadingAssetList.Contains(assetObjectKey))
#if UNITY_EDITOR
                        .AttachExternalCancellation(m_timeoutController.Timeout(TimeSpan.FromSeconds(60)));
                    m_timeoutController.Reset();
#else
                    ;
#endif
                }
                catch (OperationCanceledException ex)
                {
                    if (m_timeoutController.IsTimeout())
                    {
                        Debugger.Error($"异步等待加载资源超时： {assetObjectKey} msg:{ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region LoadAssetSync

        public T LoadAssetSync<T>(string location, string packageName = "") where T : Object
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效的");
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                return assetObject.Target as T;
            }
            AssetHandle handle = GetAssetHandleSync<T>(location, packageName);
            T ret = handle.AssetObject as T;
            assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            return ret;
        }

        #endregion

        #region LoadGameObject

        public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException("资源地址无效的");
            }
            string assetObjectKey = GetCacheKey(location, packageName);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                return AssetReference.Instantiate(assetObject.Target as GameObject, parent, this).gameObject;
            }

            AssetHandle handle = GetAssetHandleSync<GameObject>(location, packageName);
            GameObject go = AssetReference.Instantiate(handle.AssetObject as GameObject, parent, this).gameObject;
            assetObject = AssetObject.Spawn(assetObjectKey, handle.AssetObject, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            return go;
        }

        #endregion

        #region LoadAssetSyncHandle

        public AssetHandle LoadAssetSyncHandle<T>(string location, string packageName = "") where T : Object
        {
            return GetAssetHandleSync<T>(location, packageName);
        }

        public AssetHandle LoadAssetAsyncHandle<T>(string location, string packageName = "") where T : Object
        {
            return GetAssetHandleAsync<T>(location, packageName);
        }

        private AssetHandle GetAssetHandleSync<T>(string location, string packageName = "")
        {
            return GetAssetHandleSync(location, typeof(T), packageName);
        }

        private AssetHandle GetAssetHandleSync(string location, Type assetType, string packageName = "")
        {
            return string.IsNullOrEmpty(packageName)
                ? YooAssets.LoadAssetSync(location, assetType)
                : YooAssets.GetPackage(packageName)?.LoadAssetSync(location, assetType);
        }

        private AssetHandle GetAssetHandleAsync<T>(string location, string packageName = "")
        {
            return GetAssetHandleAsync(location, typeof(T), packageName);
        }

        private AssetHandle GetAssetHandleAsync(string location, Type assetType, string packageName = "")
        {
            return string.IsNullOrEmpty(packageName)
                ? YooAssets.LoadAssetAsync(location, assetType)
                : YooAssets.GetPackage(packageName)?.LoadAssetAsync(location, assetType);
        }

        #endregion

        private string GetCacheKey(string location, string packageName = "")
        {
            if (string.IsNullOrEmpty(packageName) || packageName.Equals(DefaultPackageName))
            {
                return location;
            }
            return $"{packageName}/{location}";
        }
    }
}