namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供任意方向胶囊体相关的相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>
        /// 检测有限射线与胶囊体的首次相交。
        /// </summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="delta">射线的非零方向，无须归一化。</param>
        /// <param name="length">射线的最大检测距离。</param>
        /// <param name="min">胶囊体轴线起点。</param>
        /// <param name="max">胶囊体轴线终点。</param>
        /// <param name="radius">胶囊体半径。</param>
        /// <returns>首次命中的距离、位置和表面法线；未命中时 <see cref="FPCollision.hit"/> 为假。</returns>
        public static FPCollision IntersectWithRayAndCapsule(
            FixedPointVector3 origin,
            FixedPointVector3 delta,
            FixedPoint64 length,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPoint64 radius)
        {
            var collision = new FPCollision();

            if (delta.IsZero() || length < FixedPoint64.Zero || radius < FixedPoint64.Zero)
            {
                return collision;
            }

            var direction = delta.normalized;
            var axis = max - min;
            var axisLengthSquared = axis.sqrMagnitude;
            var bestDistance = FixedPoint64.MaxValue;
            var bestNormal = FixedPointVector3.zero;

            if (axisLengthSquared == FixedPoint64.Zero)
            {
                TrySelectRaySphereHit(origin, direction, length, min, radius, ref bestDistance, ref bestNormal);
            }
            else
            {
                // 先求射线与胶囊体圆柱侧面的交点，再与两个球形端帽比较最近距离。
                var originOffset = origin - min;
                var axisDotDirection = FixedPointVector3.Dot(axis, direction);
                var axisDotOffset = FixedPointVector3.Dot(axis, originOffset);
                var directionDotOffset = FixedPointVector3.Dot(direction, originOffset);
                var offsetLengthSquared = originOffset.sqrMagnitude;
                var coefficientA = axisLengthSquared - axisDotDirection * axisDotDirection;
                var coefficientB = axisLengthSquared * directionDotOffset - axisDotOffset * axisDotDirection;
                var coefficientC = axisLengthSquared * offsetLengthSquared - axisDotOffset * axisDotOffset -
                                   radius * radius * axisLengthSquared;
                var discriminant = coefficientB * coefficientB - coefficientA * coefficientC;

                if (coefficientA != FixedPoint64.Zero && discriminant >= FixedPoint64.Zero)
                {
                    var root = FixedPointMath.Sqrt(discriminant);
                    var distance = (-coefficientB - root) / coefficientA;

                    if (distance < FixedPoint64.Zero)
                    {
                        distance = (-coefficientB + root) / coefficientA;
                    }

                    var axisParameter = axisDotOffset + distance * axisDotDirection;

                    if (distance >= FixedPoint64.Zero && distance <= length &&
                        axisParameter >= FixedPoint64.Zero && axisParameter <= axisLengthSquared)
                    {
                        var hitPoint = origin + direction * distance;
                        var pointOnAxis = min + axis * (axisParameter / axisLengthSquared);
                        bestDistance = distance;
                        bestNormal = (hitPoint - pointOnAxis).normalized;
                    }
                }

                TrySelectRaySphereHit(origin, direction, length, min, radius, ref bestDistance, ref bestNormal);
                TrySelectRaySphereHit(origin, direction, length, max, radius, ref bestDistance, ref bestNormal);
            }

            if (bestDistance == FixedPoint64.MaxValue)
            {
                return collision;
            }

            collision.hit = true;
            collision.t = bestDistance;
            collision.closestPoint = origin + direction * bestDistance;
            collision.normal = bestNormal.IsZero() ? GetFallbackNormal(axis) : bestNormal;
            return collision;
        }

        /// <summary>
        /// 检测球体与任意方向胶囊体的重叠，并计算接触信息。
        /// </summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="start">胶囊体轴线起点。</param>
        /// <param name="end">胶囊体轴线终点。</param>
        /// <param name="radiusCapsule">胶囊体半径。</param>
        /// <returns>从胶囊体指向球体的碰撞法线及两形状表面点。</returns>
        public static FPCollision IntersectWithSphereAndCapsule(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radiusCapsule)
        {
            var collision = new FPCollision();
            var pointOnAxis = ClosestPointWithPointAndLine(start, end, center);
            var offset = center - pointOnAxis;
            var distanceSquared = offset.sqrMagnitude;
            var radiusSum = radius + radiusCapsule;

            if (distanceSquared > radiusSum * radiusSum)
            {
                return collision;
            }

            var distance = FixedPointMath.Sqrt(distanceSquared);
            var normal = distance > FixedPoint64.Zero
                ? offset / distance
                : GetFallbackNormal(end - start, center - (start + end) * FixedPoint64.Half);
            var penetration = FixedPointMath.Max(FixedPoint64.Zero, radiusSum - distance);

            collision.hit = true;
            collision.normal = normal;
            collision.outsidePoint = center - normal * radius;
            collision.closestPoint = pointOnAxis + normal * radiusCapsule;
            collision.contactPoint = (collision.outsidePoint + collision.closestPoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }

        /// <summary>
        /// 检测两个任意方向胶囊体的重叠，并计算接触信息。
        /// </summary>
        /// <param name="startA">胶囊体 A 的轴线起点。</param>
        /// <param name="endA">胶囊体 A 的轴线终点。</param>
        /// <param name="capsuleRadiusA">胶囊体 A 的半径。</param>
        /// <param name="startB">胶囊体 B 的轴线起点。</param>
        /// <param name="endB">胶囊体 B 的轴线终点。</param>
        /// <param name="capsuleRadiusB">胶囊体 B 的半径。</param>
        /// <returns>从胶囊体 B 指向胶囊体 A 的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithCapsuleAndCapsule(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 capsuleRadiusA,
            FixedPointVector3 startB,
            FixedPointVector3 endB,
            FixedPoint64 capsuleRadiusB)
        {
            var collision = new FPCollision();
            ClosestPointsOnLineSegments(startA, endA, startB, endB, out var pointOnA, out var pointOnB);
            var offset = pointOnA - pointOnB;
            var distanceSquared = offset.sqrMagnitude;
            var radiusSum = capsuleRadiusA + capsuleRadiusB;

            if (distanceSquared > radiusSum * radiusSum)
            {
                return collision;
            }

            var distance = FixedPointMath.Sqrt(distanceSquared);
            var centerOffset = (startA + endA) - (startB + endB);
            var normal = distance > FixedPoint64.Zero
                ? offset / distance
                : GetFallbackNormal(endA - startA, centerOffset);
            var penetration = FixedPointMath.Max(FixedPoint64.Zero, radiusSum - distance);

            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = pointOnB + normal * capsuleRadiusB;
            collision.outsidePoint = pointOnA - normal * capsuleRadiusA;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }

        /// <summary>检测两个胶囊碰撞器是否重叠。</summary>
        /// <param name="fpCapsuleA">胶囊碰撞器 A。</param>
        /// <param name="fpCapsuleB">胶囊碰撞器 B。</param>
        /// <returns>从 B 指向 A 的碰撞法线及表面接触信息。</returns>
        public static FPCollision IntersectWithCapsuleAndCapsule(
            FPCapsuleCollider fpCapsuleA,
            FPCapsuleCollider fpCapsuleB)
        {
            return IntersectWithCapsuleAndCapsule(
                fpCapsuleA.startPos,
                fpCapsuleA.endPos,
                fpCapsuleA.scaledRadius,
                fpCapsuleB.startPos,
                fpCapsuleB.endPos,
                fpCapsuleB.scaledRadius);
        }

        /// <summary>
        /// 检测胶囊体与双面无限平面的重叠。
        /// </summary>
        /// <param name="startPos">胶囊体轴线起点。</param>
        /// <param name="endPos">胶囊体轴线终点。</param>
        /// <param name="center">胶囊体中心，用于轴线穿过平面时选择分离方向。</param>
        /// <param name="radius">胶囊体半径。</param>
        /// <param name="planeDistance">平面到原点的有符号距离。</param>
        /// <param name="planeNormal">平面的单位法线。</param>
        /// <returns>从平面指向胶囊体的碰撞法线及接触信息。</returns>
        public static FPCollision IntersectWithCapsuleAndPlane(
            FixedPointVector3 startPos,
            FixedPointVector3 endPos,
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPoint64 planeDistance,
            FixedPointVector3 planeNormal)
        {
            var collision = new FPCollision();

            if (planeNormal.IsZero())
            {
                return collision;
            }

            var normal = planeNormal.normalized;
            var normalizedDistance = planeDistance / planeNormal.magnitude;
            var startDistance = FixedPointVector3.Dot(startPos, normal) - normalizedDistance;
            var endDistance = FixedPointVector3.Dot(endPos, normal) - normalizedDistance;
            FixedPointVector3 pointOnAxis;
            FixedPoint64 signedDistance;

            if ((startDistance <= FixedPoint64.Zero && endDistance >= FixedPoint64.Zero) ||
                (startDistance >= FixedPoint64.Zero && endDistance <= FixedPoint64.Zero))
            {
                var denominator = startDistance - endDistance;
                var parameter = denominator == FixedPoint64.Zero
                    ? FixedPoint64.Half
                    : startDistance / denominator;
                pointOnAxis = startPos + (endPos - startPos) * parameter;
                signedDistance = FixedPoint64.Zero;
            }
            else if (FixedPointMath.Abs(startDistance) <= FixedPointMath.Abs(endDistance))
            {
                pointOnAxis = startPos;
                signedDistance = startDistance;
            }
            else
            {
                pointOnAxis = endPos;
                signedDistance = endDistance;
            }

            var absoluteDistance = FixedPointMath.Abs(signedDistance);

            if (absoluteDistance > radius)
            {
                return collision;
            }

            var centerDistance = FixedPointVector3.Dot(center, normal) - normalizedDistance;
            var collisionNormal = signedDistance < FixedPoint64.Zero ||
                                  (signedDistance == FixedPoint64.Zero && centerDistance < FixedPoint64.Zero)
                ? -normal
                : normal;
            var pointOnPlane = pointOnAxis - normal * signedDistance;
            var outsidePoint = pointOnAxis - collisionNormal * radius;

            collision.hit = true;
            collision.normal = collisionNormal;
            collision.closestPoint = pointOnPlane;
            collision.outsidePoint = outsidePoint;
            collision.contactPoint = (pointOnPlane + outsidePoint) * FixedPoint64.Half;
            collision.depth = FixedPointMath.Max(FixedPoint64.Zero, radius - absoluteDistance) * FixedPoint64.Half;
            return collision;
        }

        /// <summary>
        /// 检测胶囊体与有限圆柱体的重叠。
        /// </summary>
        /// <param name="startA">胶囊体轴线起点。</param>
        /// <param name="endA">胶囊体轴线终点。</param>
        /// <param name="capsuleRadiusA">胶囊体半径。</param>
        /// <param name="startB">圆柱体轴线起点。</param>
        /// <param name="endB">圆柱体轴线终点。</param>
        /// <param name="cylinderRadiusB">圆柱体半径。</param>
        /// <returns>胶囊体与圆柱侧面或端面重叠时的接触信息。</returns>
        public static FPCollision IntersectWithCapsuleAndCylinder(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 capsuleRadiusA,
            FixedPointVector3 startB,
            FixedPointVector3 endB,
            FixedPoint64 cylinderRadiusB)
        {
            ClosestPointsOnLineSegments(startA, endA, startB, endB, out var pointOnA, out var pointOnB);
            FPCollision collision;

            if (pointOnB == startB)
            {
                collision = IntersectWithSphereAndCircle(
                    pointOnA,
                    capsuleRadiusA,
                    startB,
                    (startB - endB).IsZero() ? FixedPointVector3.down : (startB - endB).normalized,
                    cylinderRadiusB);
            }
            else if (pointOnB == endB)
            {
                collision = IntersectWithSphereAndCircle(
                    pointOnA,
                    capsuleRadiusA,
                    endB,
                    (endB - startB).IsZero() ? FixedPointVector3.up : (endB - startB).normalized,
                    cylinderRadiusB);
            }
            else
            {
                var offset = pointOnA - pointOnB;
                var distance = offset.magnitude;
                var radiusSum = capsuleRadiusA + cylinderRadiusB;
                collision = new FPCollision();

                if (distance <= radiusSum)
                {
                    var normal = distance > FixedPoint64.Zero
                        ? offset / distance
                        : GetFallbackNormal(endB - startB, (startA + endA) - (startB + endB));
                    collision.hit = true;
                    collision.normal = normal;
                    collision.closestPoint = pointOnB + normal * cylinderRadiusB;
                    collision.outsidePoint = pointOnA - normal * capsuleRadiusA;
                    collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
                    collision.depth = FixedPointMath.Max(FixedPoint64.Zero, radiusSum - distance) * FixedPoint64.Half;
                }
            }

#if UNITY_EDITOR
            collision.debugInfo = pointOnA;
            collision.debugInfo1 = pointOnB;
#endif
            return collision;
        }

        private static void TrySelectRaySphereHit(
            FixedPointVector3 origin,
            FixedPointVector3 normalizedDirection,
            FixedPoint64 length,
            FixedPointVector3 center,
            FixedPoint64 radius,
            ref FixedPoint64 bestDistance,
            ref FixedPointVector3 bestNormal)
        {
            var offset = origin - center;
            var linear = FixedPointVector3.Dot(offset, normalizedDirection);
            var constant = offset.sqrMagnitude - radius * radius;
            var discriminant = linear * linear - constant;

            if (discriminant < FixedPoint64.Zero)
            {
                return;
            }

            var root = FixedPointMath.Sqrt(discriminant);
            var distance = -linear - root;

            if (distance < FixedPoint64.Zero)
            {
                distance = -linear + root;
            }

            if (distance < FixedPoint64.Zero || distance > length || distance >= bestDistance)
            {
                return;
            }

            bestDistance = distance;
            bestNormal = (origin + normalizedDirection * distance - center).normalized;
        }

        private static FixedPointVector3 GetFallbackNormal(FixedPointVector3 axis)
        {
            return GetFallbackNormal(axis, FixedPointVector3.zero);
        }

        private static FixedPointVector3 GetFallbackNormal(
            FixedPointVector3 axis,
            FixedPointVector3 preferredDirection)
        {
            if (!preferredDirection.IsZero())
            {
                return preferredDirection.normalized;
            }

            if (axis.IsZero())
            {
                return FixedPointVector3.up;
            }

            var normal = FixedPointVector3.Cross(axis, FixedPointVector3.right);

            if (normal.IsZero())
            {
                normal = FixedPointVector3.Cross(axis, FixedPointVector3.up);
            }

            return normal.normalized;
        }
    }
}