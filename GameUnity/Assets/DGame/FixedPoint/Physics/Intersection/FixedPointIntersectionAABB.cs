using System.Runtime.CompilerServices;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 表示几何体投影到指定轴后的一维闭区间。
    /// </summary>
    public struct FixedPointInterval
    {
        /// <summary>投影区间的最小值。</summary>
        public FixedPoint64 min;

        /// <summary>投影区间的最大值。</summary>
        public FixedPoint64 max;
    }

    /// <summary>
    /// 提供轴对齐包围盒相关的定点数相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FixedPoint64 Squared(FixedPoint64 value)
        {
            return value * value;
        }

        /// <summary>
        /// 判断轴对齐包围盒与球体是否重叠。
        /// </summary>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <param name="center">球心。</param>
        /// <param name="radius">非负球半径。</param>
        /// <returns>两者重叠或相切时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithAABBAndSphere(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 center,
            FixedPoint64 radius)
        {
            var remainingSquaredRadius = radius * radius;

            // 从半径平方中扣除球心到包围盒各轴区间的轴向距离平方。
            if (center.x < min.x)
            {
                remainingSquaredRadius -= Squared(center.x - min.x);
            }
            else if (center.x > max.x)
            {
                remainingSquaredRadius -= Squared(center.x - max.x);
            }

            if (center.y < min.y)
            {
                remainingSquaredRadius -= Squared(center.y - min.y);
            }
            else if (center.y > max.y)
            {
                remainingSquaredRadius -= Squared(center.y - max.y);
            }

            if (center.z < min.z)
            {
                remainingSquaredRadius -= Squared(center.z - min.z);
            }
            else if (center.z > max.z)
            {
                remainingSquaredRadius -= Squared(center.z - max.z);
            }

            return remainingSquaredRadius >= FixedPoint64.Zero;
        }

        /// <summary>
        /// 获取球心到轴对齐包围盒的最近点。
        /// </summary>
        /// <param name="point">球心或任意待查询点。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>包围盒内部或表面上距离输入点最近的点。</returns>
        public static FixedPointVector3 ClosestPointWithAABBAndSphere(
            FixedPointVector3 point,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            return ClosestPointWithPointAndAABB(point, min, max);
        }

        /// <summary>
        /// 判断包围盒 A 是否完全位于包围盒 B 内部。
        /// </summary>
        /// <param name="minA">包围盒 A 的逐分量最小点。</param>
        /// <param name="maxA">包围盒 A 的逐分量最大点。</param>
        /// <param name="minB">包围盒 B 的逐分量最小点。</param>
        /// <param name="maxB">包围盒 B 的逐分量最大点。</param>
        /// <returns>A 的全部边界均不超出 B 时返回 <see langword="true"/>。</returns>
        public static bool IsAABBInsideAABB(
            FixedPointVector3 minA,
            FixedPointVector3 maxA,
            FixedPointVector3 minB,
            FixedPointVector3 maxB)
        {
            return minA.x >= minB.x && minA.y >= minB.y && minA.z >= minB.z &&
                   maxA.x <= maxB.x && maxA.y <= maxB.y && maxA.z <= maxB.z;
        }

        /// <summary>
        /// 判断两个轴对齐包围盒是否重叠。
        /// </summary>
        /// <param name="minA">包围盒 A 的逐分量最小点。</param>
        /// <param name="maxA">包围盒 A 的逐分量最大点。</param>
        /// <param name="minB">包围盒 B 的逐分量最小点。</param>
        /// <param name="maxB">包围盒 B 的逐分量最大点。</param>
        /// <returns>三个坐标轴上的闭区间均有交集时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithAABBAndAABBFixedPoint(
            FixedPointVector3 minA,
            FixedPointVector3 maxA,
            FixedPointVector3 minB,
            FixedPointVector3 maxB)
        {
            return minA.x <= maxB.x && maxA.x >= minB.x &&
                   minA.y <= maxB.y && maxA.y >= minB.y &&
                   minA.z <= maxB.z && maxA.z >= minB.z;
        }

        /// <summary>
        /// 使用分离轴定理判断轴对齐包围盒与有向包围盒是否重叠。
        /// </summary>
        /// <param name="min">轴对齐包围盒的逐分量最小点。</param>
        /// <param name="max">轴对齐包围盒的逐分量最大点。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="fixedPointMatrix">包含三个单位局部轴的旋转矩阵。</param>
        /// <returns>15 条候选分离轴上均发生投影重叠时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithAABBAndOBBFixedPoint(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix)
        {
            var axes = new FixedPointVector3[15];
            axes[0] = FixedPointVector3.right;
            axes[1] = FixedPointVector3.up;
            axes[2] = FixedPointVector3.forward;
            axes[3] = new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13);
            axes[4] = new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23);
            axes[5] = new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33);

            // 另外九条候选轴来自两个盒体各自三条边轴的两两叉乘。
            for (var worldAxisIndex = 0; worldAxisIndex < 3; worldAxisIndex++)
            {
                for (var boxAxisIndex = 0; boxAxisIndex < 3; boxAxisIndex++)
                {
                    axes[6 + worldAxisIndex * 3 + boxAxisIndex] =
                        FixedPointVector3.Cross(axes[worldAxisIndex], axes[3 + boxAxisIndex]);
                }
            }

            for (var i = 0; i < axes.Length; i++)
            {
                if (!OverlapOnAxis(min, max, position, halfSize, fixedPointMatrix, axes[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 判断轴对齐包围盒与 OBB 碰撞器是否重叠。
        /// </summary>
        /// <param name="min">轴对齐包围盒的逐分量最小点。</param>
        /// <param name="max">轴对齐包围盒的逐分量最大点。</param>
        /// <param name="obb">待检测的 OBB 碰撞器。</param>
        /// <returns>两者重叠或接触时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithAABBAndOBBFixedPoint(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FPBoxCollider obb)
        {
            return IntersectWithAABBAndOBBFixedPoint(
                min,
                max,
                obb.position,
                obb.halfSize,
                obb.fpTransform.fixedPointMatrix);
        }

        /// <summary>
        /// 计算轴对齐包围盒在指定轴上的投影区间。
        /// </summary>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <param name="axis">投影轴，无须归一化。</param>
        /// <returns>八个顶点投影形成的闭区间。</returns>
        public static FixedPointInterval GetInterval(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 axis)
        {
            var vertices = new[]
            {
                new FixedPointVector3(min.x, max.y, max.z),
                new FixedPointVector3(min.x, max.y, min.z),
                new FixedPointVector3(min.x, min.y, max.z),
                new FixedPointVector3(min.x, min.y, min.z),
                new FixedPointVector3(max.x, max.y, max.z),
                new FixedPointVector3(max.x, max.y, min.z),
                new FixedPointVector3(max.x, min.y, max.z),
                new FixedPointVector3(max.x, min.y, min.z)
            };

            var result = new FixedPointInterval
            {
                min = FixedPointVector3.Dot(axis, vertices[0]),
                max = FixedPointVector3.Dot(axis, vertices[0])
            };

            for (var i = 1; i < vertices.Length; i++)
            {
                var projection = FixedPointVector3.Dot(axis, vertices[i]);
                result.min = FixedPointMath.Min(result.min, projection);
                result.max = FixedPointMath.Max(result.max, projection);
            }

            return result;
        }

        /// <summary>
        /// 计算有向包围盒在指定轴上的投影区间。
        /// </summary>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="fixedPointMatrix">包含三个单位局部轴的旋转矩阵。</param>
        /// <param name="axis">投影轴，无须归一化。</param>
        /// <returns>八个顶点投影形成的闭区间。</returns>
        public static FixedPointInterval GetInterval(
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix,
            FixedPointVector3 axis)
        {
            var axisX = new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13);
            var axisY = new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23);
            var axisZ = new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33);
            var vertices = new FixedPointVector3[8];

            vertices[0] = position + axisX * halfSize.x + axisY * halfSize.y + axisZ * halfSize.z;
            vertices[1] = position - axisX * halfSize.x + axisY * halfSize.y + axisZ * halfSize.z;
            vertices[2] = position + axisX * halfSize.x - axisY * halfSize.y + axisZ * halfSize.z;
            vertices[3] = position + axisX * halfSize.x + axisY * halfSize.y - axisZ * halfSize.z;
            vertices[4] = position - axisX * halfSize.x - axisY * halfSize.y - axisZ * halfSize.z;
            vertices[5] = position + axisX * halfSize.x - axisY * halfSize.y - axisZ * halfSize.z;
            vertices[6] = position - axisX * halfSize.x + axisY * halfSize.y - axisZ * halfSize.z;
            vertices[7] = position - axisX * halfSize.x - axisY * halfSize.y + axisZ * halfSize.z;

            var projection = FixedPointVector3.Dot(axis, vertices[0]);
            var result = new FixedPointInterval { min = projection, max = projection };

            for (var i = 1; i < vertices.Length; i++)
            {
                projection = FixedPointVector3.Dot(axis, vertices[i]);
                result.min = FixedPointMath.Min(result.min, projection);
                result.max = FixedPointMath.Max(result.max, projection);
            }

            return result;
        }

        /// <summary>
        /// 判断两个盒体在指定候选轴上的投影是否重叠。
        /// </summary>
        private static bool OverlapOnAxis(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix,
            FixedPointVector3 axis)
        {
            // 平行边叉乘会得到零向量，零轴不能构成有效分离轴，应直接跳过。
            if (axis.IsZero())
            {
                return true;
            }

            var a = GetInterval(min, max, axis);
            var b = GetInterval(position, halfSize, fixedPointMatrix, axis);
            return b.min <= a.max && a.min <= b.max;
        }
    }
}