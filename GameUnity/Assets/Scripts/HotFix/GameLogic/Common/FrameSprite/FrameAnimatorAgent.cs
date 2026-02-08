using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed class FrameAnimatorAgent : IMemory
    {
        #region 字段

        private FrameSpritePool m_frameSpritePool;
        private Image m_image;
        private bool m_isInit;
        private FrameAnimName m_curFrameAnimName = FrameAnimName.idle;
        private FrameAnimName m_changeFrameAnimName = FrameAnimName.idle;
        private string m_curCfgLocation;
        private bool m_isBindDisplayImage;
        private Vector3 m_scale;
        private bool m_isSetFirstFrame;

        #endregion

        public static FrameAnimatorAgent Create()
        {
            FrameAnimatorAgent agent = new FrameAnimatorAgent();
            return agent;
        }

        public async void Init(string location)
        {
            m_curCfgLocation = location;
        }

        public void BindDisplayRender(Image image)
        {
            if (m_isBindDisplayImage)
            {
                return;
            }
            m_isBindDisplayImage = true;
            m_image = image;
            m_scale = Vector3.one;
        }

        private void SetFirstFrame()
        {
            if (!m_isInit)
            {
                if (m_image != null)
                {
                    m_image.sprite = null;
                }
                return;
            }

            if (m_isSetFirstFrame)
            {
                return;
            }

            m_isSetFirstFrame = true;
            m_curFrameAnimName = m_changeFrameAnimName;
        }

        #region 释放资源

        /// <summary>
        /// 主动释放
        /// </summary>
        public void Release()
        {
            MemoryPool.Release(this);
        }

        /// <summary>
        /// 释放资源回调
        /// </summary>
        public void OnRelease()
        {
            m_frameSpritePool = null;
        }

        #endregion
    }
}