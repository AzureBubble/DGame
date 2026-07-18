using System;
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

        /// <summary>检测两个轴对齐包围盒并返回完整接触信息。</summary>
        /// <returns>从包围盒 B 指向包围盒 A 的最小分离法线、表面点和半穿透深度。</returns>
        public static FPCollision IntersectWithAABBAndAABBCollision(
            FixedPointVector3 minA,
            FixedPointVector3 maxA,
            FixedPointVector3 minB,
            FixedPointVector3 maxB)
        {
            var collision = new FPCollision();
            var overlapX = FixedPointMath.Min(maxA.x, maxB.x) - FixedPointMath.Max(minA.x, minB.x);
            var overlapY = FixedPointMath.Min(maxA.y, maxB.y) - FixedPointMath.Max(minA.y, minB.y);
            var overlapZ = FixedPointMath.Min(maxA.z, maxB.z) - FixedPointMath.Max(minA.z, minB.z);

            if (overlapX < FixedPoint64.Zero || overlapY < FixedPoint64.Zero ||
                overlapZ < FixedPoint64.Zero)
            {
                return collision;
            }

            var centerA = (minA + maxA) * FixedPoint64.Half;
            var centerB = (minB + maxB) * FixedPoint64.Half;
            var penetration = overlapX;
            var normal = centerA.x >= centerB.x ? FixedPointVector3.right : FixedPointVector3.left;

            if (overlapY < penetration)
            {
                penetration = overlapY;
                normal = centerA.y >= centerB.y ? FixedPointVector3.up : FixedPointVector3.down;
            }

            if (overlapZ < penetration)
            {
                penetration = overlapZ;
                normal = centerA.z >= centerB.z ? FixedPointVector3.forward : FixedPointVector3.back;
            }

            var pointOnB = SupportAabb(minB, maxB, normal);
            var pointOnA = SupportAabb(minA, maxA, -normal);
            FillBoxCollision(ref collision, normal, penetration, pointOnB, pointOnA);
            return collision;
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
            Span<FixedPointVector3> axes = stackalloc FixedPointVector3[15];
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

        /// <summary>检测轴对齐包围盒与有向包围盒并返回完整接触信息。</summary>
        /// <returns>从 OBB 指向 AABB 的最小分离法线、表面点和半穿透深度。</returns>
        public static FPCollision IntersectWithAABBAndOBBCollision(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix)
        {
            var collision = new FPCollision();
            Span<FixedPointVector3> axes = stackalloc FixedPointVector3[15];
            axes[0] = FixedPointVector3.right;
            axes[1] = FixedPointVector3.up;
            axes[2] = FixedPointVector3.forward;
            axes[3] = new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13);
            axes[4] = new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23);
            axes[5] = new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33);

            for (var worldAxisIndex = 0; worldAxisIndex < 3; worldAxisIndex++)
            {
                for (var boxAxisIndex = 0; boxAxisIndex < 3; boxAxisIndex++)
                {
                    axes[6 + worldAxisIndex * 3 + boxAxisIndex] =
                        FixedPointVector3.Cross(axes[worldAxisIndex], axes[3 + boxAxisIndex]);
                }
            }

            var centerA = (min + max) * FixedPoint64.Half;
            var centerDelta = centerA - position;
            var minimumPenetration = FixedPoint64.MaxValue;
            var minimumAxis = FixedPointVector3.zero;

            foreach (var candidateAxis in axes)
            {
                if (candidateAxis.IsZero()) continue;
                var axis = candidateAxis.normalized;
                var intervalA = GetInterval(min, max, axis);
                var intervalB = GetInterval(position, halfSize, fixedPointMatrix, axis);
                var penetration = FixedPointMath.Min(intervalA.max, intervalB.max) -
                                  FixedPointMath.Max(intervalA.min, intervalB.min);

                if (penetration < FixedPoint64.Zero)
                {
                    return collision;
                }

                if (penetration < minimumPenetration)
                {
                    minimumPenetration = penetration;
                    minimumAxis = FixedPointVector3.Dot(centerDelta, axis) >= FixedPoint64.Zero
                        ? axis
                        : -axis;
                }
            }

            if (minimumAxis.IsZero())
            {
                minimumAxis = FixedPointVector3.right;
                minimumPenetration = FixedPoint64.Zero;
            }

            var pointOnObb = SupportObb(position, halfSize, fixedPointMatrix, minimumAxis);
            var pointOnAabb = SupportAabb(min, max, -minimumAxis);
            FillBoxCollision(
                ref collision, minimumAxis, minimumPenetration, pointOnObb, pointOnAabb);
            return collision;
        }

        /// <summary>检测有向包围盒与三角形的重叠并返回接触信息。</summary>
        public static FPCollision IntersectWithOBBAndTriangle(
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            var localA = ToBoxLocal(a);
            var localB = ToBoxLocal(b);
            var localC = ToBoxLocal(c);
            var edgeA = localB - localA;
            var edgeB = localC - localB;
            var edgeC = localA - localC;
            Span<FixedPointVector3> axes = stackalloc FixedPointVector3[13]
            {
                FixedPointVector3.right,
                FixedPointVector3.up,
                FixedPointVector3.forward,
                FixedPointVector3.Cross(edgeA, edgeB),
                FixedPointVector3.Cross(edgeA, FixedPointVector3.right),
                FixedPointVector3.Cross(edgeA, FixedPointVector3.up),
                FixedPointVector3.Cross(edgeA, FixedPointVector3.forward),
                FixedPointVector3.Cross(edgeB, FixedPointVector3.right),
                FixedPointVector3.Cross(edgeB, FixedPointVector3.up),
                FixedPointVector3.Cross(edgeB, FixedPointVector3.forward),
                FixedPointVector3.Cross(edgeC, FixedPointVector3.right),
                FixedPointVector3.Cross(edgeC, FixedPointVector3.up),
                FixedPointVector3.Cross(edgeC, FixedPointVector3.forward)
            };
            var minimumPenetration = FixedPoint64.MaxValue;
            var minimumLocalAxis = FixedPointVector3.zero;

            foreach (var candidate in axes)
            {
                if (candidate.IsZero()) continue;
                var axis = candidate.normalized;
                var projectionA = FixedPointVector3.Dot(localA, axis);
                var projectionB = FixedPointVector3.Dot(localB, axis);
                var projectionC = FixedPointVector3.Dot(localC, axis);
                var triangleMin = FixedPointMath.Min(projectionA,
                    FixedPointMath.Min(projectionB, projectionC));
                var triangleMax = FixedPointMath.Max(projectionA,
                    FixedPointMath.Max(projectionB, projectionC));
                var boxRadius = halfSize.x * FixedPointMath.Abs(axis.x) +
                                halfSize.y * FixedPointMath.Abs(axis.y) +
                                halfSize.z * FixedPointMath.Abs(axis.z);
                var penetration = FixedPointMath.Min(boxRadius, triangleMax) -
                                  FixedPointMath.Max(-boxRadius, triangleMin);
                if (penetration < FixedPoint64.Zero) return default;
                if (penetration < minimumPenetration)
                {
                    minimumPenetration = penetration;
                    var triangleCenter = (localA + localB + localC) / 3;
                    minimumLocalAxis = FixedPointVector3.Dot(-triangleCenter, axis) >= 0 ? axis : -axis;
                }
            }

            var normal = axisX * minimumLocalAxis.x +
                         axisY * minimumLocalAxis.y +
                         axisZ * minimumLocalAxis.z;
            normal = normal.IsZero() ? FixedPointVector3.up : normal.normalized;
            var triangleMinWorld = FixedPointVector3.Min(a, FixedPointVector3.Min(b, c));
            var triangleMaxWorld = FixedPointVector3.Max(a, FixedPointVector3.Max(b, c));
            var pointOnTriangle = ClosestPointWithPointAndTriangle(
                position, FixedPointVector3.zero, triangleMinWorld, triangleMaxWorld, a, b, c);
            var pointOnBox = SupportObb(position, halfSize, orientation, -normal);
            var collision = new FPCollision();
            FillBoxCollision(ref collision, normal, minimumPenetration, pointOnTriangle, pointOnBox);
            return collision;

            FixedPointVector3 ToBoxLocal(FixedPointVector3 point)
            {
                var offset = point - position;
                return new FixedPointVector3(
                    FixedPointVector3.Dot(offset, axisX),
                    FixedPointVector3.Dot(offset, axisY),
                    FixedPointVector3.Dot(offset, axisZ));
            }
        }

        /// <summary>检测两个有向包围盒并返回完整接触信息。</summary>
        public static FPCollision IntersectWithOBBAndOBBCollision(
            FixedPointVector3 positionA,
            FixedPointVector3 halfSizeA,
            FixedPointMatrix orientationA,
            FixedPointVector3 positionB,
            FixedPointVector3 halfSizeB,
            FixedPointMatrix orientationB)
        {
            Span<FixedPointVector3> axesA = stackalloc FixedPointVector3[3]
            {
                new FixedPointVector3(orientationA.M11, orientationA.M12, orientationA.M13),
                new FixedPointVector3(orientationA.M21, orientationA.M22, orientationA.M23),
                new FixedPointVector3(orientationA.M31, orientationA.M32, orientationA.M33)
            };
            Span<FixedPointVector3> axesB = stackalloc FixedPointVector3[3]
            {
                new FixedPointVector3(orientationB.M11, orientationB.M12, orientationB.M13),
                new FixedPointVector3(orientationB.M21, orientationB.M22, orientationB.M23),
                new FixedPointVector3(orientationB.M31, orientationB.M32, orientationB.M33)
            };
            Span<FixedPointVector3> axes = stackalloc FixedPointVector3[15];
            for (var i = 0; i < 3; i++)
            {
                axes[i] = axesA[i];
                axes[3 + i] = axesB[i];
            }

            for (var a = 0; a < 3; a++)
            {
                for (var b = 0; b < 3; b++)
                {
                    axes[6 + a * 3 + b] = FixedPointVector3.Cross(axesA[a], axesB[b]);
                }
            }

            var minimumPenetration = FixedPoint64.MaxValue;
            var minimumAxis = FixedPointVector3.zero;
            var centerDelta = positionA - positionB;
            foreach (var candidate in axes)
            {
                if (candidate.IsZero()) continue;
                var axis = candidate.normalized;
                var intervalA = GetInterval(positionA, halfSizeA, orientationA, axis);
                var intervalB = GetInterval(positionB, halfSizeB, orientationB, axis);
                var penetration = FixedPointMath.Min(intervalA.max, intervalB.max) -
                                  FixedPointMath.Max(intervalA.min, intervalB.min);
                if (penetration < FixedPoint64.Zero) return default;
                if (penetration < minimumPenetration)
                {
                    minimumPenetration = penetration;
                    minimumAxis = FixedPointVector3.Dot(centerDelta, axis) >= 0 ? axis : -axis;
                }
            }

            if (minimumAxis.IsZero()) minimumAxis = FixedPointVector3.right;
            var pointOnB = SupportObb(positionB, halfSizeB, orientationB, minimumAxis);
            var pointOnA = SupportObb(positionA, halfSizeA, orientationA, -minimumAxis);
            var collision = new FPCollision();
            FillBoxCollision(
                ref collision, minimumAxis, minimumPenetration, pointOnB, pointOnA);
            return collision;
        }

        /// <summary>检测有向包围盒与网格候选三角形并合并接触约束。</summary>
        public static FPCollision IntersectWithOBBAndMesh(
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            FixedPointVector3 queryMin,
            FixedPointVector3 queryMax,
            FPMeshCollider mesh,
            System.Collections.Generic.List<int> candidates)
        {
            mesh.CollectTriangleCandidates(queryMin, queryMax, candidates);
            var constraint = FixedPointVector3.zero;
            var representative = new FPCollision();
            var hasHit = false;
            foreach (var triangleIndex in candidates)
            {
                mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                var collision = IntersectWithOBBAndTriangle(position, halfSize, orientation, a, b, c);
                if (!collision.hit) continue;
                hasHit = true;
                representative = collision;
                AddConstraints(ref constraint, collision.normal * (collision.depth * 2));
            }

            if (!hasHit) return default;
            representative.collider = mesh;
            representative.normal = constraint.IsZero() ? representative.normal : constraint.normalized;
            representative.depth = constraint.magnitude * FixedPoint64.Half;
            return representative;
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
            var center = (min + max) * FixedPoint64.Half;
            var halfSize = (max - min) * FixedPoint64.Half;
            var centerProjection = FixedPointVector3.Dot(axis, center);
            var radius = halfSize.x * FixedPointMath.Abs(axis.x) +
                         halfSize.y * FixedPointMath.Abs(axis.y) +
                         halfSize.z * FixedPointMath.Abs(axis.z);
            return new FixedPointInterval
            {
                min = centerProjection - radius,
                max = centerProjection + radius
            };
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
            var centerProjection = FixedPointVector3.Dot(axis, position);
            var radius = halfSize.x * FixedPointMath.Abs(FixedPointVector3.Dot(axis, axisX)) +
                         halfSize.y * FixedPointMath.Abs(FixedPointVector3.Dot(axis, axisY)) +
                         halfSize.z * FixedPointMath.Abs(FixedPointVector3.Dot(axis, axisZ));
            return new FixedPointInterval
            {
                min = centerProjection - radius,
                max = centerProjection + radius
            };
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

        private static FixedPointVector3 SupportAabb(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 direction)
        {
            return new FixedPointVector3(
                direction.x >= FixedPoint64.Zero ? max.x : min.x,
                direction.y >= FixedPoint64.Zero ? max.y : min.y,
                direction.z >= FixedPoint64.Zero ? max.z : min.z);
        }

        private static FixedPointVector3 SupportObb(
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            FixedPointVector3 direction)
        {
            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            return position +
                   axisX * (FixedPointVector3.Dot(direction, axisX) >= FixedPoint64.Zero
                       ? halfSize.x
                       : -halfSize.x) +
                   axisY * (FixedPointVector3.Dot(direction, axisY) >= FixedPoint64.Zero
                       ? halfSize.y
                       : -halfSize.y) +
                   axisZ * (FixedPointVector3.Dot(direction, axisZ) >= FixedPoint64.Zero
                       ? halfSize.z
                       : -halfSize.z);
        }

        private static void FillBoxCollision(
            ref FPCollision collision,
            FixedPointVector3 normal,
            FixedPoint64 penetration,
            FixedPointVector3 pointOnB,
            FixedPointVector3 pointOnA)
        {
            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = pointOnB;
            collision.outsidePoint = pointOnA;
            collision.contactPoint = (pointOnA + pointOnB) * FixedPoint64.Half;
            collision.t = penetration;
            collision.depth = penetration * FixedPoint64.Half;
        }
    }
}
