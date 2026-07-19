using System;
using System.Collections.Generic;

namespace GameBattle
{
    /// <summary>
    /// GameBattle 内部事件组件，按事件数据类型管理订阅和派发。
    /// </summary>
    public sealed class EventComponent : Entity, INonPooledEntity
    {
        #region Fields

        /// <summary>
        /// 每种事件类型一个桶，让订阅列表、发布深度和延迟清理状态保持在一起。
        /// </summary>
        private readonly Dictionary<Type, EventBucket> m_eventBuckets = new();

        /// <summary>
        /// 下一个订阅 id。
        /// </summary>
        private int m_nextSubscriptionId;

        #endregion

        #region Pool

        /// <summary>
        /// 从对象池取出时清理历史订阅状态。
        /// </summary>
        protected override void OnRentFromPool()
        {
            m_eventBuckets.Clear();
            m_nextSubscriptionId = 0;
        }

        #endregion

        #region Subscribe

        /// <summary>
        /// 订阅指定事件类型。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="handler">事件回调。</param>
        /// <returns>用于取消订阅的 token。</returns>
        public EventSubscriptionToken Subscribe<T>(Action<T> handler) where T : struct, IEvent
            => Subscribe(null, handler);

        /// <summary>
        /// 订阅指定事件类型，并记录订阅所属对象。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="owner">订阅所属对象，可用于批量移除。</param>
        /// <param name="handler">事件回调。</param>
        /// <returns>用于取消订阅的 token。</returns>
        public EventSubscriptionToken Subscribe<T>(object owner, Action<T> handler) where T : struct, IEvent
        {
            if (IsDisposed || handler == null || IsInvalidEntityOwner(owner))
            {
                return default;
            }

            var type = typeof(T);
            var bucket = GetOrCreateBucket(type);
            var subscribers = bucket.Subscribers;
            var handlerDelegate = (Delegate)handler;

            // 同一个 owner + delegate 实例重复订阅时复用已有订阅，避免重复注册。
            for (var i = 0; i < subscribers.Count; i++)
            {
                var subscriber = subscribers[i];

                if (!subscriber.Removed && MatchesOwnerAndHandler(subscriber, owner, handlerDelegate))
                {
                    return new EventSubscriptionToken(this, type, subscriber.Id);
                }
            }

            var id = NextSubscriptionId();
            subscribers.Add(new EventSubscriber(id, owner, handlerDelegate));
            return new EventSubscriptionToken(this, type, id);
        }

        /// <summary>
        /// 订阅指定事件类型，并把取消订阅 token 托管到 scope。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="scope">订阅生命周期容器。</param>
        /// <param name="handler">事件回调。</param>
        /// <returns>用于取消订阅的 token。</returns>
        public EventSubscriptionToken SubscribeScoped<T>(EventSubscriptionScopeComponent scope, Action<T> handler)
            where T : struct, IEvent
        {
            var token = Subscribe(handler);
            // scope 托管 token 释放，调用方不缓存返回值也可以自动取消订阅。
            scope?.Add(token);
            return token;
        }

        /// <summary>
        /// 订阅指定事件类型，记录订阅所属对象，并把取消订阅 token 托管到 scope。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="owner">订阅所属对象，可用于批量移除。</param>
        /// <param name="scope">订阅生命周期容器。</param>
        /// <param name="handler">事件回调。</param>
        /// <returns>用于取消订阅的 token。</returns>
        public EventSubscriptionToken SubscribeScoped<T>(object owner, EventSubscriptionScopeComponent scope,
            Action<T> handler)
            where T : struct, IEvent
        {
            var token = Subscribe(owner, handler);
            // owner 用于批量移除，scope 用于跟随生命周期自动释放。
            scope?.Add(token);
            return token;
        }

        #endregion

        #region Unsubscribe

        /// <summary>
        /// 通过订阅 token 取消订阅。
        /// </summary>
        /// <param name="token">订阅 token。</param>
        /// <returns>成功取消返回 true。</returns>
        public bool Unsubscribe(EventSubscriptionToken token)
            => token.IsOwnedBy(this) && Unsubscribe(token.EventType, token.SubscriptionId);

