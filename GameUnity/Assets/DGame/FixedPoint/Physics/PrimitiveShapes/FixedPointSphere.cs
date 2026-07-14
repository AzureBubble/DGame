/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数球体。
 */
using System;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 由球心和半径描述的定点数球体。
    /// </summary>
    public class FixedPointSphere : FixedPointShape
    {
        /// <summary>
        /// 获取球心。
        /// </summary>
        public FixedPointVector3 Point
        {
            get { return point; }
        }

        /// <summary>
        /// 获取球体半径。
        /// </summary>
        public FixedPoint64 Radius
        {
            get { return radius; }
        }

        /// <summary>球体半径。</summary>
        protected FixedPoint64 radius;

        /// <summary>球心。</summary>
        protected FixedPointVector3 point;

        /// <summary>
        /// 创建球心位于原点且半径为零的球体。
        /// </summary>
        public FixedPointSphere()
        {
            shape = ShapeType.Sphere;
        }

        /// <summary>
        /// 使用指定球心和半径创建球体。
        /// </summary>
        /// <param name="point">球心。</param>
        /// <param name="radius">非负半径。</param>
        /// <exception cref="ArgumentOutOfRangeException">半径小于零。</exception>
        public FixedPointSphere(FixedPointVector3 point, FixedPoint64 radius)
        {
            if (radius < FixedPoint64.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "球体半径不能小于零。");
            }

            this.point = point;
            this.radius = radius;
            shape = ShapeType.Sphere;
        }

#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.DrawSphere(Point.ToVector3(), radius.AsFloat());
        }
#endif
    }
}