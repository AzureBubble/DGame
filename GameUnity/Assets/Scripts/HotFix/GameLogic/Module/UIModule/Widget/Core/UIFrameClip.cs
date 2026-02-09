using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public sealed class UIFrameClip
    {
        private UIFrameAnimState m_animName;
        private List<Sprite> m_sprites;
        private int m_curIndex;
        private int m_cacheCount;
        private bool m_isLoop;

        public UIFrameClip(UIFrameAnimState animName, List<Sprite> sprites, bool isLoop)
        {
            m_animName = animName;
            m_sprites = sprites;
            m_curIndex = 0;
            m_cacheCount = sprites != null ? sprites.Count : 0;
            m_isLoop = isLoop;
        }

        public Sprite GetNext()
        {
            if (m_cacheCount <= 0)
            {
                return null;
            }

            if (m_isLoop)
            {
                m_curIndex %= m_cacheCount;
            }
            else
            {
                m_curIndex = Mathf.Min(m_curIndex, m_cacheCount - 1);
            }

            return m_sprites[m_curIndex++];
        }

        public bool IsStop()
            => !m_isLoop && m_curIndex >= m_cacheCount;

        public void Leave() => m_curIndex = 0;

        public void OnDestroy()
        {
            m_animName = UIFrameAnimState.Max;
            m_sprites = null;
            m_isLoop = false;
            m_cacheCount = 0;
            m_curIndex = 0;
        }
    }
}