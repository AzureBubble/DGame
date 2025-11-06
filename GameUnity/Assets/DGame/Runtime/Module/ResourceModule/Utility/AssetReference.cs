using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DGame
{
    [SerializeField]
    public struct AssetRefInfo
    {
        public int instanceID;
        public Object refAsset;

        public AssetRefInfo(Object asset)
        {
            refAsset = asset;
            instanceID = refAsset.GetInstanceID();
        }
    }

    [DisallowMultipleComponent]
    public class AssetReference : MonoBehaviour
    {
        [SerializeField]
        private GameObject sourceGameObject;
        [SerializeField]
        private List<AssetRefInfo> refInfos = new List<AssetRefInfo>();
        private static IResourceModule m_resourceModule;
        private static Dictionary<GameObject, AssetReference> m_originalRefs = new Dictionary<GameObject, AssetReference>();

        private void CheckInit()
        {
            if (m_resourceModule != null)
            {
                return;
            }
            else
            {
                m_resourceModule = ModuleSystem.GetModule<IResourceModule>();
            }

            if (m_resourceModule == null)
            {
                throw new DGameException("资源管理器无效");
            }
        }

        private void CheckRelease()
        {
            if (sourceGameObject != null)
            {
                m_resourceModule?.UnloadAsset(sourceGameObject);
            }
            else
            {
                Debugger.Warning("游戏对象无效");
            }
        }

        private void Awake()
        {
            if (!IsOriginalInstance())
            {
                ClearCloneReferences();
            }
        }

        private bool IsOriginalInstance()
        {
            return m_originalRefs.TryGetValue(gameObject, out AssetReference reference) && reference == this;
        }

        private void ClearCloneReferences()
        {
            sourceGameObject = null;
            refInfos?.Clear();
        }

        private void OnDestroy()
        {
            CheckInit();

            if (sourceGameObject != null)
            {
                CheckRelease();
            }

            ReleaseRefAssetInfos();
        }

        private void ReleaseRefAssetInfos()
        {
            if (refInfos != null)
            {
                for (int i = 0; i < refInfos.Count; i++)
                {
                    m_resourceModule?.UnloadAsset(refInfos[i].refAsset);
                }
                refInfos.Clear();
            }
        }

        public AssetReference Ref(GameObject source, IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            m_resourceModule = resourceModule;
            sourceGameObject = source;

            if (!m_originalRefs.ContainsKey(gameObject))
            {
                m_originalRefs.Add(gameObject, this);
            }
            return this;
        }

        public AssetReference Ref<T>(T source, IResourceModule resourceModule = null) where T : Object
        {
            if (source == null)
            {
                throw new DGameException("资源是无效的");
            }

            m_resourceModule = resourceModule;

            if (refInfos == null)
            {
                refInfos = new List<AssetRefInfo>();
            }

            refInfos.Add(new AssetRefInfo(source));
            return this;
        }

        internal static AssetReference Instantiate(GameObject source, Transform parent = null, IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            var instance = Object.Instantiate(source, parent);
            return instance.AddComponent<AssetReference>().Ref(source, resourceModule);
        }

        public static AssetReference Ref(GameObject source, GameObject instance, IResourceModule resourceModule = null)
        {
            if (source == null)
            {
                throw new DGameException("游戏对象是无效的");
            }

            if (source.scene.name != null)
            {
                throw new DGameException("游戏对象已经存在此场景中");
            }

            var com = instance.GetComponent<AssetReference>();
            return com != null ? com.Ref(source, resourceModule) : instance.AddComponent<AssetReference>().Ref(source, resourceModule);
        }


        public static AssetReference Ref<T>(T source, GameObject instance, IResourceModule resourceModule = null) where T : Object
        {
            if (source == null)
            {
                throw new DGameException("资源对象是无效的");
            }

            var com = instance.GetComponent<AssetReference>();
            return com != null ? com.Ref(source, resourceModule) : instance.AddComponent<AssetReference>().Ref(source, resourceModule);
        }
    }
}