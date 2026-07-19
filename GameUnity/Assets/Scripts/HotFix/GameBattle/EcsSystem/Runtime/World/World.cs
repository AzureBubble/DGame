using System;
using System.Collections.Generic;
using DGame.FixedPoint;

namespace GameBattle
{
    /// <summary>
    /// GameBattle 轻量 ECS 世界入口，负责实体创建、系统调度、事件根组件和对象池生命周期。
    /// </summary>
    public sealed class World : Entity, INonPooledEntity
    {
        #region Fields

        /// <summary>
        /// 单类型默认最大缓存实体数量。
        /// </summary>
        private const int DEFAULT_MAX_POOLED_COUNT_PER_TYPE = 1024;

        /// <summary>
        /// 当前世界内存活实体表，按 RuntimeId 精确索引。
        /// </summary>
        private readonly Dictionary<long, Entity> m_entities = new();

        /// <summary>
        /// 当前世界私有实体池，避免不同 World 之间复用状态。
        /// </summary>
        private readonly EntityPool m_entityPool;

        /// <summary>
        /// 业务实体自增 id。
        /// </summary>
        private long m_nextEntityId;

        /// <summary>
        /// 运行时唯一 id 自增值。
        /// </summary>
        private long m_nextRuntimeId;

        /// <summary>
        /// 当前世界已推进的逻辑帧。
        /// </summary>
        private int m_frame;

        /// <summary>
        /// 当前世界累计逻辑时间。
        /// </summary>
        private FixedPoint64 m_totalTime = FixedPoint64.Zero;

        #endregion

        #region Properties

        /// <summary>
        /// 实体生命周期系统调度器。
        /// </summary>
        public EntitySystem EntitySystem { get; }

        /// <summary>
        /// 世界根事件组件。
        /// </summary>
        public EventComponent EventComponent { get; private set; }

        #endregion

        #region Create

        /// <summary>
        /// 创建一个 ECS 世界。
        /// </summary>
        /// <param name="id">世界实体 id。传 0 时自动分配。</param>
        /// <param name="maxPooledCountPerType">每种实体类型最多缓存的数量。</param>
        public World(long id = 0, int maxPooledCountPerType = DEFAULT_MAX_POOLED_COUNT_PER_TYPE)
        {
            EntitySystem = new EntitySystem();
            m_entityPool = new EntityPool(maxPooledCountPerType);
            Initialize(this, null, id == 0 ? NextEntityId() : id, NextRuntimeId());
            RegisterEntity(this);
            EventComponent = AddComponent<EventComponent>();
        }

        /// <summary>
        /// 创建指定运行时类型的子实体。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>创建后的实体。</returns>
        public Entity CreateEntity(Type type) => AddChild(type);

        /// <summary>
        /// 创建指定类型的子实体。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>创建后的实体。</returns>
        public T CreateEntity<T>() where T : Entity, new() => AddChild<T>();

        /// <summary>
        /// 创建指定类型的子实体，并传入一个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntity<T, T1>(T1 value1) where T : Entity, IAwake<T1>, new() => AddChild<T, T1>(value1);

        /// <summary>
        /// 创建指定类型的子实体，并传入两个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntity<T, T1, T2>(T1 value1, T2 value2) where T : Entity, IAwake<T1, T2>, new()
            => AddChild<T, T1, T2>(value1, value2);

        /// <summary>
        /// 创建指定类型的子实体，并传入三个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntity<T, T1, T2, T3>(T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
            => AddChild<T, T1, T2, T3>(value1, value2, value3);

        /// <summary>
        /// 使用指定业务 id 创建运行时类型的子实体。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的实体。</returns>
        public Entity CreateEntityWithId(Type type, long id) => AddChildWithId(type, id);

        /// <summary>
        /// 使用指定业务 id 创建实体。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntityWithId<T>(long id) where T : Entity, new() => AddChildWithId<T>(id);

        /// <summary>
        /// 使用指定业务 id 创建实体，并传入一个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntityWithId<T, T1>(long id, T1 value1) where T : Entity, IAwake<T1>, new()
            => AddChildWithId<T, T1>(id, value1);

        /// <summary>
        /// 使用指定业务 id 创建实体，并传入两个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntityWithId<T, T1, T2>(long id, T1 value1, T2 value2)
            where T : Entity, IAwake<T1, T2>, new()
            => AddChildWithId<T, T1, T2>(id, value1, value2);

        /// <summary>
        /// 使用指定业务 id 创建实体，并传入三个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public T CreateEntityWithId<T, T1, T2, T3>(long id, T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
            => AddChildWithId<T, T1, T2, T3>(id, value1, value2, value3);

        #endregion

        #region Register

        /// <summary>
        /// 注册外置实体生命周期系统。
        /// </summary>
        /// <param name="system">外置实体系统。</param>
        public void RegisterSystem(IEntitySystem system) => EntitySystem.RegisterSystem(system);

        #endregion

        #region Pool

        /// <summary>
        /// 预热指定实体类型的对象池。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="count">目标缓存数量。</param>
        public void Prewarm<T>(int count) where T : Entity, new() => m_entityPool.Prewarm<T>(count);

        /// <summary>
        /// 预热指定运行时类型的对象池。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="count">目标缓存数量。</param>
        public void Prewarm(Type type, int count) => m_entityPool.Prewarm(type, count);

