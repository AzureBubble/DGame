using System;

namespace GameBattle
{
    #region Base System

    /// <summary>
    /// 外置实体系统基础接口。
    /// </summary>
    public interface IEntitySystem
    {
        /// <summary>
        /// 当前系统处理的实体类型。
        /// </summary>
        Type EntityType { get; }
    }

    #endregion

    #region Order

    /// <summary>
    /// 外置系统在同一生命周期内的执行阶段。
    /// </summary>
    public enum SystemPhase
    {
        /// <summary>
        /// 逻辑更新前的准备阶段。
        /// </summary>
        PreUpdate = 0,

        /// <summary>
        /// 默认逻辑阶段。
        /// </summary>
        Logic = 1000,

        /// <summary>
        /// 物理或碰撞阶段。
        /// </summary>
        Physics = 2000,

        /// <summary>
        /// 结算阶段。
        /// </summary>
        Resolve = 3000,

        /// <summary>
        /// 状态同步阶段。
        /// </summary>
        StateSync = 4000,

        /// <summary>
        /// 清理阶段。
        /// </summary>
        Cleanup = 5000,
    }

    /// <summary>
    /// 外置系统排序配置。Phase 先排序，Priority 用于同阶段内微调。
    /// </summary>
    public interface ISystemOrder
    {
        /// <summary>
        /// 系统执行阶段。
        /// </summary>
        SystemPhase Phase { get; }

        /// <summary>
        /// 同阶段内的执行优先级，数值越小越先执行。
        /// </summary>
        int Priority { get; }
    }

    #endregion
}
