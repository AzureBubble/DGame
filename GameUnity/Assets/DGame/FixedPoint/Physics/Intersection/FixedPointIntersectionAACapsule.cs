using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供世界 Y 轴对齐胶囊体相关的相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>检测 Y 轴对齐胶囊体与球体的重叠。</summary>
        /// <param name="start">胶囊体轴线起点。</param>
        /// <param name="end">胶囊体轴线终点。</param>
        /// <param name="radiusCapsule">胶囊体半径。</param>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <returns>从球体指向胶囊体的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithAACapsuleAndSphere(
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radiusCapsule,
            FixedPointVector3 center,
            FixedPoint64 radius)
        {
            var collision = IntersectWithSphereAndAACapsule(center, radius, start, end, radiusCapsule);
            (collision.closestPoint, collision.outsidePoint) = (collision.outsidePoint, collision.closestPoint);
            collision.normal = -collision.normal;
            return collision;
        }

        /// <summary>检测球体与 Y 轴对齐胶囊体的重叠。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="start">胶囊体轴线起点。</param>
        /// <param name="end">胶囊体轴线终点。</param>
        /// <param name="radiusCapsule">胶囊体半径。</param>
        /// <returns>从胶囊体指向球体的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithSphereAndAACapsule(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radiusCapsule)
        {
            // 通用胶囊算法同样适用于 Y 轴对齐胶囊，并能正确处理端点顺序和退化轴线。
            return IntersectWithSphereAndCapsule(center, radius, start, end, radiusCapsule);
        }

        /// <summary>检测两个 Y 轴对齐胶囊体的重叠。</summary>
        /// <param name="startA">胶囊体 A 的轴线起点。</param>
        /// <param name="endA">胶囊体 A 的轴线终点。</param>
        /// <param name="radiusCapsuleA">胶囊体 A 的半径。</param>
        /// <param name="startB">胶囊体 B 的轴线起点。</param>
        /// <param name="endB">胶囊体 B 的轴线终点。</param>
        /// <param name="radiusCapsuleB">胶囊体 B 的半径。</param>
        /// <returns>从胶囊体 B 指向胶囊体 A 的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithAACapsuleAndAACapsule(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 radiusCapsuleA,
            FixedPointVector3 startB,
            FixedPointVector3 endB,
            FixedPoint64 radiusCapsuleB)
        {
            return IntersectWithCapsuleAndCapsule(
                startA,
                endA,
                radiusCapsuleA,
                startB,
                endB,
                radiusCapsuleB);
        }

        /// <summary>检测 Y 轴对齐胶囊体与轴对齐包围盒的重叠。</summary>
        /// <param name="startA">胶囊体轴线起点。</param>
        /// <param name="endA">胶囊体轴线终点。</param>
        /// <param name="radiusCapsuleA">胶囊体半径。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>从包围盒指向胶囊体的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithAACapsuleAndAABB(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 radiusCapsuleA,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            ClosestPointsWithSegmentAndAABB(startA, endA, min, max, out var pointOnSegment, out _);
            return IntersectWithSphereAndAABB(pointOnSegment, radiusCapsuleA, min, max);
        }

        /// <summary>检测 Y 轴对齐胶囊体与有向包围盒的重叠。</summary>
        /// <param name="startA">胶囊体轴线起点。</param>
        /// <param name="endA">胶囊体轴线终点。</param>
        /// <param name="radiusCapsuleA">胶囊体半径。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="fixedPointMatrix">包含三个单位局部轴的旋转矩阵。</param>
        /// <param name="min">有向包围盒的世界坐标 AABB 最小点，仅保留用于兼容现有调用。</param>
        /// <param name="max">有向包围盒的世界坐标 AABB 最大点，仅保留用于兼容现有调用。</param>
        /// <returns>从有向包围盒指向胶囊体的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithAACapsuleAndOBB(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 radiusCapsuleA,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            var axisX = new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13);
            var axisY = new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23);
            var axisZ = new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33);
            var startOffset = startA - position;
            var endOffset = endA - position;
            var localStart = new FixedPointVector3(
                FixedPointVector3.Dot(startOffset, axisX),
                FixedPointVector3.Dot(startOffset, axisY),
                FixedPointVector3.Dot(startOffset, axisZ));
            var localEnd = new FixedPointVector3(
                FixedPointVector3.Dot(endOffset, axisX),
                FixedPointVector3.Dot(endOffset, axisY),
                FixedPointVector3.Dot(endOffset, axisZ));

            ClosestPointsWithSegmentAndAABB(
                localStart,
                localEnd,
                -halfSize,
                halfSize,
                out var localPointOnSegment,
                out _);
            var worldPointOnSegment = position +
                                      axisX * localPointOnSegment.x +
                                      axisY * localPointOnSegment.y +
                                      axisZ * localPointOnSegment.z;
            return IntersectWithSphereAndOBB(
                worldPointOnSegment,
                radiusCapsuleA,
                position,
                halfSize,
                fixedPointMatrix);
        }

        /// <summary>
        /// 精确计算闭线段与轴对齐包围盒之间的一对最近点。
        /// </summary>
        /// <remarks>
        /// 点到包围盒的平方距离沿线段参数是分段二次凸函数；以穿越各轴边界的参数为分段点，
        /// 在每段内求导即可得到全局最近点。
        /// </remarks>
        private static void ClosestPointsWithSegmentAndAABB(
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPointVector3 min,
            FixedPointVector3 max,
            out FixedPointVector3 pointOnSegment,
            out FixedPointVector3 pointOnBox)
        {
            var direction = end - start;
            var breakpoints = new List<FixedPoint64>(8)
            {
                FixedPoint64.Zero,
                FixedPoint64.One
            };
            AddSegmentBoxBreakpoints(breakpoints, start.x, direction.x, min.x, max.x);
            AddSegmentBoxBreakpoints(breakpoints, start.y, direction.y, min.y, max.y);
            AddSegmentBoxBreakpoints(breakpoints, start.z, direction.z, min.z, max.z);
            breakpoints.Sort();

            var bestParameter = FixedPoint64.Zero;
            var bestDistanceSquared = FixedPoint64.MaxValue;

            for (var i = 0; i < breakpoints.Count - 1; i++)
            {
                var intervalMin = breakpoints[i];
                var intervalMax = breakpoints[i + 1];
                var midpoint = (intervalMin + intervalMax) * FixedPoint64.Half;
                var sample = start + direction * midpoint;
                var numerator = FixedPoint64.Zero;
                var denominator = FixedPoint64.Zero;

                AccumulateSegmentBoxAxis(start.x, direction.x, sample.x, min.x, max.x, ref numerator, ref denominator);
                AccumulateSegmentBoxAxis(start.y, direction.y, sample.y, min.y, max.y, ref numerator, ref denominator);
                AccumulateSegmentBoxAxis(start.z, direction.z, sample.z, min.z, max.z, ref numerator, ref denominator);

                var candidate = denominator == FixedPoint64.Zero
                    ? intervalMin
                    : FixedPointMath.Clamp(-numerator / denominator, intervalMin, intervalMax);
                SelectCloserSegmentBoxParameter(start, direction, min, max, intervalMin, ref bestParameter,
                    ref bestDistanceSquared);
                SelectCloserSegmentBoxParameter(start, direction, min, max, intervalMax, ref bestParameter,
                    ref bestDistanceSquared);
                SelectCloserSegmentBoxParameter(start, direction, min, max, candidate, ref bestParameter,
                    ref bestDistanceSquared);
            }

            pointOnSegment = start + direction * bestParameter;
            pointOnBox = ClosestPointWithPointAndAABB(pointOnSegment, min, max);
        }

        private static void AddSegmentBoxBreakpoints(
            List<FixedPoint64> breakpoints,
            FixedPoint64 start,
            FixedPoint64 direction,
            FixedPoint64 min,
            FixedPoint64 max)
        {
            if (direction == FixedPoint64.Zero)
            {
                return;
            }

            var minParameter = (min - start) / direction;
            var maxParameter = (max - start) / direction;

            if (minParameter > FixedPoint64.Zero && minParameter < FixedPoint64.One)
            {
                breakpoints.Add(minParameter);
            }

            if (maxParameter > FixedPoint64.Zero && maxParameter < FixedPoint64.One)
            {
                breakpoints.Add(maxParameter);
            }
        }

        private static void AccumulateSegmentBoxAxis(
            FixedPoint64 start,
            FixedPoint64 direction,
            FixedPoint64 sample,
            FixedPoint64 min,
            FixedPoint64 max,
            ref FixedPoint64 numerator,
            ref FixedPoint64 denominator)
        {
            FixedPoint64 boundary;

            if (sample < min)
            {
                boundary = min;
            }
            else if (sample > max)
            {
                boundary = max;
            }
            else
            {
                return;
            }

            numerator += direction * (start - boundary);
            denominator += direction * direction;
        }

        private static void SelectCloserSegmentBoxParameter(
            FixedPointVector3 start,
            FixedPointVector3 direction,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPoint64 candidate,
            ref FixedPoint64 bestParameter,
            ref FixedPoint64 bestDistanceSquared)
        {
            var point = start + direction * candidate;
            var closestPoint = ClosestPointWithPointAndAABB(point, min, max);
            var distanceSquared = (point - closestPoint).sqrMagnitude;

            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestParameter = candidate;
            }
        }
    }
}