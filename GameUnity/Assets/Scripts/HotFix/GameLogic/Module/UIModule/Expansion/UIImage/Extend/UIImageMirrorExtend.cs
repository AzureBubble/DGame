using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameLogic
{
    [Serializable]
    public class UIImageMirrorExtend
    {
#pragma warning disable 0414

        [SerializeField] private bool m_isUseImageMirror;
        /// <summary>
        /// 镜像类型
        /// </summary>
        [SerializeField]
        private UIMirrorEffect.MirrorType m_mirrorType = UIMirrorEffect.MirrorType.Horizontal;

        [FormerlySerializedAs("m_mirror")] [SerializeField] private UIMirrorEffect uiMirrorEffect;

#pragma warning disable 0414

        public void SaveSerializeData(UIImage uiImage)
        {
            if(!uiImage.TryGetComponent(out uiMirrorEffect))
            {
                uiMirrorEffect = uiImage.gameObject.AddComponent<UIMirrorEffect>();
                uiMirrorEffect.hideFlags = HideFlags.HideInInspector;
            }
        }

        public void SetMirrorType(UIMirrorEffect.MirrorType mirrorType)
        {
            if (uiMirrorEffect != null)
            {
                uiMirrorEffect.mirrorType = mirrorType;
            }
        }
    }
}