using System;
using System.Collections.Generic;
using DGame;

namespace GameBattle
{
    /// <summary>
    /// 实体生命周期调度器，负责调用实体自带生命周期接口和外置生命周期系统。
    /// </summary>
    public sealed partial class EntitySystem
    {
        #region Fields

        /// <summary>
        /// 外置生命周期系统集合。
        /// </summary>
        private readonly EntitySystemCollection m_systems = new();

        /// <summary>
        /// 需要执行 Update 的实体队列。
        /// </summary>
        private readonly LinkedList<Entity> m_updateQueue = new();

        /// <summary>
        /// 需要执行 LateUpdate 的实体队列。
        /// </summary>
        private readonly LinkedList<Entity> m_lateUpdateQueue = new();

        /// <summary>
        /// Update 队列节点索引，用于 O(1) 移除。
        /// </summary>
        private readonly Dictionary<long, LinkedListNode<Entity>> m_updateNodes = new();

        /// <summary>
        /// LateUpdate 队列节点索引，用于 O(1) 移除。
        /// </summary>
        private readonly Dictionary<long, LinkedListNode<Entity>> m_lateUpdateNodes = new();

        #endregion

        #region Create

        /// <summary>
        /// 创建实体生命周期调度器，并注册 SourceGenerator 自动发现的外置系统。
        /// </summary>
        public EntitySystem()
        {
            RegisterGeneratedSystems();
        }

        /// <summary>
        /// 注册 SourceGenerator 自动生成的外置生命周期系统。
        /// </summary>
        partial void RegisterGeneratedSystems();

        #endregion

        #region Register

        /// <summary>
        /// 注册外置实体生命周期系统。
        /// </summary>
        /// <param name="system">外置实体系统。</param>
        public void RegisterSystem(IEntitySystem system) => m_systems.Register(system);

        #endregion

        #region Awake

        /// <summary>
        /// 唤醒实体并注册后续更新队列。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void Awake(Entity entity)
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            InvokeDirectAwake(entity);
            InvokeExternalAwake(entity);
            TryRegisterUpdate(entity);
        }

