using System;
using System.Collections.Generic;

namespace GameBattle
{
    /// <summary>
    /// 按实体类型保存外置生命周期系统。
    /// </summary>
    internal sealed class EntitySystemCollection
    {
        #region Fields

        /// <summary>
        /// 外置 Awake 系统表。
        /// </summary>
        private readonly Dictionary<Type, List<IAwakeSystemBase>> m_awakeSystems = new();

        /// <summary>
        /// 外置 Update 系统表。
        /// </summary>
        private readonly Dictionary<Type, List<IUpdateSystem>> m_updateSystems = new();

        /// <summary>
        /// 外置 LateUpdate 系统表。
        /// </summary>
        private readonly Dictionary<Type, List<ILateUpdateSystem>> m_lateUpdateSystems = new();

        /// <summary>
        /// 外置 Destroy 系统表。
        /// </summary>
        private readonly Dictionary<Type, List<IDestroySystem>> m_destroySystems = new();

        /// <summary>
        /// 外置系统注册顺序，用于同阶段同优先级时保持稳定执行。
        /// </summary>
        private readonly Dictionary<IEntitySystem, int> m_registrationOrders = new();

        /// <summary>
        /// 下一个注册顺序值。
        /// </summary>
        private int m_nextRegistrationOrder;

        #endregion

        #region Register

        /// <summary>
        /// 注册外置实体系统到对应生命周期表。
        /// </summary>
        /// <param name="system">外置实体系统。</param>
        public void Register(IEntitySystem system)
        {
            if (system == null)
            {
                throw new ArgumentNullException(nameof(system));
            }

            var registered = false;

            if (system is IAwakeSystemBase awakeSystem)
            {
                Add(m_awakeSystems, awakeSystem.EntityType, awakeSystem);
                registered = true;
            }

            if (system is IUpdateSystem updateSystem)
            {
                AddOrdered(m_updateSystems, updateSystem.EntityType, updateSystem);
                registered = true;
            }

            if (system is ILateUpdateSystem lateUpdateSystem)
            {
                AddOrdered(m_lateUpdateSystems, lateUpdateSystem.EntityType, lateUpdateSystem);
                registered = true;
            }

            if (system is IDestroySystem destroySystem)
            {
                Add(m_destroySystems, destroySystem.EntityType, destroySystem);
                registered = true;
            }

            if (!registered)
            {
                BLogger.Error($"Unsupported entity system: {system.GetType().FullName}");
            }
        }

        #endregion

        #region Query

        /// <summary>
        /// 判断指定实体类型是否存在外置 Update 系统。
        /// </summary>
        /// <param name="entityType">实体类型。</param>
        /// <returns>存在时返回 true。</returns>
        public bool HasUpdateSystem(Type entityType) => m_updateSystems.ContainsKey(entityType);

        /// <summary>
        /// 判断指定实体类型是否存在外置 LateUpdate 系统。
        /// </summary>
        /// <param name="entityType">实体类型。</param>
        /// <returns>存在时返回 true。</returns>
        public bool HasLateUpdateSystem(Type entityType) => m_lateUpdateSystems.ContainsKey(entityType);

        #endregion

        #region Invoke Awake