        /// <summary>
        /// 通过回调实例取消订阅。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="handler">注册时使用的回调实例。</param>
        /// <returns>成功取消返回 true。</returns>
        public bool Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (IsDisposed || handler == null)
            {
                return false;
            }

            var type = typeof(T);

            if (!m_eventBuckets.TryGetValue(type, out var bucket))
            {
                return false;
            }

            var subscribers = bucket.Subscribers;
            var handlerDelegate = (Delegate)handler;
            var removed = false;

            for (var i = subscribers.Count - 1; i >= 0; i--)
            {
                var subscriber = subscribers[i];

                if (subscriber.Removed || subscriber.Handler != handlerDelegate)
                {
                    continue;
                }

                RemoveAt(bucket, i);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// 通过 owner 和回调实例取消订阅。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="owner">订阅所属对象。</param>
        /// <param name="handler">注册时使用的回调实例。</param>
        /// <returns>成功取消返回 true。</returns>
        public bool Unsubscribe<T>(object owner, Action<T> handler) where T : struct, IEvent
        {
            if (IsDisposed || handler == null)
            {
                return false;
            }

            var type = typeof(T);

            if (!m_eventBuckets.TryGetValue(type, out var bucket))
            {
                return false;
            }

            var subscribers = bucket.Subscribers;
            var handlerDelegate = (Delegate)handler;
            var removed = false;

            for (var i = subscribers.Count - 1; i >= 0; i--)
            {
                var subscriber = subscribers[i];

                if (subscriber.Removed || !MatchesOwnerAndHandler(subscriber, owner, handlerDelegate))
                {
                    continue;
                }

                RemoveAt(bucket, i);
                removed = true;
            }

            return removed;
        }

        /// <summary>
        /// 移除指定 owner 下的所有订阅。
        /// </summary>
        /// <param name="owner">订阅所属对象。</param>
        public void RemoveAll(object owner)
        {
            if (IsDisposed || owner == null)
            {
                return;
            }

            foreach (var pair in m_eventBuckets)
            {
                var bucket = pair.Value;
                var subscribers = bucket.Subscribers;

                for (var i = subscribers.Count - 1; i >= 0; i--)
                {
                    var subscriber = subscribers[i];

                    if (!MatchesOwner(subscriber, owner))
                    {
                        continue;
                    }

                    RemoveAt(bucket, i);
                }
            }
        }

        #endregion

        #region Publish

        /// <summary>
        /// 派发指定事件数据。
        /// </summary>
        /// <typeparam name="T">事件数据类型。</typeparam>
        /// <param name="eventData">事件数据。</param>
        public void Publish<T>(T eventData) where T : struct, IEvent
        {
            if (IsDisposed)
            {
                return;
            }

            var type = typeof(T);

            if (!m_eventBuckets.TryGetValue(type, out var bucket))
            {
                return;
            }

            var subscribers = bucket.Subscribers;
            bucket.PublishDepth++;

            try
            {
                // 捕获原始数量，派发过程中新增的订阅不参与本次派发。
                var count = subscribers.Count;

                for (var i = 0; i < count && !IsDisposed; i++)
                {
                    var subscriber = subscribers[i];

                    if (!IsSubscriberActive(subscriber))
                    {
                        RemoveAt(bucket, i);
                        continue;
                    }

                    if (subscriber.Handler is not Action<T> callback)
                    {
                        continue;
                    }

                    callback(eventData);
                }
            }
            finally
            {
                bucket.PublishDepth--;

                // 派发过程中移除的订阅，只在最外层派发结束后统一压缩列表。
                if (bucket.PublishDepth == 0 && bucket.NeedCleanup)
                {
                    Cleanup(bucket);
                }
            }
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 清空事件组件并禁止后续订阅、取消订阅和派发。
        /// </summary>
        public void Clear()
        {
            if (IsDisposed)
            {
                return;
            }

            m_eventBuckets.Clear();
        }

        /// <summary>
        /// 销毁事件组件并清空全部订阅。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            Clear();
            base.Dispose();
        }

        #endregion

        #region Id

        /// <summary>
        /// 分配下一个订阅 id，跳过默认无效值 0。
        /// </summary>
        /// <returns>新的订阅 id。</returns>
        private int NextSubscriptionId()
        {
            unchecked
            {
                // 0 表示无效 token id，溢出回绕时跳过 0。
                m_nextSubscriptionId++;

                if (m_nextSubscriptionId == 0)
                {
                    m_nextSubscriptionId++;
                }
            }

            return m_nextSubscriptionId;
        }

        #endregion

        #region Bucket

        /// <summary>
        /// 获取或创建指定事件类型的订阅桶。
        /// </summary>
        /// <param name="type">事件数据类型。</param>
        /// <returns>事件订阅桶。</returns>
        private EventBucket GetOrCreateBucket(Type type)
        {
            if (m_eventBuckets.TryGetValue(type, out var bucket))
            {
                return bucket;
            }

            bucket = new EventBucket();
            m_eventBuckets.Add(type, bucket);
            return bucket;
        }

        #endregion

        #region Internal Unsubscribe

        /// <summary>
        /// 通过事件类型和订阅 id 取消订阅。
        /// </summary>
        /// <param name="type">事件数据类型。</param>
        /// <param name="subscriptionId">订阅 id。</param>
        /// <returns>成功取消返回 true。</returns>
        private bool Unsubscribe(Type type, int subscriptionId)
        {
            if (IsDisposed || type == null || subscriptionId == 0)
            {
                return false;
            }

            if (!m_eventBuckets.TryGetValue(type, out var bucket))
            {
                return false;
            }

            var subscribers = bucket.Subscribers;

            for (var i = 0; i < subscribers.Count; i++)
            {
                if (subscribers[i].Id != subscriptionId)
                {
                    continue;
                }

                RemoveAt(bucket, i);
                return true;
            }

            return false;
        }

        #endregion

        #region Match

        /// <summary>
        /// 判断订阅 owner 是否为已经无效的实体。
        /// </summary>
        /// <param name="owner">订阅所属对象。</param>
        /// <returns>owner 是已销毁或未初始化实体时返回 true。</returns>
        private static bool IsInvalidEntityOwner(object owner)
            => owner is Entity { IsDisposed: true } || owner is Entity { RuntimeId: 0 };

        /// <summary>
        /// 判断订阅记录是否匹配指定 owner 和回调委托。
        /// </summary>
        /// <param name="subscriber">订阅记录。</param>
        /// <param name="owner">订阅所属对象。</param>
        /// <param name="handler">回调委托。</param>
        /// <returns>匹配时返回 true。</returns>
        private static bool MatchesOwnerAndHandler(EventSubscriber subscriber, object owner, Delegate handler)
            => MatchesOwner(subscriber, owner) && subscriber.Handler == handler;

        /// <summary>
        /// 判断订阅记录是否匹配指定 owner。
        /// <remarks>Entity owner 需要同时匹配订阅时的 RuntimeId，避免实体回池复用后被误认为同一个订阅 owner。</remarks>
        /// </summary>
        /// <param name="subscriber">订阅记录。</param>
        /// <param name="owner">订阅所属对象。</param>
        /// <returns>匹配时返回 true。</returns>
        private static bool MatchesOwner(EventSubscriber subscriber, object owner)
        {
            if (!ReferenceEquals(subscriber.Owner, owner))
            {
                return false;
            }

            if (!subscriber.OwnerIsEntity)
            {
                return true;
            }

            return owner is Entity entity
                   && entity.RuntimeId != 0
                   && entity.RuntimeId == subscriber.OwnerRuntimeId
                   && entity.World == subscriber.OwnerWorld;
        }

        /// <summary>
        /// 判断订阅记录当前是否仍然有效。
        /// <remarks>Entity owner 被销毁或回池复用后，旧订阅会在下一次派发时自动失效。</remarks>
        /// </summary>
        /// <param name="subscriber">订阅记录。</param>
        /// <returns>订阅仍有效时返回 true。</returns>
        private static bool IsSubscriberActive(EventSubscriber subscriber)
        {
            if (subscriber.Removed)
            {
                return false;
            }

            if (!subscriber.OwnerIsEntity)
            {
                return true;
            }

            return subscriber.Owner is Entity entity
                   && !entity.IsDisposed
                   && entity.RuntimeId != 0
                   && entity.RuntimeId == subscriber.OwnerRuntimeId
                   && entity.World == subscriber.OwnerWorld;
        }

        #endregion

        #region Remove

        /// <summary>
        /// 从订阅桶中移除指定下标的订阅。
        /// </summary>
        /// <param name="bucket">事件订阅桶。</param>
        /// <param name="index">订阅下标。</param>
        private void RemoveAt(EventBucket bucket, int index)
        {
            var subscribers = bucket.Subscribers;

            if (bucket.PublishDepth > 0)
            {
                // Publish 正在遍历时不改变列表结构，只标记为待删除。
                var subscriber = subscribers[index];
                subscriber.Removed = true;
                subscribers[index] = subscriber;
                bucket.NeedCleanup = true;
                return;
            }

            subscribers.RemoveAt(index);
        }

        /// <summary>
        /// 清理派发过程中被标记移除的订阅。
        /// </summary>
        /// <param name="bucket">事件订阅桶。</param>
        private static void Cleanup(EventBucket bucket)
        {
            var subscribers = bucket.Subscribers;

            // 从尾部移除，避免影响尚未检查的元素下标。
            for (var i = subscribers.Count - 1; i >= 0; i--)
            {
                if (subscribers[i].Removed)
                {
                    subscribers.RemoveAt(i);
                }
            }

            bucket.NeedCleanup = false;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// 单个事件类型对应的订阅桶。
        /// </summary>
        private sealed class EventBucket
        {
            /// <summary>
            /// 订阅列表。使用 List 便于按下标快速派发，并保持紧凑内存布局。
            /// </summary>
            public readonly List<EventSubscriber> Subscribers = new();

            /// <summary>
            /// 当前事件类型的嵌套派发深度。
            /// </summary>
            public int PublishDepth;

            /// <summary>
            /// 派发过程中发生移除时置为 true，延迟清理完成后重置。
            /// </summary>
            public bool NeedCleanup;
        }

        /// <summary>
        /// 单条事件订阅记录。
        /// </summary>
        private struct EventSubscriber
        {
            /// <summary>
            /// 订阅 id，用于 token 精确取消订阅。
            /// </summary>
            public readonly int Id;

            /// <summary>
            /// 订阅所属对象，用于按 owner 批量移除订阅。
            /// </summary>
            public readonly object Owner;

            /// <summary>
            /// 订阅 owner 是否为实体。
            /// </summary>
            public readonly bool OwnerIsEntity;

            /// <summary>
            /// Entity owner 订阅时所属世界。
            /// </summary>
            public readonly World OwnerWorld;

            /// <summary>
            /// Entity owner 订阅时的运行时 id。
            /// </summary>
            public readonly long OwnerRuntimeId;

            /// <summary>
            /// 实际注册的回调委托。
            /// </summary>
            public readonly Delegate Handler;

            /// <summary>
            /// 是否已被标记移除。派发过程中取消订阅时会先标记，待派发结束后统一清理。
            /// </summary>
            public bool Removed;

            /// <summary>
            /// 创建订阅记录。
            /// </summary>
            /// <param name="id">订阅 id。</param>
            /// <param name="owner">订阅所属对象。</param>
            /// <param name="handler">回调委托。</param>
            public EventSubscriber(int id, object owner, Delegate handler)
            {
                Id = id;
                Owner = owner;

                if (owner is Entity entity)
                {
                    OwnerIsEntity = true;
                    OwnerWorld = entity.World;
                    OwnerRuntimeId = entity.RuntimeId;
                }
                else
                {
                    OwnerIsEntity = false;
                    OwnerWorld = null;
                    OwnerRuntimeId = 0;
                }

                Handler = handler;
                Removed = false;
            }
        }

        #endregion
    }
}
