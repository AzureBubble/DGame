using System;
using System.Collections.Generic;

namespace GameBattle
{
    #region Entity Interface

    /// <summary>
    /// GameBattle 轻量实体接口。
    /// </summary>
    public interface IEntity : IDisposable
    {
        /// <summary>
        /// 业务实体 id。
        /// </summary>
        long Id { get; }

        /// <summary>
        /// 当前 World 内唯一的运行时 id。
        /// </summary>
        long RuntimeId { get; }

        /// <summary>
        /// 实体是否已经销毁。
        /// </summary>
        bool IsDisposed { get; }
    }

    /// <summary>
    /// 标记实体不参与对象池复用。
    /// <remarks>实现该接口的实体始终新建，销毁后不回池，预热会被忽略。</remarks>
    /// </summary>
    public interface INonPooledEntity
    {
    }

    #endregion

    /// <summary>
    /// GameBattle 轻量实体基类，同时支持子实体和组件组合。
    /// </summary>
    public abstract class Entity : IEntity
    {
        #region Fields

        /// <summary>
        /// 组件表，按组件精确类型索引。
        /// </summary>
        private readonly Dictionary<Type, Entity> m_components = new();

        /// <summary>
        /// 子实体表，按 RuntimeId 索引。
        /// </summary>
        private readonly Dictionary<long, Entity> m_children = new();

        /// <summary>
        /// 组件稳定遍历顺序。
        /// </summary>
        private readonly List<Entity> m_componentOrder = new();

        /// <summary>
        /// 子实体稳定遍历顺序。
        /// </summary>
        private readonly List<Entity> m_childOrder = new();

        /// <summary>
        /// 销毁子实体或组件时复用的临时列表。
        /// </summary>
        private readonly List<Entity> m_disposeBuffer = new();

        /// <summary>
        /// 当前实体是否正在执行销毁流程。
        /// </summary>
        private bool m_isDisposing;

        #endregion

        #region Properties

        /// <summary>
        /// 业务实体 id。
        /// </summary>
        public long Id { get; private set; }

        /// <summary>
        /// 当前 World 内唯一的运行时 id。
        /// </summary>
        public long RuntimeId { get; private set; }

        /// <summary>
        /// 实体所属世界。
        /// </summary>
        public World World { get; private set; }

        /// <summary>
        /// 父实体。World 自身没有父实体。
        /// </summary>
        public Entity Parent { get; private set; }

        /// <summary>
        /// 实体是否已经销毁。
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 当前组件数量。
        /// </summary>
        public int ComponentCount => m_componentOrder.Count;

        /// <summary>
        /// 当前子实体数量。
        /// </summary>
        public int ChildCount => m_childOrder.Count;

        /// <summary>
        /// 组件字典，只允许框架内部访问。
        /// </summary>
        internal IReadOnlyDictionary<Type, Entity> Components => m_components;

        /// <summary>
        /// 子实体字典，只允许框架内部访问。
        /// </summary>
        internal IReadOnlyDictionary<long, Entity> Children => m_children;

        /// <summary>
        /// 实体是否已经归还到对象池。
        /// </summary>
        internal bool IsInPool { get; private set; }

        #endregion

        #region Create

        /// <summary>
        /// 在指定世界创建运行时类型实体。
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>创建后的实体。</returns>
        public static Entity Create(World world, Type type)
            => Create(world, type, 0);

        /// <summary>
        /// 在指定世界创建运行时类型实体，并指定业务 id。
        /// </summary>
        /// <param name="world">目标世界。</param>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的实体。</returns>
        public static Entity Create(World world, Type type, long id)
        {
            ValidateEntityType(type);
            return ValidateWorld(world).AddChildWithId(type, id);
        }

        /// <summary>
        /// 在指定世界创建实体。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T>(World world) where T : Entity, new()
            => Create<T>(world, 0);

        /// <summary>
        /// 在指定世界创建实体，并指定业务 id。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T>(World world, long id) where T : Entity, new()
        {
            return ValidateWorld(world).AddChildWithId<T>(id);
        }

        /// <summary>
        /// 在指定世界创建实体，并传入一个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1>(World world, T1 value1) where T : Entity, IAwake<T1>, new()
            => Create<T, T1>(world, 0, value1);

        /// <summary>
        /// 在指定世界创建实体，指定业务 id，并传入一个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1>(World world, long id, T1 value1) where T : Entity, IAwake<T1>, new()
        {
            return ValidateWorld(world).AddChildWithId<T, T1>(id, value1);
        }

