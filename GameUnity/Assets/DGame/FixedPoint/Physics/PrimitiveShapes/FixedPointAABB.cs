/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数轴对齐包围盒。
 */
#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用最小点和最大点描述的定点数轴对齐包围盒。
    /// </summary>
    public class FixedPointAABB : FixedPointShape
    {
        /// <summary>
        /// 获取包围盒在各坐标轴上的最小点。
        /// </summary>
        public FixedPointVector3 Min
        {
            get { return min; }
        }

        /// <summary>
        /// 获取包围盒在各坐标轴上的最大点。
        /// </summary>
        public FixedPointVector3 Max
        {
            get { return max; }
        }

        /// <summary>
        /// 获取包围盒中心点。
        /// </summary>
        public FixedPointVector3 Center
        {
            get { return (min + max) * 0.5f; }
        }

        /// <summary>包围盒最小点。</summary>
        protected FixedPointVector3 min;

        /// <summary>包围盒最大点。</summary>
        protected FixedPointVector3 max;

        /// <summary>
        /// 创建位于原点且尺寸为零的轴对齐包围盒。
        /// </summary>
        public FixedPointAABB()
        {
            shape = ShapeType.AABB;
        }

        /// <summary>
        /// 使用两个对角点创建轴对齐包围盒。
        /// </summary>
        /// <param name="min">第一个对角点。</param>
        /// <param name="max">第二个对角点。</param>
        /// <remarks>构造函数会逐轴重新排序两个输入点，确保 <see cref="Min"/> 不大于 <see cref="Max"/>。</remarks>
        public FixedPointAABB(FixedPointVector3 min, FixedPointVector3 max) : base()
        {
            this.min = FixedPointVector3.Min(min, max);
            this.max = FixedPointVector3.Max(min, max);
            shape = ShapeType.AABB;
        }
#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            Gizmos.color = intersected ? Color.red : Color.white;
            var center = (Min + Max) * 0.5f;
            Gizmos.DrawWireCube(center.ToVector3(), (Max - Min).ToVector3());
        }

        /// <summary>
        /// 返回便于在 Unity 中调试的最小点和最大点文本。
        /// </summary>
        /// <returns>包含最小点和最大点的文本。</returns>
        public override string ToString()
        {
            return $"最小点：{Min.ToVector3()}，最大点：{Max.ToVector3()}";
        }
#endif
    }
}