namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供有限实心圆柱体相关的相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>
        /// 检测球体与有限实心圆柱体的重叠，并计算最近表面接触信息。
        /// </summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="start">圆柱体轴线起点，即底面中心。</param>
        /// <param name="end">圆柱体轴线终点，即顶面中心。</param>
        /// <param name="radiusCylinder">圆柱体半径。</param>
        /// <returns>从圆柱体指向球体的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithSphereAndCylinder(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radiusCylinder)
        {
            var collision = new FPCollision();
            var axis = end - start;
            var height = axis.magnitude;

            if (height == FixedPoint64.Zero)
            {
                return IntersectWithSphereAndCircle(
                    center,
                    radius,
                    start,
                    FixedPointVector3.up,
                    radiusCylinder);
            }

            var axisNormal = axis / height;
            var offset = center - start;
            var axialDistance = FixedPointVector3.Dot(offset, axisNormal);
            var radialOffset = offset - axisNormal * axialDistance;
            var radialDistance = radialOffset.magnitude;
            var radialNormal = radialDistance > FixedPoint64.Zero
                ? radialOffset / radialDistance
                : GetFallbackNormal(axisNormal);

            var insideAxialRange = axialDistance >= FixedPoint64.Zero && axialDistance <= height;
            var insideRadialRange = radialDistance <= radiusCylinder;
            FixedPointVector3 pointOnCylinder;
            FixedPointVector3 normal;
            FixedPoint64 penetration;

            if (insideAxialRange && insideRadialRange)
            {
                // 球心位于圆柱体内部时，选择到侧面或两个端面中最近的离开方向。
                var distanceToSide = radiusCylinder - radialDistance;
                var distanceToStartCap = axialDistance;
                var distanceToEndCap = height - axialDistance;

                if (distanceToSide <= distanceToStartCap && distanceToSide <= distanceToEndCap)
                {
                    normal = radialNormal;
                    pointOnCylinder = center + normal * distanceToSide;
                    penetration = radius + distanceToSide;
                }
                else if (distanceToStartCap <= distanceToEndCap)
                {
                    normal = -axisNormal;
                    pointOnCylinder = center - axisNormal * distanceToStartCap;
                    penetration = radius + distanceToStartCap;
                }
                else
                {
                    normal = axisNormal;
                    pointOnCylinder = center + axisNormal * distanceToEndCap;
                    penetration = radius + distanceToEndCap;
                }
            }
            else
            {
                var clampedAxialDistance = FixedPointMath.Clamp(
                    axialDistance,
                    FixedPoint64.Zero,
                    height);
                var clampedRadialDistance = FixedPointMath.Min(radialDistance, radiusCylinder);
                pointOnCylinder = start + axisNormal * clampedAxialDistance + radialNormal * clampedRadialDistance;
                var separation = center - pointOnCylinder;
                var distance = separation.magnitude;

                if (distance > radius)
                {
                    return collision;
                }

                normal = distance > FixedPoint64.Zero ? separation / distance : radialNormal;
                penetration = FixedPointMath.Max(FixedPoint64.Zero, radius - distance);
            }

            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = pointOnCylinder;
            collision.outsidePoint = center - normal * radius;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }
    }
}