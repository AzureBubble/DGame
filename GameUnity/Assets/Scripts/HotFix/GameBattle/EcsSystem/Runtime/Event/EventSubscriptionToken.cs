using System;

namespace GameBattle
{
    /// <summary>
    /// EventComponent.Subscribe 返回的值类型订阅句柄，用于显式或托管式取消订阅。
    /// </summary>
    public readonly struct EventSubscriptionToken : IDisposable
    {
        #region Fields

        /// <summary>
        /// 生成该 token 的事件组件。
        /// </summary>
        private readonly EventComponent m_eventComponent;

        /// <summary>
        /// token 对应的事件数据类型。
        /// </summary>
        private readonly Type m_eventType;

        /// <summary>
        /// token 对应的订阅 id。
        /// </summary>
        private readonly int m_subscriptionId;

        #endregion

        #region Properties

        /// <summary>
        /// 订阅对应的事件类型。
        /// </summary>
        internal Type EventType => m_eventType;

        /// <summary>
        /// 订阅 id。
        /// </summary>
        internal int SubscriptionId => m_subscriptionId;

        /// <summary>
        /// 当前 token 是否有效。订阅 id 为 0 表示默认或无效 token。
        /// </summary>
        public bool IsValid => m_eventComponent != null && m_eventType != null && m_subscriptionId != 0;

        #endregion

        #region Create

        /// <summary>
        /// 创建事件订阅 token。
        /// </summary>
        /// <param name="eventComponent">生成该 token 的事件组件。</param>
        /// <param name="eventType">事件数据类型。</param>
        /// <param name="subscriptionId">订阅 id。</param>
        internal EventSubscriptionToken(EventComponent eventComponent, Type eventType, int subscriptionId)
        {
            m_eventComponent = eventComponent;
            m_eventType = eventType;
            m_subscriptionId = subscriptionId;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 取消当前 token 对应的订阅。
        /// </summary>
        public void Dispose() => m_eventComponent?.Unsubscribe(this);

        #endregion

        #region Query

        /// <summary>
        /// 判断当前 token 是否属于指定事件组件。
        /// </summary>
        /// <param name="eventComponent">事件组件。</param>
        /// <returns>属于指定事件组件时返回 true。</returns>
        internal bool IsOwnedBy(EventComponent eventComponent) => ReferenceEquals(m_eventComponent, eventComponent);

        #endregion
    }
}
