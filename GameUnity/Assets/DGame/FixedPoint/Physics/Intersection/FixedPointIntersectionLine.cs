namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供线段和无限直线相关的定点数最近点与相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>
        /// 计算两条闭线段之间的一对最近点及其距离。
        /// </summary>
        /// <param name="startA">线段 A 的起点。</param>
        /// <param name="endA">线段 A 的终点。</param>
        /// <param name="startB">线段 B 的起点。</param>
        /// <param name="endB">线段 B 的终点。</param>
        /// <returns>依次返回线段 A 上的最近点、线段 B 上的最近点以及两点距离。</returns>
        public static (
            FixedPointVector3 pointOnA,
            FixedPointVector3 pointOnB,
            FixedPoint64 distance) ClosestPointOnLineSegmentToLineSegment(
                FixedPointVector3 startA,
                FixedPointVector3 endA,
                FixedPointVector3 startB,
                FixedPointVector3 endB)
        {
            ClosestPointsOnLineSegments(startA, endA, startB, endB, out var pointOnA, out var pointOnB);
            return (pointOnA, pointOnB, FixedPointVector3.Distance(pointOnA, pointOnB));
        }

        /// <summary>
        /// 计算无限直线穿过有向包围盒时的进入点和离开点。
        /// </summary>
        /// <param name="origin">直线上的一点。</param>
        /// <param name="direct">直线的非零方向，无须归一化。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="orientation">包含三个单位局部轴的旋转矩阵。</param>
        /// <param name="intersection">命中时返回进入点、离开点和离开面的法线。</param>
        /// <returns>沿 <paramref name="direct"/> 到离开点的参数；不相交时返回 <c>-1</c>。</returns>
        public static FixedPoint64 ClosestWithLineAndOBB(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            out FPCollision intersection)
        {
            intersection = new FPCollision();

            if (direct.IsZero())
            {
                return -1;
            }

            var axes = new[]
            {
                new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13),
                new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23),
                new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33)
            };
            var extents = new[] { halfSize.x, halfSize.y, halfSize.z };
            var offset = position - origin;
            var tMin = FixedPoint64.MinValue;
            var tMax = FixedPoint64.MaxValue;
            var exitNormal = FixedPointVector3.zero;

            for (var i = 0; i < axes.Length; i++)
            {
                var projectedDirection = FixedPointVector3.Dot(axes[i], direct);
                var projectedOffset = FixedPointVector3.Dot(axes[i], offset);

                if (projectedDirection == FixedPoint64.Zero)
                {
                    if (FixedPointMath.Abs(projectedOffset) > extents[i])
                    {
                        return -1;
                    }

                    continue;
                }

                var first = (projectedOffset - extents[i]) / projectedDirection;
                var second = (projectedOffset + extents[i]) / projectedDirection;
                var firstNormal = -axes[i];
                var secondNormal = axes[i];

                if (first > second)
                {
                    (first, second) = (second, first);
                    (firstNormal, secondNormal) = (secondNormal, firstNormal);
                }

                if (first > tMin)
                {
                    tMin = first;
                }

                if (second < tMax)
                {
                    tMax = second;
                    exitNormal = secondNormal;
                }

                if (tMin > tMax)
                {
                    return -1;
                }
            }

            intersection.hit = true;
            intersection.t = tMax;
            intersection.outsidePoint = origin + direct * tMin;
            intersection.closestPoint = origin + direct * tMax;
            intersection.contactPoint = (intersection.outsidePoint + intersection.closestPoint) * FixedPoint64.Half;
            intersection.normal = exitNormal;
            return tMax;
        }

        /// <summary>
        /// 使用《Real-Time Collision Detection》的线段最近点算法计算两条闭线段上的最近点。
        /// </summary>
        private static void ClosestPointsOnLineSegments(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPointVector3 startB,
            FixedPointVector3 endB,
            out FixedPointVector3 pointOnA,
            out FixedPointVector3 pointOnB)
        {
            var directionA = endA - startA;
            var directionB = endB - startB;
            var offset = startA - startB;
            var lengthSquaredA = FixedPointVector3.Dot(directionA, directionA);
            var lengthSquaredB = FixedPointVector3.Dot(directionB, directionB);
            var directionBDotOffset = FixedPointVector3.Dot(directionB, offset);
            FixedPoint64 parameterA;
            FixedPoint64 parameterB;

            if (lengthSquaredA == FixedPoint64.Zero && lengthSquaredB == FixedPoint64.Zero)
            {
                pointOnA = startA;
                pointOnB = startB;
                return;
            }

            if (lengthSquaredA == FixedPoint64.Zero)
            {
                parameterA = FixedPoint64.Zero;
                parameterB = FixedPointMath.Clamp(
                    directionBDotOffset / lengthSquaredB,
                    FixedPoint64.Zero,
                    FixedPoint64.One);
            }
            else
            {
                var directionADotOffset = FixedPointVector3.Dot(directionA, offset);

                if (lengthSquaredB == FixedPoint64.Zero)
                {
                    parameterB = FixedPoint64.Zero;
                    parameterA = FixedPointMath.Clamp(
                        -directionADotOffset / lengthSquaredA,
                        FixedPoint64.Zero,
                        FixedPoint64.One);
                }
                else
                {
                    var directionsDot = FixedPointVector3.Dot(directionA, directionB);
                    var denominator = lengthSquaredA * lengthSquaredB - directionsDot * directionsDot;
                    parameterA = denominator == FixedPoint64.Zero
                        ? FixedPoint64.Zero
                        : FixedPointMath.Clamp(
                            (directionsDot * directionBDotOffset - directionADotOffset * lengthSquaredB) /
                            denominator,
                            FixedPoint64.Zero,
                            FixedPoint64.One);

                    parameterB = (directionsDot * parameterA + directionBDotOffset) / lengthSquaredB;

                    if (parameterB < FixedPoint64.Zero)
                    {
                        parameterB = FixedPoint64.Zero;
                        parameterA = FixedPointMath.Clamp(
                            -directionADotOffset / lengthSquaredA,
                            FixedPoint64.Zero,
                            FixedPoint64.One);
                    }
                    else if (parameterB > FixedPoint64.One)
                    {
                        parameterB = FixedPoint64.One;
                        parameterA = FixedPointMath.Clamp(
                            (directionsDot - directionADotOffset) / lengthSquaredA,
                            FixedPoint64.Zero,
                            FixedPoint64.One);
                    }
                }
            }

            pointOnA = startA + directionA * parameterA;
            pointOnB = startB + directionB * parameterB;
        }
    }
}