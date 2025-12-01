using YooAsset;

namespace DGame
{
    internal partial class ResourceModule
    {
        /// <summary>
        /// 资源对象
        /// </summary>
        private sealed class AssetObject : BasePoolObject
        {
            /// <summary>
            /// 资源句柄
            /// </summary>
            private AssetHandle m_assetHandle = null;
            private ResourceModule m_resourceModule;

            public static AssetObject Spawn(string name, object target, object assetHandle, ResourceModule resourceModule)
            {
                if (assetHandle == null)
                {
                    throw new DGameException("资源句柄无效");
                }

                if (resourceModule == null)
                {
                    throw new DGameException("资源管理器无效");
                }

                AssetObject assetObject = MemoryPool.Spawn<AssetObject>();
                assetObject.Initialize(name, target);
                assetObject.m_assetHandle = (AssetHandle)assetHandle;
                assetObject.m_resourceModule = resourceModule;
                return assetObject;
            }

            public override void OnRecycleToMemoryPool()
            {
                base.OnRecycleToMemoryPool();
                m_assetHandle = null;
            }

            protected internal override void ReleaseObj(bool isDestroy)
            {
                if (!isDestroy)
                {
                    AssetHandle assetHandle = m_assetHandle;

                    if (assetHandle != null && assetHandle.IsValid)
                    {
                        assetHandle.Dispose();
                    }

                    m_assetHandle = null;
                }
            }
        }
    }
}