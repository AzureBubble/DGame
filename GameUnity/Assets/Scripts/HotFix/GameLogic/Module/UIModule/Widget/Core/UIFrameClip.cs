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

        /// <summary>
        /// 构造帧动画片段
        /// </summary>
        /// <param name="animName">动画状态名称</param>
        /// <param name="sprites">精灵帧列表</param>
        /// <param name="isLoop">是否循环播放</param>
        public UIFrameClip(UIFrameAnimState animName, List<Sprite> sprites, bool isLoop)
        {
            m_animName = animName;
            m_sprites = sprites;
            m_curIndex = 0;
            m_cacheCount = sprites != null ? sprites.Count : 0;
            m_isLoop = isLoop;
        }

        /// <summary>
        /// 获取下一帧精灵
        /// </summary>
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

        /// <summary>
        /// 是否已停止播放（非循环动画播放完毕）
        /// </summary>
        public bool IsStop()
            => !m_isLoop && m_curIndex >= m_cacheCount;

        /// <summary>
        /// 离开动画，重置播放位置
        /// </summary>
        public void Leave() => m_curIndex = 0;

        /// <summary>
        /// 销毁动画片段，释放资源
        /// </summary>
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