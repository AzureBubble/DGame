using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public sealed class UIFrameAnimatorAgent : IMemory
    {
        #region 字段

        private const float FRAME_INTERVAL = 0.125f; // 1秒8帧
        private const float FRAME_TIMER_INTERVAL = FRAME_INTERVAL * 0.25f * 0.5f; // 提高八倍采样率
        private const float NORMAL_BASE_SPEED = 1.5f; // 1秒12帧
        private const float ELITE_BASE_SPEED = 1.5f; // 1秒12帧

        private GameTimer m_gameTimer;
        private FrameSpritePool m_frameSpritePool;
        private Image m_image;
        private bool m_isInit;
        private FrameAnimName m_curFrameAnimName = FrameAnimName.idle;
        private FrameAnimName m_changeFrameAnimName = FrameAnimName.idle;
        private FrameAnimName m_deathFrameAnimName = FrameAnimName.death;
        private string m_curCfgLocation;
        private bool m_isBindDisplayImage;
        private float m_deathSpeed = 1.0f;
        private Vector3 m_scale;
        private bool m_isSetFirstFrame;
        private bool m_isUnscaledTime;

        private float m_speedScale = 1.0f;
        private float m_curBaseSpeed;
        private bool m_isDestroy;

        public bool IsValid => !m_isDestroy || m_isInit || m_image == null;

        #endregion

        public static UIFrameAnimatorAgent Create()
        {
            UIFrameAnimatorAgent agent = new UIFrameAnimatorAgent();
            return agent;
        }

        public async UniTaskVoid Init(string location)
        {
            m_curCfgLocation = location;
            var isGot = await FrameSpriteMgr.Instance.GetFrameSpritePool(location, m_frameSpritePool);
            if (!isGot)
            {
                DLogger.Error($"没有找到帧动画配置文件: {location}");
            }

            m_curBaseSpeed = NORMAL_BASE_SPEED;
            m_isBindDisplayImage = false;

            if (m_isDestroy)
            {
                return;
            }
            m_isInit = true;
            SetFirstFrame();
        }

        public void SetUnscaledTime(bool isUnscaledTime)
        {
            m_isUnscaledTime = isUnscaledTime;
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

        public void StartAnim()
        {
            if (!IsValid)
            {
                return;
            }

            if (m_isUnscaledTime)
            {
                if (GameTimer.IsNull(m_gameTimer))
                {
                    m_gameTimer = GameModule.GameTimerModule.CreateUnscaledLoopGameTimer(FRAME_TIMER_INTERVAL, Update);
                }
            }
            else
            {
                if (GameTimer.IsNull(m_gameTimer))
                {
                    m_gameTimer = GameModule.GameTimerModule.CreateLoopGameTimer(FRAME_TIMER_INTERVAL, Update);
                }
            }
        }

        private void Update(object[] args)
        {
            if (!IsValid)
            {
                return;
            }
        }

        public float GetSpeed()
        {
            if (m_curFrameAnimName == FrameAnimName.run || m_curFrameAnimName == FrameAnimName.walk
                || m_curFrameAnimName == FrameAnimName.run1 || m_curFrameAnimName == FrameAnimName.walk1
                || m_curFrameAnimName == FrameAnimName.run2 || m_curFrameAnimName == FrameAnimName.walk2
                || m_curFrameAnimName == FrameAnimName.run3 || m_curFrameAnimName == FrameAnimName.walk3
                || m_curFrameAnimName == FrameAnimName.run4 || m_curFrameAnimName == FrameAnimName.walk4
                || m_curFrameAnimName == FrameAnimName.run5 || m_curFrameAnimName == FrameAnimName.walk5)
            {
                return m_curBaseSpeed;
            }

            if (m_curFrameAnimName == FrameAnimName.death || m_curFrameAnimName == FrameAnimName.death1
                || m_curFrameAnimName == FrameAnimName.death2 || m_curFrameAnimName == FrameAnimName.death3
                || m_curFrameAnimName == FrameAnimName.death4 || m_curFrameAnimName == FrameAnimName.death5)
            {
                return m_deathSpeed;
            }

            return m_speedScale * m_curBaseSpeed;
        }

        public void SwitchAnim(FrameAnimName animName)
        {
            if (!IsValid)
            {
                return;
            }

            if (m_curFrameAnimName != animName)
            {

            }
        }

        public bool IsLoopAnim(FrameAnimName animName)
        {
            return animName == FrameAnimName.idle || animName == FrameAnimName.run || animName == FrameAnimName.walk
                || animName == FrameAnimName.idle1 || animName == FrameAnimName.run1 || animName == FrameAnimName.walk1
                || animName == FrameAnimName.idle2 || animName == FrameAnimName.run2 || animName == FrameAnimName.walk2
                || animName == FrameAnimName.idle3 || animName == FrameAnimName.run3 || animName == FrameAnimName.walk3
                || animName == FrameAnimName.idle4 || animName == FrameAnimName.run4 || animName == FrameAnimName.walk4
                || animName == FrameAnimName.idle5 || animName == FrameAnimName.run5 || animName == FrameAnimName.walk5;
        }

        public void SetAnimSpeed(float speed)
        {
            m_speedScale = speed;
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
            GameModule.GameTimerModule.DestroyGameTimer(m_gameTimer);
            m_isInit = false;
            m_isDestroy = true;
            m_frameSpritePool = null;
        }

        #endregion
    }
}