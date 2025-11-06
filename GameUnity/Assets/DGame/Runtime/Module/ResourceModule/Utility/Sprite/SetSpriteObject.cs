#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR

using Sirenix.OdinInspector;

#endif

using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DGame
{
    [SerializeField]
    public class SetSpriteObject : ISetAssetObject
    {
        enum SetType
        {
            None,
            Image,
            SpriteRender,
        }

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        private SetType setType;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        private Image image;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        private SpriteRenderer spriteRenderer;

#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ShowInInspector]
#endif
        private UnityEngine.Sprite sprite;

        public string Location { get; private set; }

        private bool m_setNativeSize = false;
        Action<Image> m_imageCallback;
        Action<SpriteRenderer> m_spriteRendererCallback;
        private CancellationToken m_cancellationToken;

        public void OnSpawnFromMemoryPool()
        {
        }

        public void OnRecycleToMemoryPool()
        {
            spriteRenderer = null;
            image = null;
            Location = null;
            setType = SetType.None;
            m_setNativeSize = true;
            sprite = null;
        }


        public void SetAsset(Object asset)
        {
            sprite = asset as UnityEngine.Sprite;

            if (m_cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (image != null)
            {
                image.sprite = sprite;

                if (m_setNativeSize)
                {
                    image.SetNativeSize();
                }
                m_imageCallback?.Invoke(image);
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                m_spriteRendererCallback?.Invoke(spriteRenderer);
            }
        }

        public bool IsCanRelease()
        {
            if (setType == SetType.Image)
            {
                return image == null || image.sprite == null || (sprite != null && image.sprite != sprite);
            }
            else if(setType == SetType.SpriteRender)
            {
                return spriteRenderer == null || spriteRenderer.sprite == null || (sprite != null && spriteRenderer.sprite != sprite);
            }
            return true;
        }

        public static SetSpriteObject Create(Image image, string location, bool isNativeSize, Action<Image> imageCallback = null, CancellationToken ct = default)
        {
            SetSpriteObject setSpriteObject = MemoryPool.Spawn<SetSpriteObject>();
            setSpriteObject.Location = location;
            setSpriteObject.m_setNativeSize = isNativeSize;
            setSpriteObject.m_imageCallback = imageCallback;
            setSpriteObject.setType = SetType.Image;
            setSpriteObject.image = image;
            setSpriteObject.m_cancellationToken = ct;
            return setSpriteObject;
        }

        public static SetSpriteObject Create(SpriteRenderer spriteRenderer, string location, Action<SpriteRenderer> imageCallback = null, CancellationToken ct = default)
        {
            SetSpriteObject setSpriteObject = MemoryPool.Spawn<SetSpriteObject>();
            setSpriteObject.Location = location;
            setSpriteObject.m_spriteRendererCallback = imageCallback;
            setSpriteObject.setType = SetType.SpriteRender;
            setSpriteObject.spriteRenderer = spriteRenderer;
            setSpriteObject.m_cancellationToken = ct;
            return setSpriteObject;
        }
    }
}