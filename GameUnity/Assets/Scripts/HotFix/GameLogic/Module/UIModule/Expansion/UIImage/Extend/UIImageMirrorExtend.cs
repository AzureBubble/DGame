using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        private Image m_image;

        public bool isUseImageMirror
        {
            get => m_isUseImageMirror;
            set
            {
                if (m_isUseImageMirror == value)
                {
                    return;
                }
                m_isUseImageMirror = value;
#if UNITY_EDITOR

                if (m_image != null)
                {
                    if (value)
                    {
                        if (!m_image.TryGetComponent(out uiMirrorEffect))
                        {
                            uiMirrorEffect = m_image.gameObject.AddComponent<UIMirrorEffect>();
                            uiMirrorEffect.hideFlags = HideFlags.HideInInspector;
                        }
                    }
                    else
                    {
                        if (uiMirrorEffect != null || m_image.TryGetComponent(out uiMirrorEffect))
                        {
                            GameObject.DestroyImmediate(uiMirrorEffect);
                        }
                        uiMirrorEffect = null;
                    }
                }
#endif
                Refresh();
            }
        }

#pragma warning disable 0414

        public void SaveSerializeData(UIImage uiImage)
        {
            m_image = uiImage;
            if(!isUseImageMirror) return;
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

        public void Refresh()
        {
            m_image?.SetVerticesDirty();
        }
    }
}