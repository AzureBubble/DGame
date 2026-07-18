using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧动画播放片段。
    /// </summary>
    public sealed class FrameClip
    {
        /// <summary>
        /// 当前片段持有的帧图片列表。
        /// </summary>
        private List<Sprite> m_sprites;

        /// <summary>
        /// 当前即将播放的帧索引。
        /// </summary>
        private int m_curIndex;

        /// <summary>
        /// 缓存的帧图片数量。
        /// </summary>
        private int m_cacheCount;

        /// <summary>
        /// 当前片段是否循环播放。
        /// </summary>
        private bool m_isLoop;

        /// <summary>
        /// 初始化帧动画播放片段。
        /// </summary>
        /// <param name="animName">动画资源名称。</param>
        /// <param name="sprites">动画帧图片列表。</param>
        /// <param name="isLoop">是否循环播放。</param>
        public FrameClip(FrameAnimName animName, List<Sprite> sprites, bool isLoop)
        {
            m_sprites = sprites;
            m_curIndex = 0;
            m_cacheCount = sprites != null ? sprites.Count : 0;
            m_isLoop = isLoop;
        }

        /// <summary>
        /// 获取下一帧图片。
        /// </summary>
        /// <returns>下一帧图片；没有可用帧时返回null。</returns>
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
        /// 判断非循环动画是否已经播放结束。
        /// </summary>
        /// <returns>true表示已经播放结束。</returns>
        public bool IsStop()
            => !m_isLoop && m_curIndex >= m_cacheCount;

        /// <summary>
        /// 随机设置初始帧索引。
        /// </summary>
        /// <param name="random">随机数生成器。</param>
        public void RandomSetInitIndex(System.Random random)
        {
            if (m_cacheCount <= 0 || random == null)
            {
                return;
            }

            m_curIndex = random.Next(0, m_cacheCount);
        }

        /// <summary>
        /// 离开当前动画时重置播放索引。
        /// </summary>
        public void Leave() => m_curIndex = 0;

        /// <summary>
        /// 销毁并清理片段缓存。
        /// </summary>
        public void OnDestroy()
        {
            m_sprites = null;
            m_isLoop = false;
            m_cacheCount = 0;
            m_curIndex = 0;
        }
    }
}
