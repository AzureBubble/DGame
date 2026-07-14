/*
 * 创建日期：2022/11/1
 * 作者：応彧剛（yingyugang@gmail.com）
 * 用途：定义定点数平面。
 */
using System;
using System.Runtime.CompilerServices;

#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用单位法线和到原点的有符号距离描述的定点数平面。
    /// </summary>
    public class FixedPointPlane : FixedPointShape
    {
        /// <summary>平面的单位法线。</summary>
        public FixedPointVector3 normal;

        /// <summary>平面沿法线方向到原点的有符号距离。</summary>
        public FixedPoint64 distance;

        /// <summary>
        /// 使用指定法线和有符号距离创建平面。
        /// </summary>
        /// <param name="normal">非零平面法线；构造时会自动归一化。</param>
        /// <param name="distance">平面沿归一化法线方向到原点的有符号距离。</param>
        /// <exception cref="ArgumentException">平面法线为零向量。</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedPointPlane(FixedPointVector3 normal, FixedPoint64 distance)
        {
            if (normal.IsZero())
            {
                throw new ArgumentException("平面法线不能为零向量。", nameof(normal));
            }

            this.normal = FixedPointVector3.Normalize(normal);
            this.distance = distance;
            shape = ShapeType.Plane;
        }

        /// <summary>
        /// 创建经过原点且法线朝上的水平平面。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedPointPlane()
        {
            normal = FixedPointVector3.up;
            distance = FixedPoint64.Zero;
            shape = ShapeType.Plane;
        }

#if UNITY_2021_3_OR_NEWER
        /// <inheritdoc />
        public override void DrawGizmos(bool intersected)
        {
            var unityNormal = normal.ToVector3();

            if (unityNormal.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var previousMatrix = Gizmos.matrix;
            Gizmos.color = intersected ? Color.red : Color.white;
            Gizmos.matrix = Matrix4x4.TRS(
                (normal * distance).ToVector3(),
                Quaternion.FromToRotation(Vector3.up, unityNormal.normalized),
                Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(10f, 0f, 10f));
            Gizmos.matrix = previousMatrix;
        }
#endif
    }
}