        /// <summary>
        /// 在指定世界创建实体，并传入两个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1, T2>(World world, T1 value1, T2 value2)
            where T : Entity, IAwake<T1, T2>, new()
            => Create<T, T1, T2>(world, 0, value1, value2);

        /// <summary>
        /// 在指定世界创建实体，指定业务 id，并传入两个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1, T2>(World world, long id, T1 value1, T2 value2)
            where T : Entity, IAwake<T1, T2>, new()
        {
            return ValidateWorld(world).AddChildWithId<T, T1, T2>(id, value1, value2);
        }

        /// <summary>
        /// 在指定世界创建实体，并传入三个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1, T2, T3>(World world, T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
            => Create<T, T1, T2, T3>(world, 0, value1, value2, value3);

        /// <summary>
        /// 在指定世界创建实体，指定业务 id，并传入三个 Awake 参数。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="world">目标世界。</param>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的实体。</returns>
        public static T Create<T, T1, T2, T3>(World world, long id, T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
        {
            return ValidateWorld(world).AddChildWithId<T, T1, T2, T3>(id, value1, value2, value3);
        }

        #endregion

        #region Validation

        /// <summary>
        /// 校验并返回有效世界。
        /// </summary>
        /// <param name="world">待校验世界。</param>
        /// <returns>有效世界。</returns>
        private static World ValidateWorld(World world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            return world;
        }

        /// <summary>
        /// 校验运行时类型是否为实体类型。
        /// </summary>
        /// <param name="type">待校验类型。</param>
        private static void ValidateEntityType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(Entity).IsAssignableFrom(type))
            {
                throw new ArgumentException($"Type must inherit from Entity: {type?.FullName}", nameof(type));
            }
        }

        #endregion

        #region Initialize

        /// <summary>
        /// 初始化实体所属世界、父子关系和运行时状态。
        /// </summary>
        /// <param name="world">实体所属世界。</param>
        /// <param name="parent">父实体。</param>
        /// <param name="id">业务实体 id。</param>
        /// <param name="runtimeId">运行时唯一 id。</param>
        internal void Initialize(World world, Entity parent, long id, long runtimeId)
        {
            World = ValidateWorld(world);
            Parent = parent;
            Id = id;
            RuntimeId = runtimeId;
            IsDisposed = false;
            IsInPool = false;
            m_isDisposing = false;
            m_components.Clear();
            m_children.Clear();
            m_componentOrder.Clear();
            m_childOrder.Clear();
            m_disposeBuffer.Clear();
            OnRentFromPool();
        }

        #endregion

        #region Pool

        /// <summary>
        /// 尝试标记实体已经归还到对象池。
        /// </summary>
        /// <returns>状态切换成功时返回 true。</returns>
        internal bool TryMarkReturnedToPool()
        {
            if (IsInPool)
            {
                return false;
            }

            IsInPool = true;
            return true;
        }

        /// <summary>
        /// 标记实体已经从对象池租出。
        /// </summary>
        internal void MarkRentedFromPool()
        {
            IsInPool = false;
        }

        #endregion

        #region Reference

        /// <summary>
        /// 创建当前实体的安全引用。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>实体安全引用。</returns>
        public EntityRef<T> ToRef<T>() where T : Entity => new(World, RuntimeId, this as T);

        #endregion

        #region Stable Traversal

        /// <summary>
        /// 按稳定添加顺序获取组件。
        /// </summary>
        /// <param name="index">组件下标。</param>
        /// <returns>指定下标的组件。</returns>
        public Entity GetComponentAt(int index) => m_componentOrder[index];

        /// <summary>
        /// 按稳定添加顺序获取子实体。
        /// </summary>
        /// <param name="index">子实体下标。</param>
        /// <returns>指定下标的子实体。</returns>
        public Entity GetChildAt(int index) => m_childOrder[index];

        #endregion

        #region Component

        /// <summary>
        /// 添加组件并执行无参 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>创建后的组件。</returns>
        public T AddComponent<T>() where T : Entity, new()
        {
            var component = AddComponentInternal<T>(Id);
            World.EntitySystem.Awake(component);
            return component;
        }

        /// <summary>
        /// 添加组件并执行带一个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponent<T, T1>(T1 value1) where T : Entity, IAwake<T1>, new()
        {
            var component = AddComponentInternal<T>(Id);
            World.EntitySystem.Awake(component, value1);
            return component;
        }

