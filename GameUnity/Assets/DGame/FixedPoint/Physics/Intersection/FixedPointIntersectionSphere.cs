using System;
using System.Runtime.CompilerServices;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供球体相关的包含、重叠和接触信息查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>判断两个球体是否重叠或相切。</summary>
        /// <param name="point">球体 A 的球心。</param>
        /// <param name="radius">球体 A 的半径。</param>
        /// <param name="point1">球体 B 的球心。</param>
        /// <param name="radius1">球体 B 的半径。</param>
        /// <returns>球心距离不大于半径之和时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIntersectWithSphereAndSphere(
            FixedPointVector3 point,
            FixedPoint64 radius,
            FixedPointVector3 point1,
            FixedPoint64 radius1)
        {
            var radiusSum = radius + radius1;
            return (point - point1).sqrMagnitude <= radiusSum * radiusSum;
        }

        /// <summary>判断球体是否完全位于轴对齐包围盒内部。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>球体可以接触盒面，但不能越出任意盒面时返回 <see langword="true"/>。</returns>
        public static bool IsSphereInsideAABB(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            return center.x - min.x >= radius && max.x - center.x >= radius &&
                   center.y - min.y >= radius && max.y - center.y >= radius &&
                   center.z - min.z >= radius && max.z - center.z >= radius;
        }

        /// <summary>判断球体与轴对齐包围盒是否重叠或相切。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>球心到包围盒的平方距离不大于半径平方时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsIntersectWithSphereAndAABB(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            var closestPoint = ClosestPointWithPointAndAABB(center, min, max);
            return (closestPoint - center).sqrMagnitude <= radius * radius;
        }

        /// <summary>检测球体与轴对齐包围盒的重叠并计算接触信息。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>从包围盒指向球体的单位法线、表面点和半穿透深度。</returns>
        public static FPCollision IntersectWithSphereAndAABB(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            var collision = new FPCollision();
            var closestPoint = ClosestPointWithPointAndAABB(center, min, max);
            var separation = center - closestPoint;
            var distanceSquared = separation.sqrMagnitude;
            FixedPointVector3 normal;
            FixedPoint64 penetration;

            if (distanceSquared > FixedPoint64.Zero)
            {
                if (distanceSquared > radius * radius)
                {
                    return collision;
                }

                var distance = FixedPointMath.Sqrt(distanceSquared);
                normal = separation / distance;
                penetration = FixedPointMath.Max(FixedPoint64.Zero, radius - distance);
            }
            else
            {
                // 球心位于盒内时，普通 Clamp 会返回球心；必须改选最近盒面作为分离方向。
                GetNearestAABBSurface(center, min, max, out closestPoint, out normal, out var distanceToSurface);
                penetration = radius + distanceToSurface;
            }

            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = closestPoint;
            collision.outsidePoint = center - normal * radius;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
#if UNITY_EDITOR
            collision.debugInfo = (min + max) * FixedPoint64.Half;
            collision.debugInfo1 = closestPoint;
#endif
            return collision;
        }

        /// <summary>检测球碰撞器与 OBB 碰撞器的重叠。</summary>
        /// <param name="fpSphere">球碰撞器。</param>
        /// <param name="obb">OBB 碰撞器。</param>
        /// <returns>从 OBB 指向球体的接触信息。</returns>
        public static FPCollision IntersectWithSphereAndOBB(FPSphereCollider fpSphere, FPBoxCollider obb)
        {
            return IntersectWithSphereAndOBB(
                fpSphere.position,
                fpSphere.radius,
                obb.position,
                obb.halfSize,
                obb.fpTransform.fixedPointMatrix);
        }

        /// <summary>检测球体与有向包围盒的重叠并计算接触信息。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="orientation">包含三个单位局部轴的旋转矩阵。</param>
        /// <returns>从有向包围盒指向球体的单位法线、表面点和半穿透深度。</returns>
        public static FPCollision IntersectWithSphereAndOBB(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation)
        {
            var collision = new FPCollision();
            var closestPoint = ClosestPointWithPointAndOBB(center, position, halfSize, orientation);
            var separation = center - closestPoint;
            var distanceSquared = separation.sqrMagnitude;
            FixedPointVector3 normal;
            FixedPoint64 penetration;

            if (distanceSquared > FixedPoint64.Zero)
            {
                if (distanceSquared > radius * radius)
                {
                    return collision;
                }

                var distance = FixedPointMath.Sqrt(distanceSquared);
                normal = separation / distance;
                penetration = FixedPointMath.Max(FixedPoint64.Zero, radius - distance);
            }
            else
            {
                GetNearestOBBSurface(
                    center,
                    position,
                    halfSize,
                    orientation,
                    out closestPoint,
                    out normal,
                    out var distanceToSurface);
                penetration = radius + distanceToSurface;
            }

            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = closestPoint;
            collision.outsidePoint = center - normal * radius;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }

        /// <summary>检测两个球体的重叠并计算接触信息。</summary>
        /// <param name="point">球体 A 的球心。</param>
        /// <param name="radius">球体 A 的半径。</param>
        /// <param name="target">球体 B 的球心。</param>
        /// <param name="targetRadius">球体 B 的半径。</param>
        /// <returns>从球体 B 指向球体 A 的接触信息。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FPCollision IntersectWithSphereAndSphere(
            FixedPointVector3 point,
            FixedPoint64 radius,
            FixedPointVector3 target,
            FixedPoint64 targetRadius)
        {
            var collision = new FPCollision();
            var offset = point - target;
            var distanceSquared = offset.sqrMagnitude;
            var radiusSum = radius + targetRadius;

            if (distanceSquared > radiusSum * radiusSum)
            {
                return collision;
            }

            var distance = FixedPointMath.Sqrt(distanceSquared);
            var normal = distance > FixedPoint64.Zero ? offset / distance : FixedPointVector3.up;
            var penetration = FixedPointMath.Max(FixedPoint64.Zero, radiusSum - distance);

            collision.hit = true;
            collision.normal = normal;
            collision.closestPoint = point - normal * radius;
            collision.outsidePoint = target + normal * targetRadius;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.t = penetration;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }

        /// <summary>检测球体与网格碰撞器各三角形的重叠。</summary>
        /// <param name="point">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="fpMeshCollider">待检测的网格碰撞器。</param>
        /// <param name="onAction">可选回调；每次检测到三角形平面投影点时调用。</param>
        /// <returns>合并各三角形最小分离向量后的碰撞信息。</returns>
        public static FPCollision IntersectWithSphereAndMesh(
            FixedPointVector3 point,
            FixedPoint64 radius,
            FPMeshCollider fpMeshCollider,
            Action<FixedPointVector3> onAction = null)
        {
            var constraint = FixedPointVector3.zero;
            var hasHit = false;
            var fallbackNormal = FixedPointVector3.zero;
            var meshPosition = fpMeshCollider.position;
            var radiusVector = new FixedPointVector3(radius, radius, radius);
            var min = point - radiusVector;
            var max = point + radiusVector;

            for (var triangleOffset = 0; triangleOffset < fpMeshCollider.triangles.Length; triangleOffset += 3)
            {
                var triangleIndex = triangleOffset / 3;

                if (!IntersectWithAABBAndAABBFixedPoint(
                        min,
                        max,
                        fpMeshCollider.minimals[triangleIndex] + meshPosition,
                        fpMeshCollider.maximals[triangleIndex] + meshPosition))
                {
                    continue;
                }

                var collision = IntersectWithSphereAndTriangle(
                    point,
                    radius,
                    fpMeshCollider.vertices[fpMeshCollider.triangles[triangleOffset]] + meshPosition,
                    fpMeshCollider.vertices[fpMeshCollider.triangles[triangleOffset + 1]] + meshPosition,
                    fpMeshCollider.vertices[fpMeshCollider.triangles[triangleOffset + 2]] + meshPosition,
                    fpMeshCollider.normals[triangleIndex],
                    fpMeshCollider.distances[triangleIndex],
                    onAction);

                if (collision.hit)
                {
                    hasHit = true;
                    fallbackNormal = collision.normal;
                    AddConstraints(ref constraint, collision.normal * (collision.depth * 2));
                }
            }

            var result = new FPCollision();

            if (!hasHit)
            {
                return result;
            }

            result.hit = true;
            result.collider = fpMeshCollider;
            result.normal = constraint.IsZero() ? fallbackNormal : constraint.normalized;
            result.depth = constraint.magnitude * FixedPoint64.Half;
            result.closestPoint = point - result.normal * (radius - result.depth);
            result.outsidePoint = point - result.normal * radius;
            result.contactPoint = (result.closestPoint + result.outsidePoint) * FixedPoint64.Half;
            return result;
        }

        /// <summary>把新的最小分离向量合并到当前约束中。</summary>
        private static void AddConstraints(
            ref FixedPointVector3 constraint,
            FixedPointVector3 additionalConstraint)
        {
            if (constraint.IsZero())
            {
                constraint = additionalConstraint;
                return;
            }

            var constraintNormal = constraint.normalized;
            var magnitude = constraint.magnitude;
            var projection = FixedPointVector3.Dot(additionalConstraint, constraintNormal);

            if (projection > magnitude)
            {
                constraint = additionalConstraint;
            }
            else if (projection > FixedPoint64.Zero)
            {
                constraint += additionalConstraint - constraintNormal * projection;
            }
            else
            {
                constraint += additionalConstraint;
            }
        }

        /// <summary>检测球体与三角形的重叠并计算最近点。</summary>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="point">三角形顶点 A。</param>
        /// <param name="point1">三角形顶点 B。</param>
        /// <param name="point2">三角形顶点 C。</param>
        /// <param name="normal">三角形的单位法线。</param>
        /// <param name="normalDistance">三角形平面到原点的有符号距离。</param>
        /// <param name="onAction">可选回调；平面投影点落在三角形内部时调用。</param>
        /// <returns>从三角形指向球体的单位法线、最近点和半穿透深度。</returns>
        public static FPCollision IntersectWithSphereAndTriangle(
            FixedPointVector3 center,
            FixedPoint64 radius,
            FixedPointVector3 point,
            FixedPointVector3 point1,
            FixedPointVector3 point2,
            FixedPointVector3 normal,
            FixedPoint64 normalDistance,
            Action<FixedPointVector3> onAction = null)
        {
            var collision = new FPCollision();

            if (normal.IsZero())
            {
                return collision;
            }

            var projectedPoint = ClosestPointWithPointAndPlane(center, normalDistance, normal);
            var projectionInsideTriangle = PointInTriangle(projectedPoint, point, point1, point2);

            if (projectionInsideTriangle)
            {
                onAction?.Invoke(projectedPoint);
            }

            var closestPoint = projectionInsideTriangle
                ? projectedPoint
                : ClosestPointWithPointAndTriangle(
                    center,
                    point,
                    point1,
                    point2,
                    normal,
                    normalDistance,
                    false);
            var separation = center - closestPoint;
            var distanceSquared = separation.sqrMagnitude;

            if (distanceSquared > radius * radius)
            {
                return collision;
            }

            var distance = FixedPointMath.Sqrt(distanceSquared);
            var unitNormal = normal.normalized;
            var signedDistance = FixedPointVector3.Dot(center, unitNormal) -
                                 FixedPointVector3.Dot(point, unitNormal);
            var collisionNormal = distance > FixedPoint64.Zero
                ? separation / distance
                : signedDistance < FixedPoint64.Zero
                    ? -unitNormal
                    : unitNormal;
            var penetration = FixedPointMath.Max(FixedPoint64.Zero, radius - distance);

            collision.hit = true;
            collision.normal = collisionNormal;
            collision.closestPoint = closestPoint;
            collision.outsidePoint = center - collisionNormal * radius;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = penetration * FixedPoint64.Half;
            return collision;
        }

        /// <summary>检测球体与双面无限平面的重叠。</summary>
        /// <param name="point">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="plane">待检测平面。</param>
        /// <returns>从平面指向球体的接触信息。</returns>
        public static FPCollision IntersectWithSphereAndPlane(
            FixedPointVector3 point,
            FixedPoint64 radius,
            FixedPointPlane plane)
        {
            return IntersectWithSphereAndPlane(point, radius, plane.distance, plane.normal);
        }

        /// <summary>检测球体与双面无限平面的重叠。</summary>
        /// <param name="point">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="planeDistance">平面方程中的有符号距离。</param>
        /// <param name="planeNormal">平面的非零法线。</param>
        /// <returns>从平面指向球体的接触信息。</returns>
        public static FPCollision IntersectWithSphereAndPlane(
            FixedPointVector3 point,
            FixedPoint64 radius,
            FixedPoint64 planeDistance,
            FixedPointVector3 planeNormal)
        {
            var collision = new FPCollision();

            if (planeNormal.IsZero())
            {
                return collision;
            }

            var unitNormal = planeNormal.normalized;
            var normalizedDistance = planeDistance / planeNormal.magnitude;
            var signedDistance = FixedPointVector3.Dot(point, unitNormal) - normalizedDistance;
            var absoluteDistance = FixedPointMath.Abs(signedDistance);

            if (absoluteDistance > radius)
            {
                return collision;
            }

            var collisionNormal = signedDistance < FixedPoint64.Zero ? -unitNormal : unitNormal;
            var closestPoint = point - unitNormal * signedDistance;
            var outsidePoint = point - collisionNormal * radius;

            collision.hit = true;
            collision.normal = collisionNormal;
            collision.closestPoint = closestPoint;
            collision.outsidePoint = outsidePoint;
            collision.contactPoint = (closestPoint + outsidePoint) * FixedPoint64.Half;
            collision.depth = FixedPointMath.Max(FixedPoint64.Zero, radius - absoluteDistance) * FixedPoint64.Half;
            return collision;
        }

        /// <summary>检测球体与指定平面内的实心圆盘是否重叠。</summary>
        /// <param name="centerSphere">球心。</param>
        /// <param name="radiusSphere">球半径。</param>
        /// <param name="centerCircle">圆盘中心。</param>
        /// <param name="normalCircle">圆盘的非零法线。</param>
        /// <param name="radiusCircle">圆盘半径。</param>
        /// <returns>从圆盘指向球体的接触信息。</returns>
        public static FPCollision IntersectWithSphereAndCircle(
            FixedPointVector3 centerSphere,
            FixedPoint64 radiusSphere,
            FixedPointVector3 centerCircle,
            FixedPointVector3 normalCircle,
            FixedPoint64 radiusCircle)
        {
            var collision = new FPCollision();

            if (normalCircle.IsZero())
            {
                return collision;
            }

            var unitNormal = normalCircle.normalized;
            var signedPlaneDistance = FixedPointVector3.Dot(centerSphere - centerCircle, unitNormal);
            var projectedPoint = centerSphere - unitNormal * signedPlaneDistance;
            var radialOffset = projectedPoint - centerCircle;
            var radialDistance = radialOffset.magnitude;
            var pointOnDisk = radialDistance <= radiusCircle || radialDistance == FixedPoint64.Zero
                ? projectedPoint
                : centerCircle + radialOffset * (radiusCircle / radialDistance);
            var separation = centerSphere - pointOnDisk;
            var distance = separation.magnitude;

            if (distance > radiusSphere)
            {
                return collision;
            }

            var collisionNormal = distance > FixedPoint64.Zero
                ? separation / distance
                : signedPlaneDistance < FixedPoint64.Zero
                    ? -unitNormal
                    : unitNormal;

            collision.hit = true;
            collision.normal = collisionNormal;
            collision.closestPoint = pointOnDisk;
            collision.outsidePoint = centerSphere - collisionNormal * radiusSphere;
            collision.contactPoint = (collision.closestPoint + collision.outsidePoint) * FixedPoint64.Half;
            collision.depth = FixedPointMath.Max(FixedPoint64.Zero, radiusSphere - distance) * FixedPoint64.Half;
            return collision;
        }

        private static void GetNearestAABBSurface(
            FixedPointVector3 point,
            FixedPointVector3 min,
            FixedPointVector3 max,
            out FixedPointVector3 surfacePoint,
            out FixedPointVector3 normal,
            out FixedPoint64 distance)
        {
            distance = point.x - min.x;
            normal = FixedPointVector3.left;
            surfacePoint = new FixedPointVector3(min.x, point.y, point.z);
            SelectNearerSurface(max.x - point.x, FixedPointVector3.right,
                new FixedPointVector3(max.x, point.y, point.z), ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(point.y - min.y, FixedPointVector3.down,
                new FixedPointVector3(point.x, min.y, point.z), ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(max.y - point.y, FixedPointVector3.up,
                new FixedPointVector3(point.x, max.y, point.z), ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(point.z - min.z, FixedPointVector3.back,
                new FixedPointVector3(point.x, point.y, min.z), ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(max.z - point.z, FixedPointVector3.forward,
                new FixedPointVector3(point.x, point.y, max.z), ref distance, ref normal, ref surfacePoint);
        }

        private static void GetNearestOBBSurface(
            FixedPointVector3 point,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            out FixedPointVector3 surfacePoint,
            out FixedPointVector3 normal,
            out FixedPoint64 distance)
        {
            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            var offset = point - position;
            var local = new FixedPointVector3(
                FixedPointVector3.Dot(offset, axisX),
                FixedPointVector3.Dot(offset, axisY),
                FixedPointVector3.Dot(offset, axisZ));

            distance = local.x + halfSize.x;
            normal = -axisX;
            surfacePoint = point - axisX * distance;
            SelectNearerSurface(halfSize.x - local.x, axisX, point + axisX * (halfSize.x - local.x),
                ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(local.y + halfSize.y, -axisY, point - axisY * (local.y + halfSize.y),
                ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(halfSize.y - local.y, axisY, point + axisY * (halfSize.y - local.y),
                ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(local.z + halfSize.z, -axisZ, point - axisZ * (local.z + halfSize.z),
                ref distance, ref normal, ref surfacePoint);
            SelectNearerSurface(halfSize.z - local.z, axisZ, point + axisZ * (halfSize.z - local.z),
                ref distance, ref normal, ref surfacePoint);
        }

        private static void SelectNearerSurface(
            FixedPoint64 candidateDistance,
            FixedPointVector3 candidateNormal,
            FixedPointVector3 candidatePoint,
            ref FixedPoint64 distance,
            ref FixedPointVector3 normal,
            ref FixedPointVector3 surfacePoint)
        {
            if (candidateDistance >= distance)
            {
                return;
            }

            distance = candidateDistance;
            normal = candidateNormal;
            surfacePoint = candidatePoint;
        }
    }
}