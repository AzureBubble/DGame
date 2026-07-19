using System;

namespace GameBattle
{
    #region Direct Lifecycle

    /// <summary>
    /// 实体直接唤醒生命周期。
    /// </summary>
    public interface IAwake
    {
        /// <summary>
        /// 实体创建后执行的无参唤醒逻辑。
        /// </summary>
        void Awake();
    }

    /// <summary>
    /// 带一个参数的实体直接唤醒生命周期。
    /// </summary>
    /// <typeparam name="T1">Awake 参数类型。</typeparam>
    public interface IAwake<in T1>
    {
        /// <summary>
        /// 实体创建后执行的唤醒逻辑。
        /// </summary>
        /// <param name="value1">Awake 参数。</param>
        void Awake(T1 value1);
    }

    /// <summary>
    /// 带两个参数的实体直接唤醒生命周期。
    /// </summary>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    public interface IAwake<in T1, in T2>
    {
        /// <summary>
        /// 实体创建后执行的唤醒逻辑。
        /// </summary>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        void Awake(T1 value1, T2 value2);
    }

    /// <summary>
    /// 带三个参数的实体直接唤醒生命周期。
    /// </summary>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
    public interface IAwake<in T1, in T2, in T3>
    {
        /// <summary>
        /// 实体创建后执行的唤醒逻辑。
        /// </summary>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        void Awake(T1 value1, T2 value2, T3 value3);
    }

    #endregion

    #region External System

    /// <summary>
    /// 外置唤醒系统基础接口。
    /// </summary>
    public interface IAwakeSystemBase : IEntitySystem
    {
    }

    /// <summary>
    /// 无参外置唤醒系统。
    /// </summary>
    public interface IAwakeSystem : IAwakeSystemBase
    {
        /// <summary>
        /// 调用目标实体的外置唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        void Invoke(Entity entity);
    }

    /// <summary>
    /// 带一个参数的外置唤醒系统。
    /// </summary>
    /// <typeparam name="T1">Awake 参数类型。</typeparam>
    public interface IAwakeSystem<in T1> : IAwakeSystemBase
    {
        /// <summary>
        /// 调用目标实体的外置唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        void Invoke(Entity entity, T1 value1);
    }

    /// <summary>
    /// 带两个参数的外置唤醒系统。
    /// </summary>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    public interface IAwakeSystem<in T1, in T2> : IAwakeSystemBase
    {
        /// <summary>
        /// 调用目标实体的外置唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        void Invoke(Entity entity, T1 value1, T2 value2);
    }

    /// <summary>
    /// 带三个参数的外置唤醒系统。
    /// </summary>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
    public interface IAwakeSystem<in T1, in T2, in T3> : IAwakeSystemBase
    {
        /// <summary>
        /// 调用目标实体的外置唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        void Invoke(Entity entity, T1 value1, T2 value2, T3 value3);
    }

    #endregion

    #region Base System

    /// <summary>
    /// 无参外置唤醒系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    public abstract class EcsAwakeSystem<T> : IAwakeSystem where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 调用强类型唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        public void Invoke(Entity entity) => Awake((T)entity);

        /// <summary>
        /// 执行强类型唤醒逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        protected abstract void Awake(T self);
    }

    /// <summary>
    /// 带一个参数的外置唤醒系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    /// <typeparam name="T1">Awake 参数类型。</typeparam>
    public abstract class EcsAwakeSystem<T, T1> : IAwakeSystem<T1> where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 调用强类型唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        public void Invoke(Entity entity, T1 value1) => Awake((T)entity, value1);

        /// <summary>
        /// 执行强类型唤醒逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <param name="value1">Awake 参数。</param>
        protected abstract void Awake(T self, T1 value1);
    }

    /// <summary>
    /// 带两个参数的外置唤醒系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    public abstract class EcsAwakeSystem<T, T1, T2> : IAwakeSystem<T1, T2> where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 调用强类型唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        public void Invoke(Entity entity, T1 value1, T2 value2) => Awake((T)entity, value1, value2);

        /// <summary>
        /// 执行强类型唤醒逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        protected abstract void Awake(T self, T1 value1, T2 value2);
    }

    /// <summary>
    /// 带三个参数的外置唤醒系统基类。
    /// </summary>
    /// <typeparam name="T">目标实体类型。</typeparam>
    /// <typeparam name="T1">第一个 Awake 参数类型。</typeparam>
    /// <typeparam name="T2">第二个 Awake 参数类型。</typeparam>
    /// <typeparam name="T3">第三个 Awake 参数类型。</typeparam>
    public abstract class EcsAwakeSystem<T, T1, T2, T3> : IAwakeSystem<T1, T2, T3> where T : Entity
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        public Type EntityType => typeof(T);

        /// <summary>
        /// 调用强类型唤醒逻辑。
        /// </summary>
        /// <param name="entity">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        public void Invoke(Entity entity, T1 value1, T2 value2, T3 value3) => Awake((T)entity, value1, value2, value3);

        /// <summary>
        /// 执行强类型唤醒逻辑。
        /// </summary>
        /// <param name="self">目标实体。</param>
        /// <param name="value1">第一个 Awake 参数。</param>
        /// <param name="value2">第二个 Awake 参数。</param>
        /// <param name="value3">第三个 Awake 参数。</param>
        protected abstract void Awake(T self, T1 value1, T2 value2, T3 value3);
    }

    #endregion
}
