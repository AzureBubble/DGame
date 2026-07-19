using System;

namespace GameBattle
{
    #region Direct Lifecycle

    /// <summary>
    /// 实体直接销毁生命周期。
    /// </summary>
    public interface IDestroy
    {
        /// <summary>
        /// 实体销毁时执行的清理逻辑。
        /// </summary>
        void Destroy();
    }

    #endregion

    #region External System

    /// <summary>
    /// 外置销毁系统。
    /// </summary>
    public interface IDestroySystem : IEntitySystem
    {
        /// <summary>
        /// 调用目标实体的外置销毁逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        void Invoke(Entity entity);
    }

    #endregion

    #region Base System

    /// <summary>
    /// 外置销毁系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    public abstract class EcsDestroySystem<T> : IDestroySystem where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 调用强类型销毁逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void Invoke(Entity entity) => Destroy((T)entity);

        /// <summary>
        /// 执行强类型销毁逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        protected abstract void Destroy(T self);
    }

    #endregion
}
