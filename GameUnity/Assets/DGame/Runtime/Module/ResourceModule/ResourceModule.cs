using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace DGame
{
    internal sealed partial class ResourceModule : Module, IResourceModule
    {
        #region Properties

        private string m_applicableGameVersion;
        public string ApplicableGameVersion => m_applicableGameVersion;
        private int m_internalResourceVersion;
        public int InternalResourceVersion => m_internalResourceVersion;
        public EPlayMode PlayMode { get; set; }  = EPlayMode.OfflinePlayMode;
        public EncryptionType EncryptionType { get; set; } = EncryptionType.None;
        public bool UpdatableWhilePlaying { get; set; }
        public int DownloadingMaxNum { get; set; }
        public int FailedTryAgainNum { get; set; }
        public string DefaultPackageName { get; set; } = "DefaultPackage";
        public long Milliseconds { get; set; } = 30;
        public bool AutoUnloadBundleWhenUnused { get; set; } = false;
        public string HostServerURL { get; set; }
        public string FallbackHostServerURL { get; set; }
        public LoadResWayWebGL LoadResWayWebGL { get; set; }
        public string PackageVersion { get; set; }
        public ResourceDownloaderOperation Downloader { get; set; }

        #endregion

        #region internal

        /// <summary>
        /// 默认资源包
        /// </summary>
        private ResourcePackage DefaultPackage { get; set; }

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

        #region Override

        public override int Priority => 4;

        public override void OnCreate() { }

        public override void OnDestroy() { }

        #endregion

        #region Initialize

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

        public async UniTask<InitializationOperation> InitPackage(string packageName, bool needInitMainFest = false)
        {
#if UNITY_EDITOR

            // 编辑器模式下
            EPlayMode playMode = (EPlayMode)UnityEditor.EditorPrefs.GetInt("EditorPlayMode");
            DLogger.Warning($"======== 编辑器模式下使用的资源加载模式：{playMode} ========");

#else
            EPlayMode playMode = (EPlayMode)PlayMode;
#endif
            if (m_packagesMap.TryGetValue(packageName, out var resourcePackage))
            {
                if (resourcePackage.InitializeStatus is EOperationStatus.Processing or EOperationStatus.Succeed)
                {
                    DLogger.Error($"资源系统已经初始化过资源包：{packageName}");
                    return null;
                }
                // 没初始化成功，但是存在缓存字典中，就删除重新进行初始化
                m_packagesMap.Remove(packageName);
            }

            // 创建资源包类 并缓存
            var package = YooAssets.TryGetPackage(packageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(packageName);
            }
            m_packagesMap[packageName] = package;

            InitializationOperation initOperation = null;
            // 创建解密服务
            IDecryptionServices decryptionServices = CreateDecryptionServices();

            switch (playMode)
            {
                case EPlayMode.EditorSimulateMode:
                    // 编辑器模式下
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                    var editorPackageRoot = buildResult.PackageRootDirectory;
                    var editorCreateParam = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(editorPackageRoot),
                        AutoUnloadBundleWhenUnused = AutoUnloadBundleWhenUnused
                    };
                    initOperation = package.InitializeAsync(editorCreateParam);
                    break;

                case EPlayMode.OfflinePlayMode:
                    // 单机运行模式
                    var offlineCreateParam = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices),
                        AutoUnloadBundleWhenUnused = AutoUnloadBundleWhenUnused
                    };
                    initOperation = package.InitializeAsync(offlineCreateParam);
                    break;

                case EPlayMode.HostPlayMode:
                    // 联机运行模式
                    IRemoteServices hostPlayRemoteServices = new RemoteServices(HostServerURL, FallbackHostServerURL);
                    var hostPlayCreateParam = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(decryptionServices),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(hostPlayRemoteServices, decryptionServices),
                        AutoUnloadBundleWhenUnused = AutoUnloadBundleWhenUnused
                    };
                    initOperation = package.InitializeAsync(hostPlayCreateParam);
                    break;

                case EPlayMode.WebPlayMode:
                    // WebGL运行模式
                    var webGLCreateParam = new WebPlayModeParameters();
                    IWebDecryptionServices webDecryptionServices = CreateWebDecryptionServices();
                    IRemoteServices webGLRemoteServices = new RemoteServices(HostServerURL, FallbackHostServerURL);

