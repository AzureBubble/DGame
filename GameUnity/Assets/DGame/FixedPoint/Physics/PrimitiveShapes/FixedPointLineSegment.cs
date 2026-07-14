/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数线段。
 */
// 参考资料：《Game Physics Cookbook》
#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 由起点和终点描述的定点数线段。
    /// </summary>
    public class FixedPointLineSegment : FixedPointShape
    {
        /// <summary>
        /// 获取线段起点。
        /// </summary>
        public FixedPointVector3 Start
        {
            get { return start; }
        }

        /// <summary>
        /// 获取线段终点。
        /// </summary>
        public FixedPointVector3 End
        {
            get { return end; }
        }

        /// <summary>线段起点。</summary>
        protected FixedPointVector3 start;

        /// <summary>线段终点。</summary>
        protected FixedPointVector3 end;

        /// <summary>
        /// 创建起点和终点均位于原点的退化线段。
        /// </summary>
        public FixedPointLineSegment()
        {
            shape = ShapeType.Line;
        }

        /// <summary>
        /// 使用指定起点和终点创建线段。
        /// </summary>
        /// <param name="start">线段起点。</param>
        /// <param name="end">线段终点。</param>
        public FixedPointLineSegment(FixedPointVector3 start, FixedPointVector3 end)
        {
            this.start = start;
            this.end = end;
            shape = ShapeType.Line;
        }

#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.DrawLine(start.ToVector3(), end.ToVector3());
        }
#endif
    }
}