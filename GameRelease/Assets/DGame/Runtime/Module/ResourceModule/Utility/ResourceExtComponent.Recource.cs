using Cysharp.Threading.Tasks;

namespace DGame
{
    internal partial class ResourceExtComponent
    {
        private static IResourceModule m_resourceModule;
        private LoadAssetCallbacks m_loadAssetCallbacks;

        public static IResourceModule ResourceModule => m_resourceModule;

        private void InitializedResources()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            m_loadAssetCallbacks = new LoadAssetCallbacks(OnLoadAssetSuccess, OnLoadAssetFailure);
        }

        private void OnLoadAssetFailure(string assetName, LoadResourceStatus status, string errormessage, object userdata)
        {
            m_loadingAssetList.Remove(assetName);
            Debugger.Error("加载资源失败 '{0}' 错误信息： '{1}'.", assetName, errormessage);
        }

        private void OnLoadAssetSuccess(string assetName, object asset, float duration, object userdata)
        {
            m_loadingAssetList.Remove(assetName);
            ISetAssetObject setAssetObject = (ISetAssetObject)userdata;
            UnityEngine.Object assetObject = asset as UnityEngine.Object;
            if (assetObject != null)
            {
                m_assetItemPool.Register(AssetItemObject.Create(setAssetObject.Location, assetObject), true);
                SetAsset(setAssetObject, assetObject);
            }
            else
            {
                Debugger.Error($"加载资源失败 资源类型： {asset.GetType()}.");
            }
        }

        /// <summary>
        /// 通过资源系统设置资源。
        /// </summary>
        /// <param name="setAssetObject">需要设置的对象。</param>
        public async UniTaskVoid SetAssetByResources<T>(ISetAssetObject setAssetObject) where T : UnityEngine.Object
        {
            await TryWaitingLoading(setAssetObject.Location);

            if (m_assetItemPool.CanSpawn(setAssetObject.Location))
            {
                var assetObject = (T)m_assetItemPool.Spawn(setAssetObject.Location).Target;
                SetAsset(setAssetObject, assetObject);
            }
            else
            {
                m_loadingAssetList.Add(setAssetObject.Location);
                m_resourceModule.LoadAssetAsync(setAssetObject.Location, typeof(T), 0, m_loadAssetCallbacks, setAssetObject);
            }
        }
    }
}