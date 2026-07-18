using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DGame;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 帧动画资源池与播放更新管理器。
    /// </summary>
    public sealed class FrameSpriteMgr : Singleton<FrameSpriteMgr>
    {
        /// <summary>
        /// 帧动画调度采样间隔，沿用原单代理计时器的八倍采样率。
        /// </summary>
        private const float ANIMATOR_TICK_INTERVAL = 0.015625f;

        /// <summary>
        /// 已加载的帧动画资源池缓存，键为资源定位地址。
        /// </summary>
        private readonly Dictionary<string, FrameSpritePool> m_frameSpritePools = new Dictionary<string, FrameSpritePool>();

        /// <summary>
        /// 使用缩放时间的场景帧动画代理列表。
        /// </summary>
        private readonly List<FrameAnimatorAgent> m_scaledFrameAnimators = new List<FrameAnimatorAgent>();

        /// <summary>
        /// 使用非缩放时间的场景帧动画代理列表。
        /// </summary>
        private readonly List<FrameAnimatorAgent> m_unscaledFrameAnimators = new List<FrameAnimatorAgent>();

        /// <summary>
        /// 使用缩放时间的UI帧动画代理列表。
        /// </summary>
        private readonly List<UIFrameAnimatorAgent> m_scaledUIAnimators = new List<UIFrameAnimatorAgent>();

        /// <summary>
        /// 使用非缩放时间的UI帧动画代理列表。
        /// </summary>
        private readonly List<UIFrameAnimatorAgent> m_unscaledUIAnimators = new List<UIFrameAnimatorAgent>();

        /// <summary>
        /// 缩放时间调度计时器。列表暂时为空时仍保留，避免频繁创建和销毁。
        /// </summary>
        private GameTimer m_scaledTimer;

        /// <summary>
        /// 非缩放时间调度计时器。列表暂时为空时仍保留，避免频繁创建和销毁。
        /// </summary>
        private GameTimer m_unscaledTimer;

        /// <summary>
        /// 注册场景帧动画代理到对应时间域的调度列表。
        /// </summary>
        /// <param name="agent">待注册的场景帧动画代理。</param>
        internal void RegisterAnimator(FrameAnimatorAgent agent)
        {
            if (agent == null || agent.UpdateIndex >= 0)
            {
                return;
            }

            var animators = agent.IsUnscaledTime ? m_unscaledFrameAnimators : m_scaledFrameAnimators;
            agent.UpdateIndex = animators.Count;
            animators.Add(agent);
            EnsureTickTimer(agent.IsUnscaledTime);
        }

        /// <summary>
        /// 注册UI帧动画代理到对应时间域的调度列表。
        /// </summary>
        /// <param name="agent">待注册的UI帧动画代理。</param>
        internal void RegisterAnimator(UIFrameAnimatorAgent agent)
        {
            if (agent == null || agent.UpdateIndex >= 0)
            {
                return;
            }

            var animators = agent.IsUnscaledTime ? m_unscaledUIAnimators : m_scaledUIAnimators;
            agent.UpdateIndex = animators.Count;
            animators.Add(agent);
            EnsureTickTimer(agent.IsUnscaledTime);
        }

        /// <summary>
        /// 从调度列表注销场景帧动画代理。
        /// </summary>
        /// <param name="agent">待注销的场景帧动画代理。</param>
        internal void UnregisterAnimator(FrameAnimatorAgent agent)
        {
            var animators = agent != null && agent.IsUnscaledTime
                ? m_unscaledFrameAnimators
                : m_scaledFrameAnimators;
            RemoveAnimator(animators, agent);
        }

        /// <summary>
        /// 从调度列表注销UI帧动画代理。
        /// </summary>
        /// <param name="agent">待注销的UI帧动画代理。</param>
        internal void UnregisterAnimator(UIFrameAnimatorAgent agent)
        {
            var animators = agent != null && agent.IsUnscaledTime
                ? m_unscaledUIAnimators
                : m_scaledUIAnimators;
            RemoveAnimator(animators, agent);
        }

        /// <summary>
        /// 确保指定时间域的调度计时器已经创建。
        /// </summary>
        /// <param name="isUnscaledTime">是否使用不受时间缩放影响的时间。</param>
        private void EnsureTickTimer(bool isUnscaledTime)
        {
            if (isUnscaledTime)
            {
                if (GameTimer.IsNull(m_unscaledTimer))
                {
                    m_unscaledTimer = GameModule.GameTimerModule.CreateUnscaledLoopGameTimer(
                        ANIMATOR_TICK_INTERVAL, TickUnscaled);
                }
                return;
            }

            if (GameTimer.IsNull(m_scaledTimer))
            {
                m_scaledTimer = GameModule.GameTimerModule.CreateLoopGameTimer(
                    ANIMATOR_TICK_INTERVAL, TickScaled);
            }
        }

        /// <summary>
        /// 缩放时间计时器回调，驱动所有使用缩放时间的帧动画代理。
        /// </summary>
        /// <param name="args">计时器透传参数，当前未使用。</param>
        private void TickScaled(object[] args)
        {
            float gameTime = GameTime.Time;
            TickAnimators(m_scaledFrameAnimators, gameTime);
            TickAnimators(m_scaledUIAnimators, gameTime);
        }

        /// <summary>
        /// 非缩放时间计时器回调，驱动所有使用非缩放时间的帧动画代理。
        /// </summary>
        /// <param name="args">计时器透传参数，当前未使用。</param>
        private void TickUnscaled(object[] args)
        {
            float unscaledTime = GameTime.UnscaledTime;
            TickAnimators(m_unscaledFrameAnimators, unscaledTime);
            TickAnimators(m_unscaledUIAnimators, unscaledTime);
        }

        /// <summary>
        /// 轮询场景帧动画代理列表，并移除已经无效或播放结束的代理。
        /// </summary>
        /// <param name="animators">待轮询的场景帧动画代理列表。</param>
        /// <param name="currentTime">当前时间域下的时间戳。</param>
        private void TickAnimators(List<FrameAnimatorAgent> animators, float currentTime)
        {
            for (int i = animators.Count - 1; i >= 0; i--)
            {
                var agent = animators[i];
                if (!agent.Tick(currentTime))
                {
                    UnregisterAnimator(agent);
                }
            }
        }

        /// <summary>
        /// 轮询UI帧动画代理列表，并移除已经无效或播放结束的代理。
        /// </summary>
        /// <param name="animators">待轮询的UI帧动画代理列表。</param>
        /// <param name="currentTime">当前时间域下的时间戳。</param>
        private void TickAnimators(List<UIFrameAnimatorAgent> animators, float currentTime)
        {
            for (int i = animators.Count - 1; i >= 0; i--)
            {
                var agent = animators[i];
                if (!agent.Tick(currentTime))
                {
                    UnregisterAnimator(agent);
                }
            }
        }

        /// <summary>
        /// 以尾部交换方式从场景帧动画代理列表中移除代理。
        /// </summary>
        /// <param name="animators">目标场景帧动画代理列表。</param>
        /// <param name="agent">待移除的场景帧动画代理。</param>
        private static void RemoveAnimator(List<FrameAnimatorAgent> animators, FrameAnimatorAgent agent)
        {
            int index = agent?.UpdateIndex ?? -1;
            if ((uint)index >= (uint)animators.Count || !ReferenceEquals(animators[index], agent))
            {
                return;
            }

            int lastIndex = animators.Count - 1;
            if (index != lastIndex)
            {
                var lastAgent = animators[lastIndex];
                animators[index] = lastAgent;
                lastAgent.UpdateIndex = index;
            }

            animators.RemoveAt(lastIndex);
            agent.UpdateIndex = -1;
        }

        /// <summary>
        /// 以尾部交换方式从UI帧动画代理列表中移除代理。
        /// </summary>
        /// <param name="animators">目标UI帧动画代理列表。</param>
        /// <param name="agent">待移除的UI帧动画代理。</param>
        private static void RemoveAnimator(List<UIFrameAnimatorAgent> animators, UIFrameAnimatorAgent agent)
        {
            int index = agent?.UpdateIndex ?? -1;
            if ((uint)index >= (uint)animators.Count || !ReferenceEquals(animators[index], agent))
            {
                return;
            }

            int lastIndex = animators.Count - 1;
            if (index != lastIndex)
            {
                var lastAgent = animators[lastIndex];
                animators[index] = lastAgent;
                lastAgent.UpdateIndex = index;
            }

            animators.RemoveAt(lastIndex);
            agent.UpdateIndex = -1;
        }

        /// <summary>
        /// 获取FrameSpritePool资源。
        /// </summary>
        /// <param name="location">资源定位地址。</param>
        /// <returns>帧动画资源池；加载失败时返回null。</returns>
        public async UniTask<FrameSpritePool> GetFrameSpritePool(string location, CancellationToken ct)
        {
            if (!m_frameSpritePools.TryGetValue(location, out var pool))
            {
                var goCfg = await GameModule.ResourceModule.LoadAssetAsync<GameObject>(location, ct);
                if (goCfg != null)
                {
                    pool = goCfg.GetComponent<FrameSpritePool>();
                    m_frameSpritePools[location] = pool;
                }
            }
            return pool;
        }

        /// <summary>
        /// 清空所有FrameSpritePool缓存资源。
        /// </summary>
        private void ClearAll()
        {
            foreach (var pool in m_frameSpritePools.Values)
            {
                if (pool != null)
                {
                    GameModule.ResourceModule.UnloadAsset(pool.gameObject);
                }
            }
            m_frameSpritePools.Clear();
        }

        /// <summary>
        /// 销毁帧动画资源池管理器，停止调度并释放缓存的帧动画资源池。
        /// </summary>
        protected override void OnDestroy()
        {
            if (!GameTimer.IsNull(m_scaledTimer))
            {
                GameModule.GameTimerModule.DestroyGameTimer(m_scaledTimer);
                m_scaledTimer = null;
            }

            if (!GameTimer.IsNull(m_unscaledTimer))
            {
                GameModule.GameTimerModule.DestroyGameTimer(m_unscaledTimer);
                m_unscaledTimer = null;
            }

            for (int i = 0; i < m_scaledFrameAnimators.Count; i++)
            {
                m_scaledFrameAnimators[i].UpdateIndex = -1;
            }
            m_scaledFrameAnimators.Clear();

            for (int i = 0; i < m_unscaledFrameAnimators.Count; i++)
            {
                m_unscaledFrameAnimators[i].UpdateIndex = -1;
            }
            m_unscaledFrameAnimators.Clear();

            for (int i = 0; i < m_scaledUIAnimators.Count; i++)
            {
                m_scaledUIAnimators[i].UpdateIndex = -1;
            }
            m_scaledUIAnimators.Clear();

            for (int i = 0; i < m_unscaledUIAnimators.Count; i++)
            {
                m_unscaledUIAnimators[i].UpdateIndex = -1;
            }
            m_unscaledUIAnimators.Clear();
            ClearAll();
        }
    }
}