        /// <summary>
        /// 唤醒实体，传入一个 Awake 参数，并注册后续更新队列。
        /// </summary>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        public void Awake<T1>(Entity entity, T1 value1)
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            InvokeDirectAwake(entity, value1);
            InvokeExternalAwake(entity, value1);
            TryRegisterUpdate(entity);
        }

        /// <summary>
        /// 唤醒实体，传入两个 Awake 参数，并注册后续更新队列。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        public void Awake<T1, T2>(Entity entity, T1 value1, T2 value2)
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            InvokeDirectAwake(entity, value1, value2);
            InvokeExternalAwake(entity, value1, value2);
            TryRegisterUpdate(entity);
        }

        /// <summary>
        /// 唤醒实体，传入三个 Awake 参数，并注册后续更新队列。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        public void Awake<T1, T2, T3>(Entity entity, T1 value1, T2 value2, T3 value3)
        {
            if (entity == null || entity.IsDisposed)
            {
                return;
            }

            InvokeDirectAwake(entity, value1, value2, value3);
            InvokeExternalAwake(entity, value1, value2, value3);
            TryRegisterUpdate(entity);
        }

        #endregion

        #region Destroy

        /// <summary>
        /// 执行实体销毁生命周期，并移除更新队列。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void Destroy(Entity entity)
        {
            if (entity == null)
            {
                return;
            }

            UnregisterUpdate(entity);
            UnregisterLateUpdate(entity);

            try
            {
                m_systems.InvokeDestroy(entity);
                if (entity is IDestroy destroy)
                {
                    destroy.Destroy();
                }
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} Destroy error: {e}");
            }
        }

        #endregion

        #region Update

        /// <summary>
        /// 执行所有已注册实体的 Update 生命周期。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        public void Update(in UpdateContext context)
        {
            var node = m_updateQueue.First;
            var count = m_updateQueue.Count;

            while (count-- > 0 && node != null)
            {
                var next = node.Next;
                var entity = node.Value;

                if (entity == null || entity.IsDisposed)
                {
                    RemoveNode(m_updateQueue, m_updateNodes, node);
                    node = next;
                    continue;
                }

                try
                {
                    if (entity is IUpdate update)
                    {
                        update.Update(context);
                    }

                    m_systems.InvokeUpdate(entity, context);
                }
                catch (Exception e)
                {
                    BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} Update error: {e}");
                }

                node = next;
            }
        }

        #endregion

        #region LateUpdate

        /// <summary>
        /// 执行所有已注册实体的 LateUpdate 生命周期。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        public void LateUpdate(in UpdateContext context)
        {
            var node = m_lateUpdateQueue.First;
            var count = m_lateUpdateQueue.Count;

            while (count-- > 0 && node != null)
            {
                var next = node.Next;
                var entity = node.Value;

                if (entity == null || entity.IsDisposed)
                {
                    RemoveNode(m_lateUpdateQueue, m_lateUpdateNodes, node);
                    node = next;
                    continue;
                }

                try
                {
                    if (entity is ILateUpdate lateUpdate)
                    {
                        lateUpdate.LateUpdate(context);
                    }

                    m_systems.InvokeLateUpdate(entity, context);
                }
                catch (Exception e)
                {
                    BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} LateUpdate error: {e}");
                }

                node = next;
            }
        }

        #endregion

        #region Queue

        /// <summary>
        /// 根据实体直接接口或外置系统判断是否需要进入更新队列。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void TryRegisterUpdate(Entity entity)
        {
            var type = entity.GetType();
            if (entity is IUpdate || m_systems.HasUpdateSystem(type))
            {
                RegisterUpdate(entity);
            }

            if (entity is ILateUpdate || m_systems.HasLateUpdateSystem(type))
            {
                RegisterLateUpdate(entity);
            }
        }

        /// <summary>
        /// 注册实体到 Update 队列。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void RegisterUpdate(Entity entity)
        {
            if (m_updateNodes.ContainsKey(entity.RuntimeId))
            {
                return;
            }

            m_updateNodes.Add(entity.RuntimeId, m_updateQueue.AddLast(entity));
        }

        /// <summary>
        /// 注册实体到 LateUpdate 队列。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void RegisterLateUpdate(Entity entity)
        {
            if (m_lateUpdateNodes.ContainsKey(entity.RuntimeId))
            {
                return;
            }

            m_lateUpdateNodes.Add(entity.RuntimeId, m_lateUpdateQueue.AddLast(entity));
        }

        /// <summary>
        /// 从 Update 队列移除实体。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void UnregisterUpdate(Entity entity)
        {
            if (m_updateNodes.TryGetValue(entity.RuntimeId, out var node))
            {
                RemoveNode(m_updateQueue, m_updateNodes, node);
            }
        }

        /// <summary>
        /// 从 LateUpdate 队列移除实体。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void UnregisterLateUpdate(Entity entity)
        {
            if (m_lateUpdateNodes.TryGetValue(entity.RuntimeId, out var node))
            {
                RemoveNode(m_lateUpdateQueue, m_lateUpdateNodes, node);
            }
        }

        #endregion

        #region External Awake

        /// <summary>
        /// 调用无参外置 Awake 系统。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private void InvokeExternalAwake(Entity entity)
        {
            try
            {
                m_systems.InvokeAwake(entity);
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} AwakeSystem error: {e}");
            }
        }

        /// <summary>
        /// 调用带一个参数的外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        private void InvokeExternalAwake<T1>(Entity entity, T1 value1)
        {
            try
            {
                m_systems.InvokeAwake(entity, value1);
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} AwakeSystem error: {e}");
            }
        }

        /// <summary>
        /// 调用带两个参数的外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        private void InvokeExternalAwake<T1, T2>(Entity entity, T1 value1, T2 value2)
        {
            try
            {
                m_systems.InvokeAwake(entity, value1, value2);
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} AwakeSystem error: {e}");
            }
        }

        /// <summary>
        /// 调用带三个参数的外置 Awake 系统。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        private void InvokeExternalAwake<T1, T2, T3>(Entity entity, T1 value1, T2 value2, T3 value3)
        {
            try
            {
                m_systems.InvokeAwake(entity, value1, value2, value3);
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} AwakeSystem error: {e}");
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// 调用无参实体直接 Awake 生命周期。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        private static void InvokeDirectAwake(Entity entity)
        {
            try
            {
                if (entity is IAwake awake)
                {
                    awake.Awake();
                }
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} {nameof(IAwake.Awake)} error: {e}");
            }
        }

        /// <summary>
        /// 调用带一个参数的实体直接 Awake 生命周期。
        /// </summary>
        /// <typeparam name="T1">Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        private static void InvokeDirectAwake<T1>(Entity entity, T1 value1)
        {
            try
            {
                if (entity is IAwake<T1> awake)
                {
                    awake.Awake(value1);
                }
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} {nameof(IAwake.Awake)} error: {e}");
            }
        }

        /// <summary>
        /// 调用带两个参数的实体直接 Awake 生命周期。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        private static void InvokeDirectAwake<T1, T2>(Entity entity, T1 value1, T2 value2)
        {
            try
            {
                if (entity is IAwake<T1, T2> awake)
                {
                    awake.Awake(value1, value2);
                }
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} {nameof(IAwake.Awake)} error: {e}");
            }
        }

        /// <summary>
        /// 调用带三个参数的实体直接 Awake 生命周期。
        /// </summary>
        /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
        /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
        /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        private static void InvokeDirectAwake<T1, T2, T3>(Entity entity, T1 value1, T2 value2, T3 value3)
        {
            try
            {
                if (entity is IAwake<T1, T2, T3> awake)
                {
                    awake.Awake(value1, value2, value3);
                }
            }
            catch (Exception e)
            {
                BLogger.Error($"[GameBattle.EcsSystem] {entity.GetType().FullName} {nameof(IAwake.Awake)} error: {e}");
            }
        }

        /// <summary>
        /// 从链表队列和节点索引中移除指定节点。
        /// </summary>
        /// <param name="queue">实体更新队列。</param>
        /// <param name="nodes">节点索引表。</param>
        /// <param name="node">待移除节点。</param>
        private static void RemoveNode(
            LinkedList<Entity> queue,
            Dictionary<long, LinkedListNode<Entity>> nodes,
            LinkedListNode<Entity> node)
        {
            var entity = node.Value;
            if (entity != null)
            {
                nodes.Remove(entity.RuntimeId);
            }

            queue.Remove(node);
        }

        #endregion
    }
}
