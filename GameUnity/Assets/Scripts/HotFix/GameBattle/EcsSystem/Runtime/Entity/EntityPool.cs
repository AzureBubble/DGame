using System;
using System.Collections.Generic;

namespace GameBattle
{
    /// <summary>
    /// World 私有实体对象池，按实体精确类型缓存可复用实例。
    /// </summary>
    internal sealed class EntityPool
    {
        #region Fields

        /// <summary>
        /// 新建类型栈时的默认容量。
        /// </summary>
        private const int DEFAULT_STACK_CAPACITY = 4;

        /// <summary>
        /// 按实体类型保存的对象池栈。
        /// </summary>
        private readonly Dictionary<Type, Stack<Entity>> m_entities = new();

        /// <summary>
        /// 低频反射创建路径的构造函数缓存。
        /// </summary>
        private readonly Dictionary<Type, Func<Entity>> m_factories = new();

        /// <summary>
        /// 每种实体类型最多缓存的数量。
        /// </summary>
        private readonly int m_maxCountPerType;

        #endregion

        #region Create

        /// <summary>
        /// 创建实体对象池。
        /// </summary>
        /// <param name="maxCountPerType">每种实体类型最多缓存的数量。</param>
        public EntityPool(int maxCountPerType)
        {
            m_maxCountPerType = Math.Max(0, maxCountPerType);
        }

        #endregion

        #region Rent

        /// <summary>
        /// 租用指定泛型实体。高频路径优先使用该方法，避免反射创建。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>可初始化使用的实体实例。</returns>
        public T Rent<T>() where T : Entity, new()
        {
            var type = typeof(T);
            if (!CanPool(type))
            {
                return new T();
            }

            if (m_entities.TryGetValue(type, out var stack) && stack.Count > 0)
            {
                var entity = (T)stack.Pop();
                entity.MarkRentedFromPool();
                return entity;
            }

            return new T();
        }

        /// <summary>
        /// 租用指定运行时类型实体。该路径保留给工具或低频动态类型创建。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>可初始化使用的实体实例。</returns>
        public Entity Rent(Type type)
        {
            ValidateEntityType(type);
            if (!CanPool(type))
            {
                return GetOrCreateFactory(type)();
            }

            if (m_entities.TryGetValue(type, out var stack) && stack.Count > 0)
            {
                var entity = stack.Pop();
                entity.MarkRentedFromPool();
                return entity;
            }

            return GetOrCreateFactory(type)();
        }

        #endregion

        #region Return

        /// <summary>
        /// 归还实体到池中。
        /// </summary>
        /// <param name="entity">待回收实体。</param>
        public void Return(Entity entity)
        {
            if (entity == null || !CanPool(entity.GetType()))
            {
                return;
            }

            if (entity.IsInPool)
            {
                BLogger.Error($"Entity already returned to pool: {entity.GetType().FullName}");
                return;
            }

            var type = entity.GetType();
            var stack = GetOrCreateStack(type, DEFAULT_STACK_CAPACITY);
            if (stack.Count >= m_maxCountPerType)
            {
                return;
            }

            if (!entity.TryMarkReturnedToPool())
            {
                BLogger.Error($"Entity already returned to pool: {type.FullName}");
                return;
            }

            stack.Push(entity);
        }

        #endregion

        #region Prewarm

        /// <summary>
        /// 预热指定泛型实体池。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <param name="count">目标缓存数量。</param>
        public void Prewarm<T>(int count) where T : Entity, new()
        {
            var type = typeof(T);
            if (count <= 0 || !CanPool(type))
            {
                return;
            }

            var targetCount = Math.Min(count, m_maxCountPerType);
            var stack = GetOrCreateStack(type, targetCount);
            for (var i = stack.Count; i < targetCount; i++)
            {
                var entity = new T();
                if (entity.TryMarkReturnedToPool())
                {
                    stack.Push(entity);
                }
            }
        }

        /// <summary>
        /// 预热指定运行时类型实体池。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="count">目标缓存数量。</param>
        public void Prewarm(Type type, int count)
        {
            ValidateEntityType(type);
            if (count <= 0 || !CanPool(type))
            {
                return;
            }

            var targetCount = Math.Min(count, m_maxCountPerType);
            var stack = GetOrCreateStack(type, targetCount);
            var factory = GetOrCreateFactory(type);
            for (var i = stack.Count; i < targetCount; i++)
            {
                var entity = factory();
                if (entity.TryMarkReturnedToPool())
                {
                    stack.Push(entity);
                }
            }
        }

        #endregion

        #region Query

        /// <summary>
        /// 获取指定泛型实体池内数量。
        /// </summary>
        /// <typeparam name="T">实体类型。</typeparam>
        /// <returns>池内实体数量。</returns>
        public int Count<T>() where T : Entity
        {
            var type = typeof(T);
            return !CanPool(type) || !m_entities.TryGetValue(type, out var stack) ? 0 : stack.Count;
        }

        /// <summary>
        /// 获取指定运行时类型实体池内数量。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>池内实体数量。</returns>
        public int Count(Type type)
        {
            ValidateEntityType(type);
            if (!CanPool(type))
            {
                return 0;
            }

            return m_entities.TryGetValue(type, out var stack) ? stack.Count : 0;
        }

        #endregion

        #region Lifecycle

        /// <summary>
        /// 清空所有实体池栈。
        /// </summary>
        public void Clear()
        {
            foreach (var pair in m_entities)
            {
                pair.Value.Clear();
            }

            m_entities.Clear();
        }

        #endregion

        #region Internal

        /// <summary>
        /// 获取或创建指定类型的对象池栈。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <param name="capacity">期望初始容量。</param>
        /// <returns>实体对象池栈。</returns>
        private Stack<Entity> GetOrCreateStack(Type type, int capacity)
        {
            if (m_entities.TryGetValue(type, out var stack))
            {
                return stack;
            }

            stack = new Stack<Entity>(Math.Max(DEFAULT_STACK_CAPACITY, capacity));
            m_entities.Add(type, stack);
            return stack;
        }

        /// <summary>
        /// 获取或创建指定实体类型的无参构造工厂。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>实体创建委托。</returns>
        private Func<Entity> GetOrCreateFactory(Type type)
        {
            if (m_factories.TryGetValue(type, out var factory))
            {
                return factory;
            }

            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                throw new MissingMethodException(type.FullName, ".ctor()");
            }

            factory = () => (Entity)constructor.Invoke(null);
            m_factories.Add(type, factory);
            return factory;
        }

        #endregion

        #region Pool Policy

        /// <summary>
        /// 判断指定实体类型是否可以参与对象池复用。
        /// </summary>
        /// <param name="type">实体运行时类型。</param>
        /// <returns>可以参与池化时返回 true。</returns>
        private bool CanPool(Type type)
            => m_maxCountPerType > 0 && !typeof(INonPooledEntity).IsAssignableFrom(type);

        #endregion

        #region Validation

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
                throw new ArgumentException($"Type must inherit from {nameof(Entity)}: {type.FullName}", nameof(type));
            }
        }

        #endregion
    }
}