        /// <summary>
        /// 添加组件并执行带两个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponent<T, T1, T2>(T1 value1, T2 value2) where T : Entity, IAwake<T1, T2>, new()
        {
            var component = AddComponentInternal<T>(Id);
            World.EntitySystem.Awake(component, value1, value2);
            return component;
        }

        /// <summary>
        /// 添加组件并执行带三个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponent<T, T1, T2, T3>(T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
        {
            var component = AddComponentInternal<T>(Id);
            World.EntitySystem.Awake(component, value1, value2, value3);
            return component;
        }

        /// <summary>
        /// 添加运行时类型组件并执行无参 Awake。
        /// </summary>
        /// <param name="type">组件运行时类型。</param>
        /// <returns>创建后的组件。</returns>
        public Entity AddComponent(Type type) => AddComponentWithId(type, Id);

        /// <summary>
        /// 添加运行时类型组件，指定业务 id，并执行无参 Awake。
        /// </summary>
        /// <param name="type">组件运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的组件。</returns>
        public Entity AddComponentWithId(Type type, long id)
        {
            var component = AddComponentInternal(type, id);
            World.EntitySystem.Awake(component);
            return component;
        }

        /// <summary>
        /// 添加组件，指定业务 id，并执行无参 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponentWithId<T>(long id) where T : Entity, new()
        {
            var component = AddComponentInternal<T>(id);
            World.EntitySystem.Awake(component);
            return component;
        }

        /// <summary>
        /// 添加组件，指定业务 id，并执行带一个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponentWithId<T, T1>(long id, T1 value1) where T : Entity, IAwake<T1>, new()
        {
            var component = AddComponentInternal<T>(id);
            World.EntitySystem.Awake(component, value1);
            return component;
        }

        /// <summary>
        /// 添加组件，指定业务 id，并执行带两个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponentWithId<T, T1, T2>(long id, T1 value1, T2 value2) where T : Entity, IAwake<T1, T2>, new()
        {
            var component = AddComponentInternal<T>(id);
            World.EntitySystem.Awake(component, value1, value2);
            return component;
        }

        /// <summary>
        /// 添加组件，指定业务 id，并执行带三个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的组件。</returns>
        public T AddComponentWithId<T, T1, T2, T3>(long id, T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
        {
            var component = AddComponentInternal<T>(id);
            World.EntitySystem.Awake(component, value1, value2, value3);
            return component;
        }

        /// <summary>
        /// 判断是否存在指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>存在时返回 true。</returns>
        public bool HasComponent<T>() where T : Entity => m_components.ContainsKey(typeof(T));

        /// <summary>
        /// 获取指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>匹配的组件，不存在时返回 null。</returns>
        public T GetComponent<T>() where T : Entity
            => m_components.TryGetValue(typeof(T), out var component) ? (T)component : null;

        /// <summary>
        /// 获取指定运行时类型组件。
        /// </summary>
        /// <param name="type">组件运行时类型。</param>
        /// <returns>匹配的组件，不存在时返回 null。</returns>
        public Entity GetComponent(Type type)
        {
            if (type == null)
            {
                return null;
            }

            return m_components.TryGetValue(type, out var component) ? component : null;
        }

        /// <summary>
        /// 移除并销毁指定类型组件。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <returns>成功移除时返回 true。</returns>
        public bool RemoveComponent<T>() where T : Entity
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                return false;
            }

