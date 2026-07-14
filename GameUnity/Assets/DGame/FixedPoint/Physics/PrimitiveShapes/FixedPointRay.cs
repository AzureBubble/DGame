/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数射线及相交计算所需的预计算数据。
 */
// 参考资料：https://github.com/mattatz/unity-intersections/tree/master/Assets/Intersections/Scripts
// 参考资料：《Game Physics Cookbook》
using System;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 由起点和单位方向描述的定点数射线。
    /// </summary>
    public class FixedPointRay : FixedPointShape
    {
#if UNITY_2021_3_OR_NEWER
        private const float GizmoLength = 1000f;
#endif

        /// <summary>
        /// 获取射线起点。
        /// </summary>
        public FixedPointVector3 Point
        {
            get { return point; }
        }

        /// <summary>
        /// 获取归一化后的射线方向。
        /// </summary>
        public FixedPointVector3 Dir
        {
            get { return dir; }
        }

        /// <summary>
        /// 获取射线方向各分量的倒数，用于包围盒相交计算。
        /// </summary>
        /// <remarks>方向分量为零时，对应倒数采用 <see cref="FixedPoint64.MAX_VALUE"/> 表示正无穷方向。</remarks>
        public FixedPointVector3 InvDir
        {
            get { return invDir; }
        }

        /// <summary>
        /// 获取方向倒数各分量的符号标记；负数为 1，非负数为 0。
        /// </summary>
        public FixedPointVector3 Sign
        {
            get { return sign; }
        }

        /// <summary>射线起点。</summary>
        protected FixedPointVector3 point;

        /// <summary>归一化后的射线方向。</summary>
        protected FixedPointVector3 dir;

        /// <summary>方向各分量的倒数。</summary>
        protected FixedPointVector3 invDir;

        /// <summary>方向倒数各分量的符号标记。</summary>
        protected FixedPointVector3 sign;

        /// <summary>
        /// 创建从原点沿前方延伸的射线。
        /// </summary>
        public FixedPointRay()
        {
            point = FixedPointVector3.zero;
            dir = FixedPointVector3.forward;
            UpdateDerivedData();
            shape = ShapeType.Ray;
        }

        /// <summary>
        /// 使用指定起点和方向创建射线。
        /// </summary>
        /// <param name="point">射线起点。</param>
        /// <param name="dir">非零射线方向；构造时会自动归一化。</param>
        /// <exception cref="ArgumentException">射线方向为零向量。</exception>
        public FixedPointRay(FixedPointVector3 point, FixedPointVector3 dir) : base()
        {
            if (dir.IsZero())
            {
                throw new ArgumentException("射线方向不能为零向量。", nameof(dir));
            }

            this.point = point;
            this.dir = dir.normalized;
            UpdateDerivedData();
            shape = ShapeType.Ray;
        }

        /// <summary>
        /// 根据当前单位方向更新包围盒相交算法使用的预计算数据。
        /// </summary>
        private void UpdateDerivedData()
        {
            invDir = new FixedPointVector3(
                1 / this.dir.x,
                1 / this.dir.y,
                1 / this.dir.z
            );
            sign = new FixedPointVector3(
                invDir.x < 0 ? 1 : 0,
                invDir.y < 0 ? 1 : 0,
                invDir.z < 0 ? 1 : 0
            );
        }

#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.DrawSphere(Point.ToVector3(), 0.1f);
            Gizmos.DrawRay(Point.ToVector3(), Dir.ToVector3() * GizmoLength);
        }
#endif
    }
}