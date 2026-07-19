using System;

namespace GameBattle
{
    #region Direct Lifecycle

    /// <summary>
    /// 实体直接 LateUpdate 生命周期。
    /// </summary>
    public interface ILateUpdate
    {
        /// <summary>
        /// 每帧逻辑后更新。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        void LateUpdate(in UpdateContext context);
    }

    #endregion

    #region External System

    /// <summary>
    /// 外置 LateUpdate 系统。
    /// </summary>
    public interface ILateUpdateSystem : IEntitySystem
    {
        /// <summary>
        /// 调用目标实体的外置 LateUpdate 逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        void Invoke(Entity entity, in UpdateContext context);
    }

    #endregion

    #region Base System

    /// <summary>
    /// 外置 LateUpdate 系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    public abstract class EcsLateUpdateSystem<T> : ILateUpdateSystem, ISystemOrder where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 系统执行阶段。
        /// </summary>
        public virtual SystemPhase Phase => SystemPhase.Logic;

        /// <summary>
        /// 同阶段内的执行优先级，数值越小越先执行。
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// 调用强类型 LateUpdate 逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        public void Invoke(Entity entity, in UpdateContext context) => LateUpdate((T)entity, context);

        /// <summary>
        /// 执行强类型 LateUpdate 逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        protected abstract void LateUpdate(T self, in UpdateContext context);
    }

    #endregion
}
