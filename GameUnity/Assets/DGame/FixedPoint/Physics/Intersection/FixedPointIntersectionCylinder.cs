using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供有限实心圆柱体相关的相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>检测有限射线与有限实心圆柱体的首次相交。</summary>
        /// <returns>最近合法交点、外法线和世界距离；起点严格位于圆柱内部时不命中。</returns>
        public static FPCollision IntersectWithRayAndCylinder(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius)
        {
            var collision = new FPCollision();
            if (direct.IsZero() || length < FixedPoint64.Zero || radius < FixedPoint64.Zero)
            {
                return collision;
            }

            var axis = end - start;
            var height = axis.magnitude;
            if (height == FixedPoint64.Zero)
            {
                return collision;
            }

            var axisNormal = axis / height;
            var direction = direct.normalized;
            var originOffset = origin - start;
            var axialOrigin = FixedPointVector3.Dot(originOffset, axisNormal);
            var radialOrigin = originOffset - axisNormal * axialOrigin;
            if (axialOrigin > FixedPoint64.Zero && axialOrigin < height &&
                radialOrigin.sqrMagnitude < radius * radius)
            {
                return collision;
            }

            var bestDistance = FixedPoint64.MaxValue;
            var bestNormal = FixedPointVector3.zero;
            var axialDirection = FixedPointVector3.Dot(direction, axisNormal);
            var radialDirection = direction - axisNormal * axialDirection;
            var coefficientA = radialDirection.sqrMagnitude;
            var coefficientB = FixedPointVector3.Dot(radialOrigin, radialDirection);
            var coefficientC = radialOrigin.sqrMagnitude - radius * radius;
            var discriminant = coefficientB * coefficientB - coefficientA * coefficientC;

            if (coefficientA > FixedPoint64.Zero && discriminant >= FixedPoint64.Zero)
            {
                var root = FixedPointMath.Sqrt(discriminant);
                SelectCylinderSideHit((-coefficientB - root) / coefficientA);
                SelectCylinderSideHit((-coefficientB + root) / coefficientA);
            }

            if (axialDirection != FixedPoint64.Zero)
            {
                SelectCylinderCapHit(-axialOrigin / axialDirection, -axisNormal);
                SelectCylinderCapHit((height - axialOrigin) / axialDirection, axisNormal);
            }

            if (bestDistance == FixedPoint64.MaxValue)
            {
                return collision;
            }

            collision.hit = true;
            collision.t = bestDistance;
            collision.closestPoint = origin + direction * bestDistance;
            collision.normal = bestNormal;
            return collision;

            void SelectCylinderSideHit(FixedPoint64 distance)
            {
                if (distance < FixedPoint64.Zero || distance > length || distance >= bestDistance) return;
                var axialDistance = axialOrigin + axialDirection * distance;
                if (axialDistance < FixedPoint64.Zero || axialDistance > height) return;
                var hitPoint = origin + direction * distance;
                var pointOnAxis = start + axisNormal * axialDistance;
                var normal = hitPoint - pointOnAxis;
                if (normal.IsZero()) return;
                bestDistance = distance;
                bestNormal = normal.normalized;
            }

            void SelectCylinderCapHit(FixedPoint64 distance, FixedPointVector3 normal)
            {
                if (distance < FixedPoint64.Zero || distance > length || distance >= bestDistance) return;
                var hitPoint = origin + direction * distance;
                var capCenter = normal == axisNormal ? end : start;
                if ((hitPoint - capCenter).sqrMagnitude > radius * radius) return;
                bestDistance = distance;
                bestNormal = normal;
            }
        }

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

        /// <summary>Tests an AABB against a finite solid cylinder.</summary>
        public static FPCollision IntersectWithAABBAndCylinder(
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 cylinderStart,
            FixedPointVector3 cylinderEnd,
            FixedPoint64 cylinderRadius)
        {
            var center = (min + max) * FixedPoint64.Half;
            var halfSize = (max - min) * FixedPoint64.Half;
            return IntersectConvexShapes(
                ConvexShape.CreateBox(center, halfSize, FixedPointMatrix.Identity),
                ConvexShape.CreateCylinder(cylinderStart, cylinderEnd, cylinderRadius));
        }

        /// <summary>Tests an OBB against a finite solid cylinder.</summary>
        public static FPCollision IntersectWithOBBAndCylinder(
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            FixedPointVector3 cylinderStart,
            FixedPointVector3 cylinderEnd,
            FixedPoint64 cylinderRadius)
        {
            return IntersectConvexShapes(
                ConvexShape.CreateBox(position, halfSize, orientation),
                ConvexShape.CreateCylinder(cylinderStart, cylinderEnd, cylinderRadius));
        }

        /// <summary>Tests two finite solid cylinders.</summary>
        public static FPCollision IntersectWithCylinderAndCylinder(
            FixedPointVector3 startA,
            FixedPointVector3 endA,
            FixedPoint64 radiusA,
            FixedPointVector3 startB,
            FixedPointVector3 endB,
            FixedPoint64 radiusB)
        {
            return IntersectConvexShapes(
                ConvexShape.CreateCylinder(startA, endA, radiusA),
                ConvexShape.CreateCylinder(startB, endB, radiusB));
        }

        /// <summary>Tests a finite solid cylinder against one triangle.</summary>
        public static FPCollision IntersectWithCylinderAndTriangle(
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            return IntersectConvexShapes(
                ConvexShape.CreateCylinder(start, end, radius),
                ConvexShape.CreateTriangle(a, b, c));
        }

        /// <summary>Tests a finite solid cylinder against candidate triangles in a mesh.</summary>
        public static FPCollision IntersectWithCylinderAndMesh(
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius,
            FPMeshCollider mesh,
            List<int> candidates)
        {
            var radiusVector = new FixedPointVector3(radius, radius, radius);
            mesh.CollectTriangleCandidates(
                FixedPointVector3.Min(start, end) - radiusVector,
                FixedPointVector3.Max(start, end) + radiusVector,
                candidates);

            var best = new FPCollision();
            foreach (var triangleIndex in candidates)
            {
                mesh.GetWorldTriangle(triangleIndex, out var a, out var b, out var c);
                var collision = IntersectWithCylinderAndTriangle(start, end, radius, a, b, c);
                if (collision.hit && (!best.hit || collision.depth > best.depth))
                {
                    best = collision;
                }
            }

            return best;
        }

        /// <summary>
        /// 使用球心到有限实心圆柱的有符号距离场执行球体扫掠检测。
        /// 该算法会区分圆柱平面端盖、圆角边缘和侧面，不把圆柱近似成胶囊体。
        /// </summary>
        public static FPCollision IntersectWithSphereCastAndCylinder(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPoint64 sphereRadius,
            FixedPointVector3 cylinderStart,
            FixedPointVector3 cylinderEnd,
            FixedPoint64 cylinderRadius)
        {
            var collision = new FPCollision();
            if (direction.IsZero() || length < FixedPoint64.Zero || sphereRadius < FixedPoint64.Zero ||
                cylinderRadius < FixedPoint64.Zero || cylinderStart == cylinderEnd)
            {
                return collision;
            }

            var normalizedDirection = direction.normalized;
            var distanceAlongRay = FixedPoint64.Zero;
            const int maxIterations = 96;

            for (var iteration = 0; iteration < maxIterations; iteration++)
            {
                var center = origin + normalizedDirection * distanceAlongRay;
                var signedDistance = SignedDistanceToCylinder(
                    center, cylinderStart, cylinderEnd, cylinderRadius, out var normal);
                var separation = signedDistance - sphereRadius;

                if (separation <= FixedPoint64.EN4)
                {
                    collision.hit = true;
                    collision.t = distanceAlongRay;
                    collision.closestPoint = center;
                    collision.normal = normal;
                    return collision;
                }

                distanceAlongRay += FixedPointMath.Max(separation, FixedPoint64.EN5);
                if (distanceAlongRay > length)
                {
                    return collision;
                }
            }

            return collision;
        }

        private static FixedPoint64 SignedDistanceToCylinder(
            FixedPointVector3 point,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius,
            out FixedPointVector3 normal)
        {
            var axis = end - start;
            var height = axis.magnitude;
            var axisNormal = axis / height;
            var offset = point - start;
            var axialDistance = FixedPointVector3.Dot(offset, axisNormal);
            var radialOffset = offset - axisNormal * axialDistance;
            var radialDistance = radialOffset.magnitude;
            var radialNormal = radialDistance > FixedPoint64.Zero
                ? radialOffset / radialDistance
                : GetFallbackNormal(axisNormal);

            var outsideAxial = axialDistance < FixedPoint64.Zero
                ? axialDistance
                : axialDistance > height
                    ? axialDistance - height
                    : FixedPoint64.Zero;
            var outsideRadial = FixedPointMath.Max(FixedPoint64.Zero, radialDistance - radius);

            if (outsideAxial != FixedPoint64.Zero || outsideRadial > FixedPoint64.Zero)
            {
                var clampedAxial = FixedPointMath.Clamp(axialDistance, FixedPoint64.Zero, height);
                var clampedRadial = FixedPointMath.Min(radialDistance, radius);
                var closestPoint = start + axisNormal * clampedAxial + radialNormal * clampedRadial;
                var separation = point - closestPoint;
                var distance = separation.magnitude;
                normal = distance > FixedPoint64.Zero
                    ? separation / distance
                    : axialDistance < FixedPoint64.Zero ? -axisNormal : axisNormal;
                return distance;
            }

            var distanceToSide = radius - radialDistance;
            var distanceToStartCap = axialDistance;
            var distanceToEndCap = height - axialDistance;
            if (distanceToSide <= distanceToStartCap && distanceToSide <= distanceToEndCap)
            {
                normal = radialNormal;
                return -distanceToSide;
            }

            if (distanceToStartCap <= distanceToEndCap)
            {
                normal = -axisNormal;
                return -distanceToStartCap;
            }

            normal = axisNormal;
            return -distanceToEndCap;
        }

        private enum ConvexShapeType : byte
        {
            Box,
            Cylinder,
            Triangle
        }

        private readonly struct ConvexShape
        {
            internal readonly ConvexShapeType type;
            internal readonly FixedPointVector3 center;
            internal readonly FixedPointVector3 size;
            internal readonly FixedPointMatrix orientation;
            internal readonly FixedPointVector3 pointA;
            internal readonly FixedPointVector3 pointB;
            internal readonly FixedPointVector3 pointC;
            internal readonly FixedPoint64 radius;

            private ConvexShape(
                ConvexShapeType type,
                FixedPointVector3 center,
                FixedPointVector3 size,
                FixedPointMatrix orientation,
                FixedPointVector3 pointA,
                FixedPointVector3 pointB,
                FixedPointVector3 pointC,
                FixedPoint64 radius)
            {
                this.type = type;
                this.center = center;
                this.size = size;
                this.orientation = orientation;
                this.pointA = pointA;
                this.pointB = pointB;
                this.pointC = pointC;
                this.radius = radius;
            }

            internal static ConvexShape CreateBox(
                FixedPointVector3 center,
                FixedPointVector3 halfSize,
                FixedPointMatrix orientation)
            {
                return new ConvexShape(ConvexShapeType.Box, center, halfSize, orientation,
                    default, default, default, FixedPoint64.Zero);
            }

            internal static ConvexShape CreateCylinder(
                FixedPointVector3 start,
                FixedPointVector3 end,
                FixedPoint64 radius)
            {
                return new ConvexShape(ConvexShapeType.Cylinder, (start + end) * FixedPoint64.Half,
                    default, default, start, end, default, radius);
            }

            internal static ConvexShape CreateTriangle(
                FixedPointVector3 a,
                FixedPointVector3 b,
                FixedPointVector3 c)
            {
                return new ConvexShape(ConvexShapeType.Triangle,
                    (a + b + c) / 3, default, default, a, b, c, FixedPoint64.Zero);
            }

            internal FixedPointVector3 Support(FixedPointVector3 direction)
            {
                switch (type)
                {
                    case ConvexShapeType.Box:
                    {
                        var axisX = new FixedPointVector3(
                            orientation.M11, orientation.M12, orientation.M13);
                        var axisY = new FixedPointVector3(
                            orientation.M21, orientation.M22, orientation.M23);
                        var axisZ = new FixedPointVector3(
                            orientation.M31, orientation.M32, orientation.M33);
                        return center +
                               axisX * (FixedPointVector3.Dot(direction, axisX) >= FixedPoint64.Zero
                                   ? size.x : -size.x) +
                               axisY * (FixedPointVector3.Dot(direction, axisY) >= FixedPoint64.Zero
                                   ? size.y : -size.y) +
                               axisZ * (FixedPointVector3.Dot(direction, axisZ) >= FixedPoint64.Zero
                                   ? size.z : -size.z);
                    }
                    case ConvexShapeType.Cylinder:
                    {
                        var axis = pointB - pointA;
                        var axisNormal = axis.normalized;
                        var axialDirection = FixedPointVector3.Dot(direction, axisNormal);
                        var capCenter = axialDirection >= FixedPoint64.Zero ? pointB : pointA;
                        var radialDirection = direction - axisNormal * axialDirection;
                        return radialDirection.IsZero()
                            ? capCenter
                            : capCenter + radialDirection.normalized * radius;
                    }
                    default:
                    {
                        var result = pointA;
                        var bestProjection = FixedPointVector3.Dot(pointA, direction);
                        var projection = FixedPointVector3.Dot(pointB, direction);
                        if (projection > bestProjection)
                        {
                            result = pointB;
                            bestProjection = projection;
                        }

                        if (FixedPointVector3.Dot(pointC, direction) > bestProjection)
                        {
                            result = pointC;
                        }

                        return result;
                    }
                }
            }
        }

        private struct ConvexSupportPoint
        {
            internal FixedPointVector3 point;
            internal FixedPointVector3 pointOnA;
            internal FixedPointVector3 pointOnB;
        }

        private struct EpaFace
        {
            internal int a;
            internal int b;
            internal int c;
            internal FixedPointVector3 normal;
            internal FixedPoint64 distance;
        }

        private struct EpaEdge
        {
            internal int a;
            internal int b;
        }

        private const int EpaVertexCapacity = 64;
        private const int EpaFaceCapacity = 128;
        private const int EpaEdgeCapacity = 256;

        [System.ThreadStatic] private static ConvexSupportPoint[] s_simplexBuffer;
        [System.ThreadStatic] private static ConvexSupportPoint[] s_epaVertexBuffer;
        [System.ThreadStatic] private static EpaFace[] s_epaFaceBuffer;
        [System.ThreadStatic] private static EpaEdge[] s_epaEdgeBuffer;

        private static FPCollision IntersectConvexShapes(ConvexShape shapeA, ConvexShape shapeB)
        {
            var simplex = s_simplexBuffer ??= new ConvexSupportPoint[4];
            var simplexCount = 0;
            var direction = shapeA.center - shapeB.center;
            if (direction.IsZero()) direction = FixedPointVector3.right;

            AddSimplexPoint(simplex, ref simplexCount, GetConvexSupport(shapeA, shapeB, direction));
            direction = -simplex[0].point;

            for (var iteration = 0; iteration < 32; iteration++)
            {
                if (direction.IsZero())
                {
                    return BuildFallbackConvexCollision(shapeA, shapeB);
                }

                var support = GetConvexSupport(shapeA, shapeB, direction);
                if (FixedPointVector3.Dot(support.point, direction) < FixedPoint64.Zero)
                {
                    return default;
                }

                AddSimplexPoint(simplex, ref simplexCount, support);
                if (UpdateGjkSimplex(simplex, ref simplexCount, ref direction))
                {
                    return ExpandConvexPenetration(shapeA, shapeB, simplex);
                }
            }

            return default;
        }

        private static ConvexSupportPoint GetConvexSupport(
            ConvexShape shapeA,
            ConvexShape shapeB,
            FixedPointVector3 direction)
        {
            var pointOnA = shapeA.Support(-direction);
            var pointOnB = shapeB.Support(direction);
            return new ConvexSupportPoint
            {
                point = pointOnB - pointOnA,
                pointOnA = pointOnA,
                pointOnB = pointOnB
            };
        }

        private static void AddSimplexPoint(
            ConvexSupportPoint[] simplex,
            ref int count,
            ConvexSupportPoint point)
        {
            for (var i = System.Math.Min(count, 3); i > 0; i--)
            {
                simplex[i] = simplex[i - 1];
            }

            simplex[0] = point;
            count = System.Math.Min(count + 1, 4);
        }

        private static bool UpdateGjkSimplex(
            ConvexSupportPoint[] simplex,
            ref int count,
            ref FixedPointVector3 direction)
        {
            if (count == 2)
            {
                var ab = simplex[1].point - simplex[0].point;
                var ao = -simplex[0].point;
                if (FixedPointVector3.Dot(ab, ao) > FixedPoint64.Zero)
                {
                    direction = TripleCross(ab, ao, ab);
                }
                else
                {
                    count = 1;
                    direction = ao;
                }

                return false;
            }

            if (count == 3)
            {
                return UpdateGjkTriangle(simplex, ref count, ref direction);
            }

            if (count < 4)
            {
                direction = -simplex[0].point;
                return false;
            }

            var origin = FixedPointVector3.zero;
            if (TryReduceTetrahedronFace(simplex, 0, 1, 2, 3, origin, ref count, ref direction) ||
                TryReduceTetrahedronFace(simplex, 0, 2, 3, 1, origin, ref count, ref direction) ||
                TryReduceTetrahedronFace(simplex, 0, 3, 1, 2, origin, ref count, ref direction))
            {
                return false;
            }

            return true;
        }

        private static bool UpdateGjkTriangle(
            ConvexSupportPoint[] simplex,
            ref int count,
            ref FixedPointVector3 direction)
        {
            var a = simplex[0];
            var b = simplex[1];
            var c = simplex[2];
            var ab = b.point - a.point;
            var ac = c.point - a.point;
            var ao = -a.point;
            var abc = FixedPointVector3.Cross(ab, ac);

            if (FixedPointVector3.Dot(FixedPointVector3.Cross(abc, ac), ao) > FixedPoint64.Zero)
            {
                if (FixedPointVector3.Dot(ac, ao) > FixedPoint64.Zero)
                {
                    simplex[1] = c;
                    count = 2;
                    direction = TripleCross(ac, ao, ac);
                }
                else
                {
                    simplex[1] = b;
                    count = 2;
                    direction = FixedPointVector3.Dot(ab, ao) > FixedPoint64.Zero
                        ? TripleCross(ab, ao, ab)
                        : ao;
                    if (FixedPointVector3.Dot(ab, ao) <= FixedPoint64.Zero) count = 1;
                }

                return false;
            }

            if (FixedPointVector3.Dot(FixedPointVector3.Cross(ab, abc), ao) > FixedPoint64.Zero)
            {
                simplex[1] = b;
                count = 2;
                direction = FixedPointVector3.Dot(ab, ao) > FixedPoint64.Zero
                    ? TripleCross(ab, ao, ab)
                    : ao;
                if (FixedPointVector3.Dot(ab, ao) <= FixedPoint64.Zero) count = 1;
                return false;
            }

            if (FixedPointVector3.Dot(abc, ao) > FixedPoint64.Zero)
            {
                direction = abc;
            }
            else
            {
                simplex[1] = c;
                simplex[2] = b;
                direction = -abc;
            }

            return false;
        }

        private static bool TryReduceTetrahedronFace(
            ConvexSupportPoint[] simplex,
            int indexA,
            int indexB,
            int indexC,
            int oppositeIndex,
            FixedPointVector3 origin,
            ref int count,
            ref FixedPointVector3 direction)
        {
            var a = simplex[indexA];
            var b = simplex[indexB];
            var c = simplex[indexC];
            var normal = FixedPointVector3.Cross(b.point - a.point, c.point - a.point);
            if (FixedPointVector3.Dot(normal, simplex[oppositeIndex].point - a.point) > FixedPoint64.Zero)
            {
                normal = -normal;
            }

            if (FixedPointVector3.Dot(normal, origin - a.point) <= FixedPoint64.Zero)
            {
                return false;
            }

            simplex[0] = a;
            simplex[1] = b;
            simplex[2] = c;
            count = 3;
            direction = normal;
            return true;
        }

        private static FixedPointVector3 TripleCross(
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            var result = FixedPointVector3.Cross(FixedPointVector3.Cross(a, b), c);
            if (!result.IsZero()) return result;
            result = FixedPointVector3.Cross(a, FixedPointVector3.right);
            if (!result.IsZero()) return result;
            return FixedPointVector3.Cross(a, FixedPointVector3.up);
        }

        private static FPCollision ExpandConvexPenetration(
            ConvexShape shapeA,
            ConvexShape shapeB,
            ConvexSupportPoint[] simplex)
        {
            var vertices = s_epaVertexBuffer ??= new ConvexSupportPoint[EpaVertexCapacity];
            var faces = s_epaFaceBuffer ??= new EpaFace[EpaFaceCapacity];
            var edges = s_epaEdgeBuffer ??= new EpaEdge[EpaEdgeCapacity];
            for (var i = 0; i < 4; i++) vertices[i] = simplex[i];
            var vertexCount = 4;
            var faceCount = 0;
            AddEpaFace(vertices, faces, ref faceCount, 0, 1, 2);
            AddEpaFace(vertices, faces, ref faceCount, 0, 3, 1);
            AddEpaFace(vertices, faces, ref faceCount, 0, 2, 3);
            AddEpaFace(vertices, faces, ref faceCount, 1, 3, 2);

            for (var iteration = 0; iteration < 48 && faceCount > 0; iteration++)
            {
                var closestFaceIndex = 0;
                for (var i = 1; i < faceCount; i++)
                {
                    if (faces[i].distance < faces[closestFaceIndex].distance)
                    {
                        closestFaceIndex = i;
                    }
                }

                var closestFace = faces[closestFaceIndex];
                var support = GetConvexSupport(shapeA, shapeB, closestFace.normal);
                var supportDistance = FixedPointVector3.Dot(support.point, closestFace.normal);
                var duplicate = false;
                for (var i = 0; i < vertexCount; i++)
                {
                    if ((vertices[i].point - support.point).sqrMagnitude <= FixedPoint64.EN8)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate || supportDistance - closestFace.distance <= FixedPoint64.EN4)
                {
                    return BuildEpaCollision(vertices, closestFace);
                }

                if (vertexCount >= vertices.Length)
                {
                    return BuildEpaCollision(vertices, closestFace);
                }

                var newVertexIndex = vertexCount++;
                vertices[newVertexIndex] = support;
                var edgeCount = 0;
                for (var faceIndex = faceCount - 1; faceIndex >= 0; faceIndex--)
                {
                    var face = faces[faceIndex];
                    if (FixedPointVector3.Dot(
                            face.normal, support.point - vertices[face.a].point) <= FixedPoint64.EN5)
                    {
                        continue;
                    }

                    AddEpaBoundaryEdge(edges, ref edgeCount, face.a, face.b);
                    AddEpaBoundaryEdge(edges, ref edgeCount, face.b, face.c);
                    AddEpaBoundaryEdge(edges, ref edgeCount, face.c, face.a);
                    faces[faceIndex] = faces[--faceCount];
                }

                for (var edgeIndex = 0; edgeIndex < edgeCount; edgeIndex++)
                {
                    var edge = edges[edgeIndex];
                    if (!AddEpaFace(vertices, faces, ref faceCount,
                            edge.a, edge.b, newVertexIndex))
                    {
                        return BuildEpaCollision(vertices, closestFace);
                    }
                }
            }

            return BuildFallbackConvexCollision(shapeA, shapeB);
        }

        private static bool AddEpaFace(
            ConvexSupportPoint[] vertices,
            EpaFace[] faces,
            ref int faceCount,
            int a,
            int b,
            int c)
        {
            var normal = FixedPointVector3.Cross(
                vertices[b].point - vertices[a].point,
                vertices[c].point - vertices[a].point);
            if (normal.IsZero()) return true;
            normal = normal.normalized;
            var distance = FixedPointVector3.Dot(normal, vertices[a].point);
            if (distance < FixedPoint64.Zero)
            {
                (b, c) = (c, b);
                normal = -normal;
                distance = -distance;
            }

            if (faceCount >= faces.Length) return false;
            faces[faceCount++] = new EpaFace
            {
                a = a,
                b = b,
                c = c,
                normal = normal,
                distance = distance
            };
            return true;
        }

        private static void AddEpaBoundaryEdge(EpaEdge[] edges, ref int edgeCount, int a, int b)
        {
            for (var i = 0; i < edgeCount; i++)
            {
                if (edges[i].a == b && edges[i].b == a)
                {
                    for (var moveIndex = i; moveIndex < edgeCount - 1; moveIndex++)
                    {
                        edges[moveIndex] = edges[moveIndex + 1];
                    }

                    edgeCount--;
                    return;
                }
            }

            if (edgeCount < edges.Length)
            {
                edges[edgeCount++] = new EpaEdge { a = a, b = b };
            }
        }

        private static FPCollision BuildEpaCollision(
            ConvexSupportPoint[] vertices,
            EpaFace face)
        {
            var a = vertices[face.a];
            var b = vertices[face.b];
            var c = vertices[face.c];
            var target = face.normal * face.distance;
            GetTriangleBarycentric(target, a.point, b.point, c.point,
                out var weightA, out var weightB, out var weightC);
            var pointOnA = a.pointOnA * weightA + b.pointOnA * weightB + c.pointOnA * weightC;
            var pointOnB = a.pointOnB * weightA + b.pointOnB * weightB + c.pointOnB * weightC;
            return new FPCollision
            {
                hit = true,
                normal = face.normal,
                closestPoint = pointOnB,
                outsidePoint = pointOnA,
                contactPoint = (pointOnA + pointOnB) * FixedPoint64.Half,
                t = face.distance,
                depth = face.distance * FixedPoint64.Half
            };
        }

        private static void GetTriangleBarycentric(
            FixedPointVector3 point,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c,
            out FixedPoint64 weightA,
            out FixedPoint64 weightB,
            out FixedPoint64 weightC)
        {
            var v0 = b - a;
            var v1 = c - a;
            var v2 = point - a;
            var d00 = FixedPointVector3.Dot(v0, v0);
            var d01 = FixedPointVector3.Dot(v0, v1);
            var d11 = FixedPointVector3.Dot(v1, v1);
            var d20 = FixedPointVector3.Dot(v2, v0);
            var d21 = FixedPointVector3.Dot(v2, v1);
            var denominator = d00 * d11 - d01 * d01;
            if (denominator == FixedPoint64.Zero)
            {
                weightA = FixedPoint64.One / 3;
                weightB = weightA;
                weightC = weightA;
                return;
            }

            weightB = (d11 * d20 - d01 * d21) / denominator;
            weightC = (d00 * d21 - d01 * d20) / denominator;
            weightA = FixedPoint64.One - weightB - weightC;
        }

        private static FPCollision BuildFallbackConvexCollision(
            ConvexShape shapeA,
            ConvexShape shapeB)
        {
            var normal = shapeA.center - shapeB.center;
            if (normal.IsZero()) normal = FixedPointVector3.right;
            normal = normal.normalized;
            var pointOnA = shapeA.Support(-normal);
            var pointOnB = shapeB.Support(normal);
            var penetration = FixedPointMath.Max(
                FixedPoint64.Zero,
                FixedPointVector3.Dot(pointOnB - pointOnA, normal));
            return new FPCollision
            {
                hit = true,
                normal = normal,
                closestPoint = pointOnB,
                outsidePoint = pointOnA,
                contactPoint = (pointOnA + pointOnB) * FixedPoint64.Half,
                t = penetration,
                depth = penetration * FixedPoint64.Half
            };
        }
    }
}