        /// <summary>
        /// 获取指定实体类型当前池内数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>池内可复用实体数量。</returns>
        public int GetPooledCount<T>() where T : Entity => m_entityPool.Count<T>();

        /// <summary>
        /// 获取指定运行时类型当前池内数量。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>池内可复用实体数量。</returns>
        public int GetPooledCount(Type type) => m_entityPool.Count(type);

        #endregion

        #region Query

        /// <summary>
        /// 通过 RuntimeId 查找指定类型实体。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="runtimeId">运行时唯一 id。</param>
        /// <returns>匹配的实体，未找到时返回 null。</returns>
        public T GetEntity<T>(long runtimeId) where T : Entity
            => m_entities.TryGetValue(runtimeId, out var entity) ? entity as T : null;

        /// <summary>
        /// 通过 RuntimeId 查找实体。
        /// </summary>
        /// <param name="runtimeId">运行时唯一 id。</param>
        /// <returns>匹配的实体，未找到时返回 null。</returns>
        public Entity GetEntity(long runtimeId)
            => m_entities.TryGetValue(runtimeId, out var entity) ? entity : null;

        #endregion

        #region Tick

        /// <summary>
        /// 推进一帧并生成本帧更新上下文。
        /// </summary>
        /// <param name="deltaTime">本帧逻辑时间步长。</param>
        /// <returns>本帧更新上下文。</returns>
        public UpdateContext AdvanceFrame(FixedPoint64 deltaTime)
        {
            m_frame++;
            m_totalTime += deltaTime;
            return new UpdateContext(this, m_frame, deltaTime, m_totalTime);
        }

        /// <summary>
        /// 推进一帧并依次执行 Update 与 LateUpdate。
        /// </summary>
        /// <param name="deltaTime">本帧逻辑时间步长。</param>
        public void Tick(FixedPoint64 deltaTime)
        {
            var context = AdvanceFrame(deltaTime);
            Update(context);
            LateUpdate(context);
        }

        /// <summary>
        /// 推进一帧并执行 Update。
        /// </summary>
        /// <param name="deltaTime">本帧逻辑时间步长。</param>
        public void Update(FixedPoint64 deltaTime)
        {
            var context = AdvanceFrame(deltaTime);
            Update(context);
        }

        /// <summary>
        /// 使用外部传入的上下文执行 Update。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        public void Update(in UpdateContext context) => EntitySystem.Update(context);

        /// <summary>
        /// 使用外部传入的上下文执行 LateUpdate。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        public void LateUpdate(in UpdateContext context) => EntitySystem.LateUpdate(context);

        #endregion

        #region Lifecycle

        /// <summary>
        /// 销毁世界并清空实体索引与对象池。
        /// </summary>
        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            EventComponent = null;
            base.Dispose();
            m_entities.Clear();
            m_entityPool.Clear();
            m_frame = 0;
            m_totalTime = FixedPoint64.Zero;
        }

        #endregion

        #region Internal Create

        /// <summary>
        /// 分配下一个业务实体 id。
        /// </summary>
        /// <returns>新的业务实体 id。</returns>
        internal long NextEntityId() => ++m_nextEntityId;

        /// <summary>
        /// 从对象池创建或租用实体，并完成世界状态初始化。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="parent">父实体。</param>
        /// <param name="id">业务实体 id。传 0 时自动分配。</param>
        /// <returns>初始化后的实体。</returns>
        internal T CreateEntityInternal<T>(Entity parent, long id) where T : Entity, new()
        {
            var entity = m_entityPool.Rent<T>();
            entity.Initialize(this, parent, id == 0 ? NextEntityId() : id, NextRuntimeId());
            RegisterEntity(entity);
            return entity;
        }

        /// <summary>
        /// 从对象池创建或租用运行时类型实体，并完成世界状态初始化。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="parent">父实体。</param>
        /// <param name="id">业务实体 id。传 0 时自动分配。</param>
        /// <returns>初始化后的实体。</returns>
        internal Entity CreateEntityInternal(Type type, Entity parent, long id)
        {
            var entity = m_entityPool.Rent(type);
            entity.Initialize(this, parent, id == 0 ? NextEntityId() : id, NextRuntimeId());
            RegisterEntity(entity);
            return entity;
        }

        #endregion

        #region Internal Pool

        /// <summary>
        /// 将已销毁实体归还到当前世界对象池。
        /// </summary>
        /// <param name="entity">待回收实体。</param>
        internal void ReturnEntity(Entity entity)
        {
            if (entity == null || !entity.IsDisposed || entity is INonPooledEntity)
            {
                return;
            }

            m_entityPool.Return(entity);
        }

        #endregion

        #region Internal Query

        /// <summary>
        /// 从世界运行时索引中移除实体。
        /// </summary>
        /// <param name="runtimeId">运行时唯一 id。</param>
        internal void UnregisterEntity(long runtimeId)
        {
            if (runtimeId == 0)
            {
                return;
            }

            m_entities.Remove(runtimeId);
        }

        #endregion

        #region Id

        /// <summary>
        /// 分配下一个运行时唯一 id。
        /// </summary>
        /// <returns>新的运行时唯一 id。</returns>
        private long NextRuntimeId() => ++m_nextRuntimeId;

        #endregion

        #region Internal

        /// <summary>
        /// 将实体注册到世界运行时索引。
        /// </summary>
        /// <param name="entity">待注册实体。</param>
        private void RegisterEntity(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            m_entities.Add(entity.RuntimeId, entity);
        }

        #endregion
    }
}
