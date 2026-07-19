using System;

namespace GameBattle
{
    #region Direct Lifecycle

    /// <summary>
    /// 实体直接 Update 生命周期。
    /// </summary>
    public interface IUpdate
    {
        /// <summary>
        /// 每帧逻辑更新。
        /// </summary>
        /// <param name="context">更新上下文。</param>
        void Update(in UpdateContext context);
    }

    #endregion

    #region External System

    /// <summary>
    /// 外置 Update 系统。
    /// </summary>
    public interface IUpdateSystem : IEntitySystem
    {
        /// <summary>
        /// 调用目标实体的外置 Update 逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        void Invoke(Entity entity, in UpdateContext context);
    }

    #endregion

    #region Base System

    /// <summary>
    /// 外置 Update 系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    public abstract class EcsUpdateSystem<T> : IUpdateSystem, ISystemOrder where T : Entity
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
        /// 调用强类型 Update 逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        public void Invoke(Entity entity, in UpdateContext context) => Update((T)entity, context);

        /// <summary>
        /// 执行强类型 Update 逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <param name="context">更新上下文。</param>
        protected abstract void Update(T self, in UpdateContext context);
    }

    #endregion
}
