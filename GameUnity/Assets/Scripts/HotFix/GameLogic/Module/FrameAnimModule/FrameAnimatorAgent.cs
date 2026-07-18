using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using GameProto;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 场景帧动画播放状态。
    /// </summary>
    public enum FrameAnimState
    {
        /// <summary>
        /// 待机状态。
        /// </summary>
        Idle,
        /// <summary>
        /// 移动状态。
        /// </summary>
        Move,
        /// <summary>
        /// 死亡状态。
        /// </summary>
        Death,
        /// <summary>
        /// 技能1状态。
        /// </summary>
        Skill1,
        /// <summary>
        /// 技能2状态。
        /// </summary>
        Skill2,
        /// <summary>
        /// 受击1状态。
        /// </summary>
        Hurt1,
        /// <summary>
        /// 受击2状态。
        /// </summary>
        Hurt2,
        /// <summary>
        /// 状态数量上限。
        /// </summary>
        Max
    }

    /// <summary>
    /// 场景帧动画参数定义。
    /// </summary>
    public static class FrameAnimParamDefine
    {
        /// <summary>
        /// 移动状态参数。
        /// </summary>
        public static readonly int Moving = Animator.StringToHash("Moving");
        /// <summary>
        /// 技能索引参数。
        /// </summary>
        public static readonly int SkillIndex = Animator.StringToHash("SkillIndex");
        /// <summary>
        /// 受击表现参数。
        /// </summary>
        public static readonly int ImpactId = Animator.StringToHash("ImpactId");
        /// <summary>
        /// 死亡状态参数。
        /// </summary>
        public static readonly int Death = Animator.StringToHash("Death");
        /// <summary>
        /// 移动速度缩放参数。
        /// </summary>
        public static readonly int MoveSpeedScale = Animator.StringToHash("MoveSpeed");
        /// <summary>
        /// 技能速度缩放参数。
        /// </summary>
        public static readonly int SkillSpeedScale = Animator.StringToHash("SkillSpeed");
        /// <summary>
        /// 显示状态参数。
        /// </summary>
        public static readonly int Show = Animator.StringToHash("Show");
        /// <summary>
        /// 受击动画索引参数。
        /// </summary>
        public static readonly int HurtIndex = Animator.StringToHash("HurtIndex");
    }

    /// <summary>
    /// 场景帧动画播放代理。
    /// </summary>
    public sealed class FrameAnimatorAgent : MemoryObject
    {
        #region 字段

        /// <summary>
        /// 基础帧间隔，默认一秒八帧。
        /// </summary>
        private const float FRAME_INTERVAL = 0.125f; // 1秒8帧

        /// <summary>
        /// 普通模型基础播放速度。
        /// </summary>
        private const float NORMAL_BASE_SPEED = 1.5f; // 1秒12帧

        /// <summary>
        /// 精英模型基础播放速度。
        /// </summary>
        private const float ELITE_BASE_SPEED = 1.5f; // 1秒12帧

        /// <summary>
        /// 移动动画初始帧随机数生成器。
        /// </summary>
        private static readonly System.Random s_random = new System.Random();

        /// <summary>
        /// 当前使用的帧动画资源池。
        /// </summary>
        private FrameSpritePool m_frameSpritePool;

        /// <summary>
        /// 场景模型显示用的精灵渲染器。
        /// </summary>
        private SpriteRenderer m_spriteRenderer;

        /// <summary>
        /// 是否已经完成资源初始化。
        /// </summary>
        private bool m_isInit;

        /// <summary>
        /// 当前播放的帧动画状态。
        /// </summary>
        private FrameAnimState m_curFrameAnimName = FrameAnimState.Idle;

        /// <summary>
        /// 初始化完成前缓存的目标帧动画状态。
        /// </summary>
        private FrameAnimState m_changeFrameAnimName = FrameAnimState.Idle;

        /// <summary>
        /// 各状态对应的帧动画片段缓存。
        /// </summary>
        private FrameClip[] m_animClips = new FrameClip[(int)FrameAnimState.Max];

        /// <summary>
        /// 浮点动画参数缓存。
        /// </summary>
        private readonly Dictionary<int, float> m_floatMap = new Dictionary<int, float>();

        /// <summary>
        /// 当前帧动画配置资源地址。
        /// </summary>
        private string m_curCfgLocation;

        /// <summary>
        /// 是否已经绑定显示用的精灵渲染器。
        /// </summary>
        private bool m_isBindSpriteRenderer;

        /// <summary>
        /// 死亡动画播放速度。
        /// </summary>
        private float m_deathSpeed = 1.0f;

        /// <summary>
        /// 模型显示缩放。
        /// </summary>
        private Vector3 m_modelScale;

        /// <summary>
        /// 是否已经设置首帧。
        /// </summary>
        private bool m_isSetFirstFrame;

        /// <summary>
        /// 是否使用非缩放时间驱动。
        /// </summary>
        private bool m_isUnscaledTime;

        /// <summary>
        /// 上一次推进动画帧时的时间戳。
        /// </summary>
        private float m_preFrameTime;

        /// <summary>
        /// 动画速度缩放系数。
        /// </summary>
        private float m_speedScale = 1.0f;

        /// <summary>
        /// 当前基础播放速度。
        /// </summary>
        private float m_curBaseSpeed;

        /// <summary>
        /// 是否已经释放或销毁。
        /// </summary>
        private bool m_isDestroy;

        /// <summary>
        /// 是否已经请求开始播放。
        /// </summary>
        private bool m_isStarted;

        /// <summary>
        /// 当前代理在调度列表中的索引；-1表示未注册。
        /// </summary>
        internal int UpdateIndex { get; set; } = -1;

        /// <summary>
        /// 当前代理是否由非缩放时间调度。
        /// </summary>
        internal bool IsUnscaledTime => m_isUnscaledTime;

        /// <summary>
        /// 是否有效（未销毁、已初始化、已绑定SpriteRenderer）
        /// </summary>
        public bool IsValid => !m_isDestroy && m_isInit && m_spriteRenderer != null;

        private int m_initVersion;
        private CancellationTokenSource m_initCts;

        #endregion

        /// <summary>
        /// 创建帧动画代理实例
        /// </summary>
        public static FrameAnimatorAgent Create()
        {
            var agent = MemoryObject.Spawn<FrameAnimatorAgent>();
            agent.m_isDestroy = false;
            return agent;
        }

        /// <summary>
        /// 初始化帧动画代理，异步加载帧动画资源
        /// </summary>
        /// <param name="modelConfig">模型配置</param>
        public async UniTask Init(ModelConfig modelConfig)
        {
            m_initCts?.Cancel();
            m_initCts?.Dispose();
            m_initCts = new CancellationTokenSource();
            int version = ++m_initVersion;
            var ct = m_initCts.Token;
            if (modelConfig == null || string.IsNullOrEmpty(modelConfig.FrameCfgLocation))
            {
                DLogger.Error($"请检查模型配置表，模型配置表为空");
                return;
            }
            m_curCfgLocation = modelConfig.FrameCfgLocation;
            try
            {
                var frameSpritePool = await FrameSpriteMgr.Instance.GetFrameSpritePool(m_curCfgLocation, ct);
                if (ct.IsCancellationRequested || version != m_initVersion || m_isDestroy)
                {
                    return;
                }

                if (frameSpritePool == null)
                {
                    DLogger.Error($"没有找到帧动画配置文件: {m_curCfgLocation}");
                    return;
                }

                m_frameSpritePool = frameSpritePool;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            m_modelScale = modelConfig.ModelScale > 0 ? new Vector3(modelConfig.ModelScale, modelConfig.ModelScale, modelConfig.ModelScale) : Vector3.one;
            m_deathSpeed = modelConfig.DeathFrameSpeed > 0 ? modelConfig.DeathFrameSpeed : 1;
            m_curBaseSpeed = NORMAL_BASE_SPEED;
            m_isBindSpriteRenderer = false;

            if (m_isDestroy)
            {
                return;
            }

            m_animClips[(int)FrameAnimState.Idle] = new FrameClip(FrameAnimName.idle,
                m_frameSpritePool.GetSprites(FrameAnimName.idle), IsLoopAnim(FrameAnimState.Idle));
            var moveClip = new FrameClip(FrameAnimName.run,
                m_frameSpritePool.GetSprites(FrameAnimName.run), IsLoopAnim(FrameAnimState.Move));
            moveClip.RandomSetInitIndex(s_random);
            m_animClips[(int)FrameAnimState.Move] = moveClip;
            m_animClips[(int)FrameAnimState.Death] = new FrameClip(FrameAnimName.death,
                m_frameSpritePool.GetSprites(FrameAnimName.death), IsLoopAnim(FrameAnimState.Death));
            m_animClips[(int)FrameAnimState.Skill1] = new FrameClip(FrameAnimName.skill1,
                m_frameSpritePool.GetSprites(FrameAnimName.skill1), IsLoopAnim(FrameAnimState.Skill1));
            m_animClips[(int)FrameAnimState.Skill2] = new FrameClip(FrameAnimName.skill2,
                m_frameSpritePool.GetSprites(FrameAnimName.skill2), IsLoopAnim(FrameAnimState.Skill2));
            m_animClips[(int)FrameAnimState.Hurt1] = new FrameClip(FrameAnimName.hurt1,
                m_frameSpritePool.GetSprites(FrameAnimName.hurt1), IsLoopAnim(FrameAnimState.Hurt1));
            m_animClips[(int)FrameAnimState.Hurt2] = new FrameClip(FrameAnimName.hurt2,
                m_frameSpritePool.GetSprites(FrameAnimName.hurt2), IsLoopAnim(FrameAnimState.Hurt2));
            m_isInit = true;
            SetFirstFrame();
        }

        /// <summary>
        /// 设置是否使用不受时间缩放影响的时间
        /// </summary>
        /// <param name="isUnscaledTime">true=使用UnscaledTime，false=使用普通Time</param>
        public void SetUnscaledTime(bool isUnscaledTime)
        {
            if (m_isUnscaledTime == isUnscaledTime)
            {
                return;
            }

            bool isRegistered = UpdateIndex >= 0;
            if (isRegistered && FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.UnregisterAnimator(this);
            }

            m_isUnscaledTime = isUnscaledTime;

            if (m_isStarted && IsValid)
            {
                m_preFrameTime = isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
            }

            if (isRegistered && FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.RegisterAnimator(this);
            }
        }

        /// <summary>
        /// 绑定显示用的SpriteRenderer组件
        /// </summary>
        /// <param name="spriteRenderer">SpriteRenderer组件</param>
        public void BindDisplayRender(SpriteRenderer spriteRenderer)
        {
            if (m_isBindSpriteRenderer)
            {
                return;
            }
            m_isBindSpriteRenderer = true;
            m_spriteRenderer = spriteRenderer;
            SetFirstFrame();
        }

        /// <summary>
        /// 绑定显示用的SpriteRenderer组件。
        /// </summary>
        /// <param name="spriteRenderer">SpriteRenderer组件。</param>
        public void BindSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            BindDisplayRender(spriteRenderer);
        }

        /// <summary>
        /// 在初始化和绑定显示组件都满足后设置首帧图片。
        /// </summary>
        private void SetFirstFrame()
        {
            if (!m_isInit)
            {
                if (m_spriteRenderer != null)
                {
                    m_spriteRenderer.sprite = null;
                }
                return;
            }

            if (m_isSetFirstFrame || m_spriteRenderer == null)
            {
                return;
            }
            
            m_curFrameAnimName = m_changeFrameAnimName;
            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                DLogger.Warning($"没找到动画Clip: {m_curFrameAnimName}");
                return;
            }

            SetSprite(curClip.GetNext());
            SetSpriteRendererSize();
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
            m_isSetFirstFrame = true;
        }

        /// <summary>
        /// 设置当前帧图片到精灵渲染器。
        /// </summary>
        /// <param name="sprite">待显示的帧图片。</param>
        private void SetSprite(Sprite sprite)
        {
            if (m_isDestroy || !m_isInit || m_spriteRenderer == null || sprite == null)
            {
                return;
            }

            m_spriteRenderer.sprite = sprite;
        }

        /// <summary>
        /// 开始播放帧动画
        /// </summary>
        public void StartAnim()
        {
            if (!IsValid)
            {
                return;
            }

            m_isStarted = true;
            FrameSpriteMgr.Instance.RegisterAnimator(this);
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;
        }

        /// <summary>
        /// 调度器驱动的帧动画更新。
        /// </summary>
        /// <param name="currentTime">当前时间域下的时间戳。</param>
        /// <returns>true表示仍需继续调度；false表示已无效或播放结束。</returns>
        internal bool Tick(float currentTime)
        {
            if (!IsValid)
            {
                return false;
            }

            var curClip = m_animClips[(int)m_curFrameAnimName];

            if (curClip == null)
            {
                return false;
            }

            if (curClip.IsStop())
            {
                return false;
            }

            var deltaTime = currentTime - m_preFrameTime;

            if (deltaTime * GetSpeed() > FRAME_INTERVAL)
            {
                SetSprite(curClip.GetNext());
                SetSpriteRendererSize();
                m_preFrameTime = currentTime;
            }

            return true;
        }

        /// <summary>
        /// 设置或还原精灵渲染器所在节点的模型缩放。
        /// </summary>
        /// <param name="revert">是否还原为默认缩放。</param>
        private void SetSpriteRendererSize(bool revert = false)
        {
            if (m_spriteRenderer == null)
            {
                return;
            }

            m_spriteRenderer.transform.localScale = revert ? Vector3.one : m_modelScale;
        }

        /// <summary>
        /// 获取当前动画播放速度
        /// </summary>
        /// <returns>当前动画播放速度。</returns>
        public float GetSpeed()
        {
            if (m_curFrameAnimName == FrameAnimState.Move)
            {
                return m_curBaseSpeed;
            }

            if (m_curFrameAnimName == FrameAnimState.Death)
            {
                return m_deathSpeed;
            }

            return m_speedScale * m_curBaseSpeed;
        }

        /// <summary>
        /// 切换动画状态
        /// </summary>
        /// <param name="animName">目标动画状态</param>
        public void SwitchAnim(FrameAnimState animName)
        {
            if (!IsValid)
            {
                m_changeFrameAnimName = animName;
                return;
            }

            var oldAnimName = m_curFrameAnimName;
            if (animName != oldAnimName)
            {
                m_curFrameAnimName = animName;
                var oldClip = m_animClips[(int)oldAnimName];
                oldClip?.Leave();

                if (m_isStarted)
                {
                    FrameSpriteMgr.Instance.RegisterAnimator(this);
                }
            }
        }

        /// <summary>
        /// 根据布尔参数切换动画状态。
        /// </summary>
        /// <param name="id">动画参数ID。</param>
        /// <param name="value">参数值。</param>
        public void SwitchAnim(int id, bool value)
        {
            var state = FrameAnimState.Idle;
            if (id == FrameAnimParamDefine.Death)
            {
                if (value)
                {
                    state = FrameAnimState.Death;
                }
            }
            else if (id == FrameAnimParamDefine.Show)
            {
                if (value)
                {
                    state = FrameAnimState.Idle;
                }
            }
            else if (id == FrameAnimParamDefine.Moving)
            {
                if (value)
                {
                    state = FrameAnimState.Move;
                }
            }

            SwitchAnim(state);
        }

        /// <summary>
        /// 根据整型参数切换动画状态。
        /// </summary>
        /// <param name="id">动画参数ID。</param>
        /// <param name="value">参数值。</param>
        public void SwitchAnim(int id, int value)
        {
            var state = FrameAnimState.Idle;
            if (id == FrameAnimParamDefine.SkillIndex)
            {
                switch (value)
                {
                    case 1:
                        state = FrameAnimState.Skill1;
                        break;
                    case 2:
                        state = FrameAnimState.Skill2;
                        break;
                    default:
                        state = FrameAnimState.Idle;
                        break;
                }
            }
            else if (id == FrameAnimParamDefine.HurtIndex)
            {
                switch (value)
                {
                    case 1:
                        state = FrameAnimState.Hurt1;
                        break;
                    case 2:
                        state = FrameAnimState.Hurt2;
                        break;
                    default:
                        state = FrameAnimState.Idle;
                        break;
                }
            }
            else if (id == FrameAnimParamDefine.ImpactId)
            {
                state = m_isInit ? m_curFrameAnimName : m_changeFrameAnimName;
            }

            SwitchAnim(state);
        }

        /// <summary>
        /// 重播动画
        /// </summary>
        /// <param name="animName"></param>
        public void ReplayAnim(FrameAnimState animName)
        {
            if (!IsValid)
            {
                m_changeFrameAnimName = animName;
                return;
            }

            if (animName != m_curFrameAnimName)
            {
                var oldClip = m_animClips[(int)m_curFrameAnimName];
                oldClip?.Leave();
                m_curFrameAnimName = animName;
            }
            
            var clip = m_animClips[(int)m_curFrameAnimName];
            if (clip == null)
            {
                return;
            }

            clip.Leave();
            SetSprite(clip.GetNext());
            SetSpriteRendererSize();
            m_preFrameTime = m_isUnscaledTime ? GameTime.UnscaledTime : GameTime.Time;

            if (m_isStarted)
            {
                FrameSpriteMgr.Instance.RegisterAnimator(this);
            }
        }

        /// <summary>
        /// 判断指定动画是否循环播放
        /// </summary>
        /// <param name="animName">动画状态</param>
        /// <returns>true=循环播放</returns>
        public bool IsLoopAnim(FrameAnimState animName)
            => animName == FrameAnimState.Idle || animName == FrameAnimState.Move;

        /// <summary>
        /// 设置动画播放速度缩放
        /// </summary>
        /// <param name="speed">速度缩放倍数</param>
        public void SetAnimSpeed(float speed)
        {
            m_speedScale = speed;
        }

        /// <summary>
        /// 设置全部动画播放速度缩放。
        /// </summary>
        /// <param name="speed">速度缩放倍数。</param>
        public void SetAllAnimSpeed(float speed)
        {
            SetAnimSpeed(speed);
        }

        /// <summary>
        /// 设置浮点类型动画参数。
        /// </summary>
        /// <param name="id">动画参数ID。</param>
        /// <param name="value">参数值。</param>
        public void SetFloat(int id, float value)
        {
            if (m_floatMap.ContainsKey(id))
            {
                m_floatMap[id] = value;
            }
            else
            {
                m_floatMap.Add(id, value);
            }
        }

        /// <summary>
        /// 设置是否使用不受时间缩放影响的时间。
        /// </summary>
        /// <param name="isUnScale">true=使用UnscaledTime，false=使用普通Time。</param>
        public void SetUnScale(bool isUnScale)
        {
            SetUnscaledTime(isUnScale);
        }

        #region 释放资源

        /// <summary>
        /// 主动释放
        /// </summary>
        public void Release()
        {
            MemoryObject.Release(this);
        }

        /// <summary>
        /// 释放资源回调
        /// </summary>
        public override void OnRelease()
        {
            m_initVersion++;
            m_initCts?.Cancel();
            m_initCts?.Dispose();
            m_initCts = null;
            if (FrameSpriteMgr.IsValid)
            {
                FrameSpriteMgr.Instance.UnregisterAnimator(this);
            }
            m_isInit = false;
            m_isDestroy = true;
            m_isStarted = false;
            m_frameSpritePool = null;
            SetSpriteRendererSize(true);
            if (m_spriteRenderer != null)
            {
                m_spriteRenderer.sprite = null;
            }
            m_spriteRenderer = null;
            m_curFrameAnimName = FrameAnimState.Idle;
            m_changeFrameAnimName = FrameAnimState.Idle;
            m_curCfgLocation = string.Empty;
            m_isBindSpriteRenderer = false;
            m_deathSpeed = 1.0f;
            m_modelScale = Vector3.one;
            m_isSetFirstFrame = false;
            m_isUnscaledTime = false;
            m_preFrameTime = 0;
            m_speedScale = 1.0f;
            m_floatMap.Clear();

            for (int i = 0; i < m_animClips.Length; i++)
            {
                m_animClips[i]?.OnDestroy();
                m_animClips[i] = null;
            }
        }

        #endregion
    }
}
