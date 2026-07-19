using DGame;
using DGame.FixedPoint;

namespace GameBattle
{
    /// <summary>
    /// 战斗 ECS 单帧更新上下文。
    /// </summary>
    public readonly struct UpdateContext
    {
        #region Properties

        /// <summary>
        /// 当前更新所属世界。
        /// </summary>
        public readonly World World;

        /// <summary>
        /// 当前逻辑帧。
        /// </summary>
        public readonly int Frame;

        /// <summary>
        /// 本帧逻辑时间步长。
        /// </summary>
        public readonly FixedPoint64 DeltaTime;

        /// <summary>
        /// 当前世界累计逻辑时间。
        /// </summary>
        public readonly FixedPoint64 TotalTime;

        #endregion

        #region Create

        /// <summary>
        /// 创建单帧更新上下文。
        /// </summary>
        /// <param name="world">当前更新所属世界。</param>
        /// <param name="frame">当前逻辑帧。</param>
        /// <param name="deltaTime">本帧逻辑时间步长。</param>
        /// <param name="totalTime">当前世界累计逻辑时间。</param>
        public UpdateContext(World world, int frame, FixedPoint64 deltaTime, FixedPoint64 totalTime)
        {
            World = world;
            Frame = frame;
            DeltaTime = deltaTime;
            TotalTime = totalTime;
        }

        #endregion
    }
}
