namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点数学进行物理模拟的游戏对象抽象基类，确保跨平台确定性。
    /// 纯逻辑部分双端共用；Unity 表现/注册部分见 FPGameObject.Unity.cs。
    /// </summary>
    public abstract partial class FPGameObject : FastListItem
    {
        /// <summary>
        /// 获取与当前游戏对象关联的定点变换组件。
        /// </summary>
        public FPTransform fpTransform { get; private set; }

        /// <summary>
        /// 更新当前游戏对象的视图或渲染表现。
        /// </summary>
        public abstract void OnViewUpdate();

        /// <summary>
        /// 更新当前游戏对象的物理逻辑，通常在每个物理步执行。
        /// </summary>
        public abstract void OnLogicUpdate();

        /// <summary>
        /// 获取或设置当前游戏对象在 <see cref="FastList{T}"/> 中的索引。
        /// </summary>
        public int index { get; set; } = -1;

        /// <summary>
        /// 当前游戏对象持有的定点计时器集合。
        /// </summary>
        internal readonly FastList<FPTimer> fixedPointTimers = new ();
    }
}
