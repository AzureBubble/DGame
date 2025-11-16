namespace DGame
{
    internal partial class ResourceModule
    {
        private IObjectPool<AssetObject> m_assetObjectPool;

        /// <summary>
        /// 资源对象池自动释放可释放对象的时间间隔(秒)。
        /// </summary>
        public float AssetAutoReleaseInterval
        {
            get => m_assetObjectPool.AutoReleaseInterval;
            set => m_assetObjectPool.AutoReleaseInterval = value;
        }

        public int AssetPoolCapacity { get => m_assetObjectPool.Capacity; set => m_assetObjectPool.Capacity = value; }
        public int AssetPoolPriority { get => m_assetObjectPool.Priority; set => m_assetObjectPool.Priority = value; }
        public float AssetExpireTime {get => m_assetObjectPool.ExpireTime; set => m_assetObjectPool.ExpireTime = value; }

        public void UnloadAsset(object asset)
        {
            if (m_assetObjectPool != null)
            {
                m_assetObjectPool.Recycle(asset);
            }
        }

        public void SetObjectPoolModule(IObjectPoolModule poolModule)
        {
            if (poolModule == null)
            {
                throw new DGameException("对象池管理器无效");
            }

            m_assetObjectPool = poolModule.CreateMultiSpawnObjectPool<AssetObject>("Asset Object Pool");
        }
    }
}