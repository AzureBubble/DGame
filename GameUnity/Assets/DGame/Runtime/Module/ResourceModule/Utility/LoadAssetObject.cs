#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR

using Sirenix.OdinInspector;

#endif

using UnityEngine;

namespace DGame
{
    [SerializeField]
    public class LoadAssetObject
    {

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public ISetAssetObject assetObject { get; }

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public Object assetTarget { get; }

#if UNITY_EDITOR
        public bool isSelect { get; }
#endif


        public LoadAssetObject(ISetAssetObject assetObject, Object assetTarget)
        {
            this.assetObject = assetObject;
            this.assetTarget = assetTarget;
        }
    }
}