        /// <summary>
        /// 调用指定实体类型的无参外置 Awake 系统。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void InvokeAwake(Entity entity)
        {
            if (entity == null || !m_awakeSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] is IAwakeSystem awakeSystem)
                {
                    awakeSystem.Invoke(entity);
                }
            }
        }

        /// <summary>
        /// 调用指定实体类型的带一个参数外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        public void InvokeAwake<T1>(Entity entity, T1 value1)
        {
            if (entity == null || !m_awakeSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] is IAwakeSystem<T1> awakeSystem)
                {
                    awakeSystem.Invoke(entity, value1);
                }
            }
        }

        /// <summary>
        /// 调用指定实体类型的带两个参数外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        public void InvokeAwake<T1, T2>(Entity entity, T1 value1, T2 value2)
        {
            if (entity == null || !m_awakeSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] is IAwakeSystem<T1, T2> awakeSystem)
                {
                    awakeSystem.Invoke(entity, value1, value2);
                }
            }
        }

        /// <summary>
        /// 调用指定实体类型的带三个参数外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        public void InvokeAwake<T1, T2, T3>(Entity entity, T1 value1, T2 value2, T3 value3)
        {
            if (entity == null || !m_awakeSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                if (systems[i] is IAwakeSystem<T1, T2, T3> awakeSystem)
                {
                    awakeSystem.Invoke(entity, value1, value2, value3);
                }
            }
        }

        #endregion

        #region Invoke Update

        /// <summary>
        /// 调用指定实体类型的外置 Update 系统。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        public void InvokeUpdate(Entity entity, in UpdateContext context)
        {
            if (entity == null || !m_updateSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                systems[i].Invoke(entity, context);
            }
        }

        /// <summary>
        /// 调用指定实体类型的外置 LateUpdate 系统。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        public void InvokeLateUpdate(Entity entity, in UpdateContext context)
        {
            if (entity == null || !m_lateUpdateSystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                systems[i].Invoke(entity, context);
            }
        }

        #endregion

        #region Invoke Destroy

        /// <summary>
        /// 调用指定实体类型的外置 Destroy 系统。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void InvokeDestroy(Entity entity)
        {
            if (entity == null || !m_destroySystems.TryGetValue(entity.GetType(), out var systems))
            {
                return;
            }

            for (var i = 0; i < systems.Count; i++)
            {
                systems[i].Invoke(entity);
            }
        }

        #endregion

        #region Add

        /// <summary>
        /// 添加不需要排序的外置系统。
        /// </summary>
        /// <typeparam name="T">外置系统接口类型。</typeparam>
        /// <param name="systems">系统表。</param>
        /// <param name="entityType">实体类型。</param>
        /// <param name="system">外置系统实例。</param>
        private void Add<T>(Dictionary<Type, List<T>> systems, Type entityType, T system)
            where T : IEntitySystem
        {
            var list = GetOrCreateList(systems, entityType);
            EnsureNotRegistered(list, entityType, system);
            RegisterOrder(system);
            list.Add(system);
        }

        /// <summary>
        /// 添加需要按阶段和优先级排序的外置系统。
        /// </summary>
        /// <typeparam name="T">外置系统接口类型。</typeparam>
        /// <param name="systems">系统表。</param>
        /// <param name="entityType">实体类型。</param>
        /// <param name="system">外置系统实例。</param>
        private void AddOrdered<T>(Dictionary<Type, List<T>> systems, Type entityType, T system)
            where T : IEntitySystem
        {
            var list = GetOrCreateList(systems, entityType);
            EnsureNotRegistered(list, entityType, system);
            RegisterOrder(system);
            list.Add(system);
            list.Sort(CompareSystemOrder);
        }

        /// <summary>
        /// 获取或创建指定实体类型的系统列表。
        /// </summary>
        /// <typeparam name="T">外置系统接口类型。</typeparam>
        /// <param name="systems">系统表。</param>
        /// <param name="entityType">实体类型。</param>
        /// <returns>系统列表。</returns>
        private static List<T> GetOrCreateList<T>(Dictionary<Type, List<T>> systems, Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            if (systems.TryGetValue(entityType, out var list))
            {
                return list;
            }

            list = new List<T>();
            systems.Add(entityType, list);
            return list;
        }

        #endregion

        #region Validation

        /// <summary>
        /// 校验同一实体类型下不能重复注册同一个系统类型。
        /// </summary>
        /// <typeparam name="T">外置系统接口类型。</typeparam>
        /// <param name="list">已有系统列表。</param>
        /// <param name="entityType">实体类型。</param>
        /// <param name="system">待注册系统。</param>
        private static void EnsureNotRegistered<T>(List<T> list, Type entityType, T system)
            where T : IEntitySystem
        {
            var systemType = system.GetType();
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].GetType() == systemType)
                {
                    throw new InvalidOperationException(
                        $"Entity system already registered: {systemType.FullName}, entity: {entityType.FullName}");
                }
            }
        }

        #endregion

        #region Order

        /// <summary>
        /// 记录外置系统首次注册顺序。
        /// </summary>
        /// <param name="system">外置系统实例。</param>
        private void RegisterOrder(IEntitySystem system)
        {
            if (m_registrationOrders.ContainsKey(system))
            {
                return;
            }

            m_registrationOrders.Add(system, m_nextRegistrationOrder++);
        }

        /// <summary>
        /// 比较两个外置系统的执行顺序。
        /// </summary>
        /// <typeparam name="T">外置系统接口类型。</typeparam>
        /// <param name="left">左侧系统。</param>
        /// <param name="right">右侧系统。</param>
        /// <returns>排序比较结果。</returns>
        private int CompareSystemOrder<T>(T left, T right)
            where T : IEntitySystem
        {
            var leftPhase = GetPhase(left);
            var rightPhase = GetPhase(right);
            var phaseCompare = leftPhase.CompareTo(rightPhase);
            if (phaseCompare != 0)
            {
                return phaseCompare;
            }

            var priorityCompare = GetPriority(left).CompareTo(GetPriority(right));
            if (priorityCompare != 0)
            {
                return priorityCompare;
            }

            return m_registrationOrders[left].CompareTo(m_registrationOrders[right]);
        }

        /// <summary>
        /// 获取系统阶段。
        /// </summary>
        /// <param name="system">外置系统实例。</param>
        /// <returns>系统阶段。</returns>
        private static SystemPhase GetPhase(IEntitySystem system)
            => system is ISystemOrder order ? order.Phase : SystemPhase.Logic;

        /// <summary>
        /// 获取系统优先级。
        /// </summary>
        /// <param name="system">外置系统实例。</param>
        /// <returns>系统优先级。</returns>
        private static int GetPriority(IEntitySystem system)
            => system is ISystemOrder order ? order.Priority : 0;

        #endregion
    }
}
