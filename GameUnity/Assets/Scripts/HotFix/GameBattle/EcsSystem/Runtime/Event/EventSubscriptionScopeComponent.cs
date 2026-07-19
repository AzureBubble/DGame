using System.Collections.Generic;

namespace GameBattle
{
    /// <summary>
    /// 事件订阅生命周期容器，用于集中释放一组订阅 token。
    /// </summary>
    public sealed class EventSubscriptionScopeComponent : Entity
    {
        #region Fields

        /// <summary>
        /// 当前 scope 托管的订阅 token 列表。
        /// </summary>
        private readonly List<EventSubscriptionToken> m_tokens = new();

        /// <summary>
        /// 标记当前 scope 是否已经完成释放。
        /// </summary>
        private bool m_disposed;

        #endregion

        #region Pool

        /// <summary>
        /// 从对象池取出时重置订阅列表与释放标记。
        /// </summary>
        protected override void OnRentFromPool()
        {
            m_tokens.Clear();
            m_disposed = false;
        }

        #endregion

        #region Add

        /// <summary>
        /// 把订阅 token 加入当前生命周期容器。
        /// </summary>
        /// <param name="token">订阅 token。</param>
        public void Add(EventSubscriptionToken token)
        {
            // 默认 token 直接忽略，调用方可以安全传入订阅失败返回的 token。
            if (!token.IsValid)
            {
                return;
            }

            if (m_disposed)
            {
                // 已释放的 scope 不能再持有订阅，因此新传入的 token 立即释放。
                token.Dispose();
                return;
            }

            m_tokens.Add(token);
        }

        /// <summary>
        /// 把订阅 token 加入当前生命周期容器，并返回原 token。
        /// </summary>
        /// <param name="token">订阅 token。</param>
        /// <returns>原订阅 token。</returns>
        public EventSubscriptionToken AddToScope(EventSubscriptionToken token)
        {
            // 便于调用方在保留 token 的同时绑定 scope。
            Add(token);
            return token;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 释放当前持有的全部订阅，但不把 scope 标记为已释放。
        /// </summary>
        public void Clear()
        {
            if (m_disposed)
            {
                return;
            }

            ReleaseAll();
        }

        /// <summary>
        /// 释放当前持有的全部订阅，并把 scope 标记为已释放。
        /// </summary>
        public override void Dispose()
        {
            if (m_disposed || IsDisposed)
            {
                return;
            }

            ReleaseAll();
            m_disposed = true;
            base.Dispose();
        }

        #endregion

        #region Internal

        /// <summary>
        /// 释放当前托管的全部订阅 token。
        /// </summary>
        private void ReleaseAll()
        {
            // 从尾部释放，避免释放过程中间接触碰当前 scope 时影响尚未处理的下标。
            for (var i = m_tokens.Count - 1; i >= 0; i--)
            {
                m_tokens[i].Dispose();
            }

            m_tokens.Clear();
        }

        #endregion
    }
}