#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                DLogger.Info("======================= WEIXINMINIGAME =======================");
                // 注意：如果有子目录，请修改此处！
                string webGLPackageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE";
                webGLCreateParam.WebServerFileSystemParameters =
                    WechatFileSystemCreater.CreateFileSystemParameters(webGLPackageRoot, webGLRemoteServices, webDecryptionServices);
#else
                    DLogger.Info("======================= UNITY_WEBGL =======================");
                    if (LoadResWayWebGL == LoadResWayWebGL.Remote)
                    {
                        webGLCreateParam.WebRemoteFileSystemParameters = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(webGLRemoteServices, webDecryptionServices);
                    }
                    webGLCreateParam.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters(webDecryptionServices);
#endif
                    webGLCreateParam.AutoUnloadBundleWhenUnused = AutoUnloadBundleWhenUnused;
                    initOperation = package.InitializeAsync(webGLCreateParam);
                    break;
            }

            await initOperation.ToUniTask();
            DLogger.Info($"======== 初始化资源包版本：{initOperation?.Status} ========");

            if (needInitMainFest)
            {
                // 请求资源清单的版本信息
                var requestPackageVersionOperation = package.RequestPackageVersionAsync();
                await requestPackageVersionOperation;
                if (requestPackageVersionOperation.Status == EOperationStatus.Succeed)
                {
                    // 传入资源版本信息更新资源清单
                    var updatePackageManifestAsync = package.UpdatePackageManifestAsync(requestPackageVersionOperation.PackageVersion);
                    await updatePackageManifestAsync;

                    if (updatePackageManifestAsync.Status == EOperationStatus.Failed)
                    {
                        DLogger.Fatal($"更新资源清单失败: {updatePackageManifestAsync.Status}");
                    }
                }
                else
                {
                    DLogger.Fatal($"请求资源清单的版本信息失败: {requestPackageVersionOperation.Status}");
                }
            }

            return initOperation;
        }

        #endregion

        #region DecryptionServices

        /// <summary>
        /// 创建解密服务
        /// </summary>
        private IDecryptionServices CreateDecryptionServices()
            => EncryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetDecryption(),
                EncryptionType.FileStream => new FileStreamDecryption(),
                _ => null
            };

        /// <summary>
        /// 创建WebGL解密服务。
        /// </summary>
        private IWebDecryptionServices CreateWebDecryptionServices()
            => EncryptionType switch
            {
                EncryptionType.FileOffset => new FileOffsetWebDecryption(),
                EncryptionType.FileStream => new FileStreamWebDecryption(),
                _ => null
            };

        #endregion

        #region Other

        public void SetRemoteServerURL(string defaultRemoteServerURL, string fallbackHostServerURL)
        {
            HostServerURL = defaultRemoteServerURL;
            FallbackHostServerURL = fallbackHostServerURL;
        }

        private string GetCacheKey(string location, string packageName = "")
        {
            if (string.IsNullOrEmpty(packageName) || packageName.Equals(DefaultPackageName))
            {
                return location;
            }
            return $"{packageName}/{location}";
        }

        #endregion

        #region GetPackageVersion And UpdatePackageManifest And CreateResourceDownloader

        public string GetPackageVersion(string customPackageName = "")
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
            return Downloader = package?.CreateResourceDownloader(DownloadingMaxNum, FailedTryAgainNum);
        }

        #endregion

        #region Clear

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
            ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles, customPackageName);
        }

        #endregion

        #region 资源回收

        private Action<bool> m_forceUnloadUnusedAssetsAction;

        public void OnLowMemory()
        {
            DLogger.Warning("Low memory reported...");
            m_forceUnloadUnusedAssetsAction?.Invoke(true);
        }

        public void SetForceUnloadUnusedAssetsAction(Action<bool> action)
        {
            m_forceUnloadUnusedAssetsAction = action;
        }

        public void UnloadUnusedAssets()
        {
            m_assetObjectPool.ReleaseAllUnused();

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
            DLogger.Warning($"WebGL 不支持 {nameof(ForceUnloadAllAssets)}");
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
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.GetAssetInfos(resTag)
                : YooAssets.GetPackage(packageName)?.GetAssetInfos(resTag);

        public AssetInfo[] GetAssetInfos(string[] tags, string packageName = "")
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.GetAssetInfos(tags)
                : YooAssets.GetPackage(packageName)?.GetAssetInfos(tags);

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
                string key = GetCacheKey(location, packageName);

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
                m_assetInfosMap[key] = assetInfo;
                return assetInfo;
            }
        }

        #endregion

        #region Check

        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="packageName">资源包名称</param>
        public bool IsNeedDownloadFromRemote(string location, string packageName = "")
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.IsNeedDownloadFromRemote(location)
                : YooAssets.GetPackage(packageName).IsNeedDownloadFromRemote(location);

        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="packageName">资源包名称</param>
        public bool IsNeedDownloadFromRemote(AssetInfo assetInfo, string packageName = "")
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.IsNeedDownloadFromRemote(assetInfo)
                : YooAssets.GetPackage(packageName).IsNeedDownloadFromRemote(assetInfo);

        public CheckAssetStatus ContainsAsset(string location, string packageName = "")
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
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.CheckLocationValid(location)
                : YooAssets.GetPackage(packageName).CheckLocationValid(location);

        #endregion

        #region LoadAssetAsync

        public async void LoadAssetAsync(string location, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData, string packageName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(location))
                {
                    throw new DGameException($"资源地址无效: [{location}]");
                }

                if (loadAssetCallbacks == null)
                {
                    throw new DGameException("资源加载回调函数无效");
                }

                if (!CheckLocationValid(location, packageName))
                {
                    string errorMsg = $"资源地址无效: [{location}]";
                    DLogger.Error(errorMsg);
                    if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        loadAssetCallbacks.LoadAssetFailureCallback(location, LoadResourceStatus.NotExist, errorMsg, userData);
                    }
                    return;
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

                await handle;

                if (handle.AssetObject == null || handle.Status == EOperationStatus.Failed)
                {
                    handle?.Dispose();
                    m_loadingAssetList.Remove(assetObjectKey);
                    string errorMsg = Utility.StringUtil.Format("无法加载资源对象：{0}", location);

                    if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                        return;
                    }
                    throw new DataException(errorMsg);
                }

                assetObject = AssetObject.Create(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                m_loadingAssetList.Remove(assetObjectKey);

                if (loadAssetCallbacks.LoadAssetSuccessCallback != null)
                {
                    loadAssetCallbacks.LoadAssetSuccessCallback?.Invoke(location, handle.AssetObject, Time.time - duration, userData);
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"异步加载资源失败: {e}";
                DLogger.Error(errorMsg);
                if (loadAssetCallbacks != null && loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                }
            }
        }

        public async void LoadAssetAsync(string location, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData, string packageName = "")
        {
            try
            {
                if (string.IsNullOrEmpty(location))
                {
                    throw new DGameException($"资源地址无效: [{location}]");
                }

                if (loadAssetCallbacks == null)
                {
                    throw new DGameException("资源加载回调函数无效");
                }

                if (!CheckLocationValid(location, packageName))
                {
                    string errorMsg = $"资源地址无效: [{location}]";
                    DLogger.Error(errorMsg);
                    if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        loadAssetCallbacks.LoadAssetFailureCallback(location, LoadResourceStatus.NotExist, errorMsg, userData);
                    }
                    return;
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
                    string errorMsg = Utility.StringUtil.Format("无法加载资源 [{0}]，原因: '{1}'", location, assetInfo.Error);

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

                await handle;

                if (handle.AssetObject == null || handle.Status == EOperationStatus.Failed)
                {
                    handle?.Dispose();
                    m_loadingAssetList.Remove(assetObjectKey);
                    string errorMsg = Utility.StringUtil.Format("无法加载资源对象：{0}", location);

                    if (loadAssetCallbacks.LoadAssetFailureCallback != null)
                    {
                        loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                        return;
                    }
                    throw new DataException(errorMsg);
                }

                assetObject = AssetObject.Create(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                m_loadingAssetList.Remove(assetObjectKey);

                if (loadAssetCallbacks.LoadAssetSuccessCallback != null)
                {
                    loadAssetCallbacks.LoadAssetSuccessCallback?.Invoke(location, handle.AssetObject, Time.time - duration, userData);
                }
            }
            catch (Exception e)
            {
                string errorMsg = $"异步加载资源失败: {e}";
                DLogger.Error(errorMsg);
                if (loadAssetCallbacks != null && loadAssetCallbacks.LoadAssetFailureCallback != null)
                {
                    loadAssetCallbacks.LoadAssetFailureCallback?.Invoke(location, LoadResourceStatus.NotReady, errorMsg, userData);
                }
            }
        }

        public async UniTaskVoid LoadAssetAsync<T>(string location, Action<T> callback, string packageName = "")
            where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(location))
            {
                DLogger.Error($"资源地址无效: [{location}]");
                return;
            }

            if (!CheckLocationValid(location, packageName))
            {
                DLogger.Error($"资源地址无效: [{location}]");
                callback?.Invoke(null);
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
                    assetObject = AssetObject.Create(assetObjectKey, assetHandle.AssetObject, assetHandle, this);
                    m_assetObjectPool.Register(assetObject, true);
                    callback?.Invoke(assetObject.Target as T);
                }
                else
                {
                    callback?.Invoke(null);
                }
            };
        }

        public async UniTask<T> LoadAssetAsync<T>(string location, CancellationToken cancellationToken = default,
            string packageName = "") where T : UnityEngine.Object
            => await LoadAssetAsync(location, typeof(T), cancellationToken, packageName) as T;

        public async UniTask<UnityEngine.Object> LoadAssetAsync(
            string location, Type assetType, CancellationToken cancellationToken = default, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException($"资源地址无效: [{location}]");
            }

            if (!CheckLocationValid(location, packageName))
            {
                DLogger.Error($"资源地址无效: [{location}]");
                return null;
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            await TryWaitingLoading(assetObjectKey);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);
            if (assetObject != null)
            {
                await UniTask.Yield();
                return assetObject.Target as UnityEngine.Object;
            }
            m_loadingAssetList.Add(assetObjectKey);
            AssetHandle handle = null;

            try
            {
                handle = GetAssetHandleAsync(location, assetType, packageName);
                bool cancelOrError = await handle.ToUniTask(cancellationToken: cancellationToken)
                    .AttachExternalCancellation(cancellationToken)
                    .SuppressCancellationThrow();

                if (cancelOrError)
                {
                    handle?.Dispose();
                    m_loadingAssetList.Remove(assetObjectKey);
                    return null;
                }
                assetObject = AssetObject.Create(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                return handle.AssetObject;
            }
            catch (Exception e)
            {
                handle?.Dispose();
                DLogger.Error($"加载资源失败: {location}, 错误: {e.Message}");
                return null;
            }
            finally
            {
                m_loadingAssetList.Remove(assetObjectKey);
            }
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
                        DLogger.Error($"异步等待加载资源超时： {assetObjectKey} msg:{ex.Message}");
                    }
                }
            }
        }

        private async UniTaskVoid InvokeProgress(string location, AssetHandle handle,
            LoadAssetUpdateCallback loadAssetUpdateCallback, object userData)
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DataException($"资源地址无效 [{location}]");
            }

            if (loadAssetUpdateCallback != null)
            {
                while (handle != null && handle.IsValid && !handle.IsDone)
                {
                    await UniTask.Yield();
                    loadAssetUpdateCallback.Invoke(location, handle.Progress, userData);
                }
            }
        }

        #endregion

        #region LoadAssetSync

        public T LoadAsset<T>(string location, string packageName = "") where T : UnityEngine.Object
            => LoadAsset(location, typeof(T), packageName) as T;

        public UnityEngine.Object LoadAsset(string location, Type assetType, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException($"资源地址无效的: [{location}]");
            }

            if (!CheckLocationValid(location, packageName))
            {
                DLogger.Error($"资源地址无效的: [{location}]");
                return null;
            }

            string assetObjectKey = GetCacheKey(location, packageName);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                return assetObject.Target as UnityEngine.Object;
            }
            AssetHandle handle = GetAssetHandleSync(location, assetType, packageName);
            var ret = handle.AssetObject;
            assetObject = AssetObject.Create(assetObjectKey, ret, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            return ret;
        }

        #endregion

        #region LoadGameObject

        public async UniTask<GameObject> LoadGameObjectAsync(string location, Transform parent = null,
            CancellationToken cancellationToken = default, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException($"资源地址无效: [{location}]");
            }

            if (!CheckLocationValid(location, packageName))
            {
                DLogger.Error($"资源地址无效: [{location}]");
                return null;
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
            AssetHandle handle = null;
            try
            {
                handle = GetAssetHandleAsync<GameObject>(location, packageName);
                bool cancelOrFailed = await handle.ToUniTask(cancellationToken: cancellationToken)
                    .AttachExternalCancellation(cancellationToken).SuppressCancellationThrow();

                if (cancelOrFailed)
                {
                    handle?.Dispose();
                    m_loadingAssetList.Remove(assetObjectKey);
                    return null;
                }
                GameObject go = AssetReference.Instantiate(handle.AssetObject as GameObject, parent, this).gameObject;
                assetObject = AssetObject.Create(assetObjectKey, handle.AssetObject, handle, this);
                m_assetObjectPool.Register(assetObject, true);
                return go;
            }
            catch (Exception e)
            {
                handle?.Dispose();
                DLogger.Error($"加载资源失败: {location}, 错误: {e.Message}");
                return null;
            }
            finally
            {
                m_loadingAssetList.Remove(assetObjectKey);
            }
        }

        public GameObject LoadGameObject(string location, Transform parent = null, string packageName = "")
        {
            if (string.IsNullOrEmpty(location))
            {
                throw new DGameException($"资源地址无效: [{location}]");
            }

            if (!CheckLocationValid(location, packageName))
            {
                DLogger.Error($"资源地址无效: [{location}]");
                return null;
            }
            string assetObjectKey = GetCacheKey(location, packageName);
            AssetObject assetObject = m_assetObjectPool.Spawn(assetObjectKey);

            if (assetObject != null)
            {
                return AssetReference.Instantiate(assetObject.Target as GameObject, parent, this).gameObject;
            }

            AssetHandle handle = GetAssetHandleSync<GameObject>(location, packageName);
            GameObject go = AssetReference.Instantiate(handle.AssetObject as GameObject, parent, this).gameObject;
            assetObject = AssetObject.Create(assetObjectKey, handle.AssetObject, handle, this);
            m_assetObjectPool.Register(assetObject, true);
            return go;
        }

        #endregion

        #region LoadAssetSyncHandle

        public AssetHandle LoadAssetSyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object
            => GetAssetHandleSync<T>(location, packageName);

        public AssetHandle LoadAssetSyncHandle(string location, Type assetType, string packageName = "")
        => GetAssetHandleSync(location, assetType, packageName);

        public AssetHandle LoadAssetAsyncHandle<T>(string location, string packageName = "") where T : UnityEngine.Object
            => GetAssetHandleAsync<T>(location, packageName);

        public AssetHandle LoadAssetAsyncHandle(string location, Type assetType, string packageName = "")
            => GetAssetHandleAsync(location, assetType, packageName);

        private AssetHandle GetAssetHandleSync<T>(string location, string packageName = "")
            => GetAssetHandleSync(location, typeof(T), packageName);

        private AssetHandle GetAssetHandleSync(string location, Type assetType, string packageName = "")
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.LoadAssetSync(location, assetType)
                : YooAssets.GetPackage(packageName)?.LoadAssetSync(location, assetType);

        private AssetHandle GetAssetHandleAsync<T>(string location, string packageName = "")
            => GetAssetHandleAsync(location, typeof(T), packageName);

        private AssetHandle GetAssetHandleAsync(string location, Type assetType, string packageName = "")
            => string.IsNullOrEmpty(packageName)
                ? YooAssets.LoadAssetAsync(location, assetType)
                : YooAssets.GetPackage(packageName)?.LoadAssetAsync(location, assetType);

        #endregion
    }
}