            component.Dispose();
            return true;
        }

        #endregion

        #region Child

        /// <summary>
        /// 添加子实体并执行无参 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <returns>创建后的子实体。</returns>
        public T AddChild<T>() where T : Entity, new()
        {
            var child = AddChildInternal<T>(World.NextEntityId());
            World.EntitySystem.Awake(child);
            return child;
        }

        /// <summary>
        /// 添加子实体并执行带一个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChild<T, T1>(T1 value1) where T : Entity, IAwake<T1>, new()
        {
            var child = AddChildInternal<T>(World.NextEntityId());
            World.EntitySystem.Awake(child, value1);
            return child;
        }

        /// <summary>
        /// 添加子实体并执行带两个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChild<T, T1, T2>(T1 value1, T2 value2) where T : Entity, IAwake<T1, T2>, new()
        {
            var child = AddChildInternal<T>(World.NextEntityId());
            World.EntitySystem.Awake(child, value1, value2);
            return child;
        }

        /// <summary>
        /// 添加子实体并执行带三个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChild<T, T1, T2, T3>(T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
        {
            var child = AddChildInternal<T>(World.NextEntityId());
            World.EntitySystem.Awake(child, value1, value2, value3);
            return child;
        }

        /// <summary>
        /// 添加运行时类型子实体并执行无参 Awake。
        /// </summary>
        /// <param name="type">子实体运行时类型。</param>
        /// <returns>创建后的子实体。</returns>
        public Entity AddChild(Type type)
            => AddChildWithId(type, World.NextEntityId());

        /// <summary>
        /// 添加运行时类型子实体，指定业务 id，并执行无参 Awake。
        /// </summary>
        /// <param name="type">子实体运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的子实体。</returns>
        public Entity AddChildWithId(Type type, long id)
        {
            var child = AddChildInternal(type, id);
            World.EntitySystem.Awake(child);
            return child;
        }

        /// <summary>
        /// 添加子实体，指定业务 id，并执行无参 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChildWithId<T>(long id) where T : Entity, new()
        {
            var child = AddChildInternal<T>(id);
            World.EntitySystem.Awake(child);
            return child;
        }

        /// <summary>
        /// 添加子实体，指定业务 id，并执行带一个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChildWithId<T, T1>(long id, T1 value1) where T : Entity, IAwake<T1>, new()
        {
            var child = AddChildInternal<T>(id);
            World.EntitySystem.Awake(child, value1);
            return child;
        }

        /// <summary>
        /// 添加子实体，指定业务 id，并执行带两个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChildWithId<T, T1, T2>(long id, T1 value1, T2 value2) where T : Entity, IAwake<T1, T2>, new()
        {
            var child = AddChildInternal<T>(id);
            World.EntitySystem.Awake(child, value1, value2);
            return child;
        }

        /// <summary>
        /// 添加子实体，指定业务 id，并执行带三个参数的 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        /// <returns>创建后的子实体。</returns>
        public T AddChildWithId<T, T1, T2, T3>(long id, T1 value1, T2 value2, T3 value3)
            where T : Entity, IAwake<T1, T2, T3>, new()
        {
            var child = AddChildInternal<T>(id);
            World.EntitySystem.Awake(child, value1, value2, value3);
            return child;
        }

        /// <summary>
        /// 通过 RuntimeId 获取指定类型子实体。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <param name="runtimeId">运行时唯一 id。</param>
        /// <returns>匹配的子实体，不存在时返回 null。</returns>
        public T GetChild<T>(long runtimeId) where T : Entity
            => m_children.TryGetValue(runtimeId, out var child) ? child as T : null;

        #endregion

        #region Lifecycle

        /// <summary>
        /// 销毁实体、子实体和组件，并在可复用时归还到对象池。
        /// </summary>
        public virtual void Dispose()
        {
            if (IsDisposed || m_isDisposing)
            {
                return;
            }

            m_isDisposing = true;
            DisposeEntities(m_childOrder, m_children);
            DisposeEntities(m_componentOrder, m_components);
            World?.EntitySystem.Destroy(this);

            var world = World;
            var parent = Parent;
            var runtimeId = RuntimeId;

            parent?.Detach(this);
            world?.UnregisterEntity(runtimeId);

            OnReturnToPool();
            ResetState();

            world?.ReturnEntity(this);
        }

        #endregion

        #region Internal Component

        /// <summary>
        /// 创建组件但不触发 Awake。
        /// </summary>
        /// <typeparam name="T">组件类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的组件。</returns>
        private T AddComponentInternal<T>(long id) where T : Entity, new()
        {
            EnsureAlive();
            var type = typeof(T);

            if (m_components.ContainsKey(type))
            {
                throw new InvalidOperationException($"Entity already has component: {type.FullName}");
            }

            var component = World.CreateEntityInternal<T>(this, id);
            m_components.Add(type, component);
            m_componentOrder.Add(component);
            return component;
        }

        /// <summary>
        /// 创建运行时类型组件但不触发 Awake。
        /// </summary>
        /// <param name="type">组件运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的组件。</returns>
        private Entity AddComponentInternal(Type type, long id)
        {
            EnsureAlive();
            ValidateEntityType(type);

            if (m_components.ContainsKey(type))
            {
                throw new InvalidOperationException($"Entity already has component: {type.FullName}");
            }

            var component = World.CreateEntityInternal(type, this, id);
            m_components.Add(type, component);
            m_componentOrder.Add(component);
            return component;
        }

        #endregion

        #region Internal Child

        /// <summary>
        /// 创建子实体但不触发 Awake。
        /// </summary>
        /// <typeparam name="T">子实体类型。</typeparam>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的子实体。</returns>
        private T AddChildInternal<T>(long id) where T : Entity, new()
        {
            EnsureAlive();

            var child = World.CreateEntityInternal<T>(this, id);
            m_children.Add(child.RuntimeId, child);
            m_childOrder.Add(child);
            return child;
        }

        /// <summary>
        /// 创建运行时类型子实体但不触发 Awake。
        /// </summary>
        /// <param name="type">子实体运行时类型。</param>
        /// <param name="id">业务实体 id。</param>
        /// <returns>创建后的子实体。</returns>
        private Entity AddChildInternal(Type type, long id)
        {
            EnsureAlive();
            ValidateEntityType(type);

            var child = World.CreateEntityInternal(type, this, id);
            m_children.Add(child.RuntimeId, child);
            m_childOrder.Add(child);
            return child;
        }

        /// <summary>
        /// 从当前实体的组件或子实体集合中移除指定实体。
        /// </summary>
        /// <param name="entity">待解绑实体。</param>
        private void Detach(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            var type = entity.GetType();
            if (m_components.TryGetValue(type, out var component) && ReferenceEquals(component, entity))
            {
                m_components.Remove(type);
                m_componentOrder.Remove(entity);
                return;
            }

            if (m_children.TryGetValue(entity.RuntimeId, out var child) && ReferenceEquals(child, entity))
            {
                m_children.Remove(entity.RuntimeId);
                m_childOrder.Remove(entity);
            }
        }

        #endregion

        #region Internal Lifecycle

        /// <summary>
        /// 确认实体仍处于可操作状态。
        /// </summary>
        private void EnsureAlive()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            if (m_isDisposing)
            {
                throw new InvalidOperationException($"Entity is disposing: {GetType().FullName}");
            }

            if (World == null)
            {
                throw new InvalidOperationException($"Entity world is null: {GetType().FullName}");
            }
        }

        /// <summary>
        /// 按稳定顺序销毁子实体集合。
        /// </summary>
        /// <param name="orderedEntities">稳定顺序列表。</param>
        /// <param name="entities">子实体索引表。</param>
        private void DisposeEntities(List<Entity> orderedEntities, Dictionary<long, Entity> entities)
        {
            if (orderedEntities.Count == 0 && entities.Count == 0)
            {
                return;
            }

            m_disposeBuffer.Clear();
            for (var i = 0; i < orderedEntities.Count; i++)
            {
                m_disposeBuffer.Add(orderedEntities[i]);
            }

            for (var i = 0; i < m_disposeBuffer.Count; i++)
            {
                m_disposeBuffer[i]?.Dispose();
            }

            entities.Clear();
            orderedEntities.Clear();
            m_disposeBuffer.Clear();
        }

        /// <summary>
        /// 按稳定顺序销毁组件集合。
        /// </summary>
        /// <param name="orderedEntities">稳定顺序列表。</param>
        /// <param name="entities">组件索引表。</param>
        private void DisposeEntities(List<Entity> orderedEntities, Dictionary<Type, Entity> entities)
        {
            if (orderedEntities.Count == 0 && entities.Count == 0)
            {
                return;
            }

            m_disposeBuffer.Clear();
            for (var i = 0; i < orderedEntities.Count; i++)
            {
                m_disposeBuffer.Add(orderedEntities[i]);
            }

            for (var i = 0; i < m_disposeBuffer.Count; i++)
            {
                m_disposeBuffer[i]?.Dispose();
            }

            entities.Clear();
            orderedEntities.Clear();
            m_disposeBuffer.Clear();
        }

        /// <summary>
        /// 重置实体状态，使其进入已销毁且可回池状态。
        /// </summary>
        private void ResetState()
        {
            Id = 0;
            RuntimeId = 0;
            Parent = null;
            World = null;
            IsDisposed = true;
            m_isDisposing = false;
            m_components.Clear();
            m_children.Clear();
            m_componentOrder.Clear();
            m_childOrder.Clear();
            m_disposeBuffer.Clear();
        }

        #endregion

        #region Pool Hooks

        /// <summary>
        /// 从对象池租出并初始化完成后调用。
        /// </summary>
        protected virtual void OnRentFromPool()
        {
        }

        /// <summary>
        /// 即将归还对象池前调用。
        /// </summary>
        protected virtual void OnReturnToPool()
        {
        }

        #endregion
    }
}
