using System;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供射线与线段、平面、三角形及体积几何体的相交查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>获取有限射线上距离指定线段最近的位置。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="length">射线的最大长度。</param>
        /// <param name="start">目标线段起点。</param>
        /// <param name="end">目标线段终点。</param>
        /// <returns>有限射线线段上的最近点；方向为零或长度非正时返回起点。</returns>
        public static FixedPointVector3 ClosestPointWithRayAndLinesegment(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 start,
            FixedPointVector3 end)
        {
            if (direct.IsZero() || length <= FixedPoint64.Zero)
            {
                return origin;
            }

            var rayEnd = origin + direct.normalized * length;
            ClosestPointsOnLineSegments(origin, rayEnd, start, end, out var pointOnRay, out _);
            return pointOnRay;
        }

        /// <summary>检测双面无限平面与射线的相交。</summary>
        /// <param name="point">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="planeDistance">平面方程中的有符号距离。</param>
        /// <param name="planeNormal">平面的非零法线。</param>
        /// <param name="intersection">命中时返回交点、射线参数和朝向射线的单位法线。</param>
        /// <returns>交点位于射线正向半轴时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithRayAndPlaneFixedPoint(
            FixedPointVector3 point,
            FixedPointVector3 direct,
            FixedPoint64 planeDistance,
            FixedPointVector3 planeNormal,
            out FPCollision intersection)
        {
            intersection = new FPCollision();
            var denominator = FixedPointVector3.Dot(direct, planeNormal);

            if (denominator == FixedPoint64.Zero)
            {
                return false;
            }

            var parameter = (planeDistance - FixedPointVector3.Dot(point, planeNormal)) / denominator;

            if (parameter < FixedPoint64.Zero)
            {
                return false;
            }

            var unitNormal = planeNormal.normalized;
            intersection.hit = true;
            intersection.t = parameter;
            intersection.normal = denominator < FixedPoint64.Zero ? unitNormal : -unitNormal;
            intersection.closestPoint = point + direct * parameter;
            return true;
        }

        /// <summary>检测射线与三角形的双面相交。</summary>
        /// <param name="point">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="center">三角形局部顶点的世界平移量。</param>
        /// <param name="a">三角形局部顶点 A。</param>
        /// <param name="b">三角形局部顶点 B。</param>
        /// <param name="c">三角形局部顶点 C。</param>
        /// <param name="intersection">命中时返回交点、射线参数和朝向射线的单位法线。</param>
        /// <returns>射线在正向半轴命中三角形内部或边界时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithRayAndTriangleFixedPoint(
            FixedPointVector3 point,
            FixedPointVector3 direct,
            FixedPointVector3 center,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c,
            out FPCollision intersection)
        {
            var plane = FromTriangle(center, a, b, c);

            if (plane.normal.IsZero() ||
                !IntersectWithRayAndPlaneFixedPoint(
                    point,
                    direct,
                    plane.distance,
                    plane.normal,
                    out intersection))
            {
                intersection = new FPCollision();
                return false;
            }

            if (PointInTriangle(intersection.closestPoint, a + center, b + center, c + center))
            {
                return true;
            }

            intersection = new FPCollision();
            return false;
        }

        /// <summary>检测有限射线与轴对齐包围盒的相交。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="length">射线的最大检测距离。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <param name="intersection">命中时返回交点、距离和表面法线。</param>
        /// <returns>有限射线命中包围盒时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithRayAndAABBFixedPointA(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 min,
            FixedPointVector3 max,
            out FPCollision intersection)
        {
            intersection = new FPCollision();

            if (direct.IsZero() || length < FixedPoint64.Zero)
            {
                return false;
            }

            var normalizedDirection = direct.normalized;

            if (!TryIntersectLineWithAABB(
                    origin,
                    normalizedDirection,
                    min,
                    max,
                    out var entryParameter,
                    out var exitParameter,
                    out var entryNormal,
                    out var exitNormal) ||
                exitParameter < FixedPoint64.Zero)
            {
                return false;
            }

            var startsInside = entryParameter < FixedPoint64.Zero;
            var hitParameter = startsInside ? exitParameter : entryParameter;

            if (hitParameter > length)
            {
                return false;
            }

            intersection.hit = true;
            intersection.t = hitParameter;
            intersection.normal = startsInside ? exitNormal : entryNormal;
            intersection.closestPoint = origin + normalizedDirection * hitParameter;
            intersection.outsidePoint = origin + normalizedDirection * (startsInside ? entryParameter : exitParameter);
            return true;
        }

        /// <summary>检测位移线段与轴对齐包围盒的首次进入相交。</summary>
        /// <param name="origin">位移起点。</param>
        /// <param name="delta">完整位移向量，对应参数区间为 <c>[0, 1]</c>。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <param name="intersection">命中时返回进入点、参数和表面法线。</param>
        /// <returns>首次进入参数；未命中或起点已在盒内时返回 <see cref="FixedPoint64.MaxValue"/>。</returns>
        public static FixedPoint64 IntersectWithRayAndAABBFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 delta,
            FixedPointVector3 min,
            FixedPointVector3 max,
            out FPCollision intersection)
        {
            intersection = new FPCollision();

            if (PointInAABB(origin, min, max) || delta.IsZero() ||
                !TryIntersectLineWithAABB(
                    origin,
                    delta,
                    min,
                    max,
                    out var entryParameter,
                    out var exitParameter,
                    out var entryNormal,
                    out _))
            {
                return FixedPoint64.MaxValue;
            }

            if (exitParameter < FixedPoint64.Zero || entryParameter < FixedPoint64.Zero ||
                entryParameter > FixedPoint64.One)
            {
                return FixedPoint64.MaxValue;
            }

            intersection.hit = true;
            intersection.t = entryParameter;
            intersection.normal = entryNormal;
            intersection.closestPoint = origin + delta * entryParameter;
            return entryParameter;
        }

        /// <summary>检测有限射线与有向包围盒的相交。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="length">射线的最大检测距离。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="orientation">包含三个单位局部轴的旋转矩阵。</param>
        /// <param name="intersection">命中时返回交点、距离和世界坐标法线。</param>
        /// <returns>命中距离；未命中时返回 <c>-1</c>。</returns>
        public static FixedPoint64 IntersectWithRayAndOBBFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            out FPCollision intersection)
        {
            intersection = new FPCollision();

            if (direct.IsZero() || length < FixedPoint64.Zero)
            {
                return -1;
            }

            var distance = IntersectWithRayAndOBBFixedPoint(
                origin,
                direct.normalized,
                position,
                halfSize,
                orientation,
                out intersection);

            if (!intersection.hit || distance > length)
            {
                intersection = new FPCollision();
                return -1;
            }

            return distance;
        }

        /// <summary>检测射线与有向包围盒的相交。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向；返回参数按此向量计量。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="orientation">包含三个单位局部轴的旋转矩阵。</param>
        /// <param name="intersection">命中时返回交点、参数、另一个交点和世界坐标法线。</param>
        /// <returns>射线首次命中参数；起点在盒内时返回离开参数；未命中时返回 <c>-1</c>。</returns>
        public static FixedPoint64 IntersectWithRayAndOBBFixedPoint(
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

            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            var originOffset = origin - position;
            var localOrigin = new FixedPointVector3(
                FixedPointVector3.Dot(originOffset, axisX),
                FixedPointVector3.Dot(originOffset, axisY),
                FixedPointVector3.Dot(originOffset, axisZ));
            var localDirection = new FixedPointVector3(
                FixedPointVector3.Dot(direct, axisX),
                FixedPointVector3.Dot(direct, axisY),
                FixedPointVector3.Dot(direct, axisZ));

            if (!TryIntersectLineWithAABB(
                    localOrigin,
                    localDirection,
                    -halfSize,
                    halfSize,
                    out var entryParameter,
                    out var exitParameter,
                    out var localEntryNormal,
                    out var localExitNormal) ||
                exitParameter < FixedPoint64.Zero)
            {
                return -1;
            }

            var startsInside = entryParameter < FixedPoint64.Zero;
            var hitParameter = startsInside ? exitParameter : entryParameter;
            var otherParameter = startsInside ? entryParameter : exitParameter;
            var localNormal = startsInside ? localExitNormal : localEntryNormal;

            intersection.hit = true;
            intersection.t = hitParameter;
            intersection.closestPoint = origin + direct * hitParameter;
            intersection.outsidePoint = origin + direct * otherParameter;
            intersection.normal = axisX * localNormal.x + axisY * localNormal.y + axisZ * localNormal.z;
            return hitParameter;
        }

        /// <summary>检测有限射线与 AABB 按指定半径扩张后的精确圆角体相交。</summary>
        /// <param name="origin">扫描球心起点。</param>
        /// <param name="direct">扫描方向，无须归一化。</param>
        /// <param name="length">最大扫描距离。</param>
        /// <param name="min">AABB 最小点。</param>
        /// <param name="max">AABB 最大点。</param>
        /// <param name="radius">圆角半径，即扫描球半径。</param>
        /// <returns>命中时返回扫描球心在首次接触时的位置、距离及由盒体指向球心的法线。</returns>
        public static FPCollision IntersectWithRayAndRoundedAABB(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPoint64 radius)
        {
            var collision = new FPCollision();

            if (direct.IsZero() || length < FixedPoint64.Zero || radius < FixedPoint64.Zero)
            {
                return collision;
            }

            var initialOverlap = IntersectWithSphereAndAABB(origin, radius, min, max);

            if (initialOverlap.hit)
            {
                initialOverlap.t = FixedPoint64.Zero;
                initialOverlap.closestPoint = origin;
                return initialOverlap;
            }

            var direction = direct.normalized;
            var bestDistance = FixedPoint64.MaxValue;
            var bestNormal = FixedPointVector3.zero;

            // 六个平面负责圆角盒的平直面区域。
            TrySelectRoundedAABBFace(origin, direction, length, min, max, min.x - radius, 0, false,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBFace(origin, direction, length, min, max, max.x + radius, 0, true,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBFace(origin, direction, length, min, max, min.y - radius, 1, false,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBFace(origin, direction, length, min, max, max.y + radius, 1, true,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBFace(origin, direction, length, min, max, min.z - radius, 2, false,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBFace(origin, direction, length, min, max, max.z + radius, 2, true,
                ref bestDistance, ref bestNormal);

            // 十二条边按胶囊体检测；胶囊端帽同时精确覆盖八个圆角顶点。
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, min.y, min.z), new FixedPointVector3(max.x, min.y, min.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, min.y, max.z), new FixedPointVector3(max.x, min.y, max.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, max.y, min.z), new FixedPointVector3(max.x, max.y, min.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, max.y, max.z), new FixedPointVector3(max.x, max.y, max.z), radius,
                ref bestDistance, ref bestNormal);

            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, min.y, min.z), new FixedPointVector3(min.x, max.y, min.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, min.y, max.z), new FixedPointVector3(min.x, max.y, max.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(max.x, min.y, min.z), new FixedPointVector3(max.x, max.y, min.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(max.x, min.y, max.z), new FixedPointVector3(max.x, max.y, max.z), radius,
                ref bestDistance, ref bestNormal);

            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, min.y, min.z), new FixedPointVector3(min.x, min.y, max.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(min.x, max.y, min.z), new FixedPointVector3(min.x, max.y, max.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(max.x, min.y, min.z), new FixedPointVector3(max.x, min.y, max.z), radius,
                ref bestDistance, ref bestNormal);
            TrySelectRoundedAABBEdge(origin, direction, length,
                new FixedPointVector3(max.x, max.y, min.z), new FixedPointVector3(max.x, max.y, max.z), radius,
                ref bestDistance, ref bestNormal);

            if (bestDistance == FixedPoint64.MaxValue)
            {
                return collision;
            }

            collision.hit = true;
            collision.t = bestDistance;
            collision.closestPoint = origin + direction * bestDistance;
            collision.normal = bestNormal;
            return collision;
        }

        /// <summary>检测有限射线与 OBB 按指定半径扩张后的精确圆角体相交。</summary>
        public static FPCollision IntersectWithRayAndRoundedOBB(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix orientation,
            FixedPoint64 radius)
        {
            var collision = new FPCollision();

            if (direct.IsZero() || length < FixedPoint64.Zero || radius < FixedPoint64.Zero)
            {
                return collision;
            }

            var direction = direct.normalized;
            var axisX = new FixedPointVector3(orientation.M11, orientation.M12, orientation.M13);
            var axisY = new FixedPointVector3(orientation.M21, orientation.M22, orientation.M23);
            var axisZ = new FixedPointVector3(orientation.M31, orientation.M32, orientation.M33);
            var offset = origin - position;
            var localOrigin = new FixedPointVector3(
                FixedPointVector3.Dot(offset, axisX),
                FixedPointVector3.Dot(offset, axisY),
                FixedPointVector3.Dot(offset, axisZ));
            var localDirection = new FixedPointVector3(
                FixedPointVector3.Dot(direction, axisX),
                FixedPointVector3.Dot(direction, axisY),
                FixedPointVector3.Dot(direction, axisZ));
            var localCollision = IntersectWithRayAndRoundedAABB(
                localOrigin,
                localDirection,
                length,
                -halfSize,
                halfSize,
                radius);

            if (!localCollision.hit)
            {
                return collision;
            }

            collision.hit = true;
            collision.t = localCollision.t;
            collision.closestPoint = origin + direction * localCollision.t;
            collision.normal = (axisX * localCollision.normal.x +
                                axisY * localCollision.normal.y +
                                axisZ * localCollision.normal.z).normalized;
            return collision;
        }

        private static void TrySelectRoundedAABBFace(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPoint64 plane,
            int axis,
            bool positive,
            ref FixedPoint64 bestDistance,
            ref FixedPointVector3 bestNormal)
        {
            var directionComponent = axis == 0 ? direction.x : axis == 1 ? direction.y : direction.z;

            if (directionComponent == FixedPoint64.Zero)
            {
                return;
            }

            var originComponent = axis == 0 ? origin.x : axis == 1 ? origin.y : origin.z;
            var distance = (plane - originComponent) / directionComponent;

            if (distance < FixedPoint64.Zero || distance > length || distance >= bestDistance)
            {
                return;
            }

            var point = origin + direction * distance;
            var insideFace = axis == 0
                ? point.y >= min.y && point.y <= max.y && point.z >= min.z && point.z <= max.z
                : axis == 1
                    ? point.x >= min.x && point.x <= max.x && point.z >= min.z && point.z <= max.z
                    : point.x >= min.x && point.x <= max.x && point.y >= min.y && point.y <= max.y;

            if (!insideFace)
            {
                return;
            }

            bestDistance = distance;
            bestNormal = axis == 0
                ? (positive ? FixedPointVector3.right : FixedPointVector3.left)
                : axis == 1
                    ? (positive ? FixedPointVector3.up : FixedPointVector3.down)
                    : (positive ? FixedPointVector3.forward : FixedPointVector3.back);
        }

        private static void TrySelectRoundedAABBEdge(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPoint64 length,
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPoint64 radius,
            ref FixedPoint64 bestDistance,
            ref FixedPointVector3 bestNormal)
        {
            var edgeCollision = IntersectWithRayAndCapsule(origin, direction, length, start, end, radius);

            if (!edgeCollision.hit || edgeCollision.t >= bestDistance)
            {
                return;
            }

            bestDistance = edgeCollision.t;
            bestNormal = edgeCollision.normal;
        }

        private static bool IntersectRayAndSphereFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 center,
            FixedPoint64 radius,
            out FPCollision intersection)
        {
            intersection = new FPCollision();

            if (direct.IsZero() || length < FixedPoint64.Zero)
            {
                return false;
            }

            var normalizedDirection = direct.normalized;
            var offset = origin - center;
            var linear = FixedPointVector3.Dot(offset, normalizedDirection);
            var constant = offset.sqrMagnitude - radius * radius;

            // 与项目 Raycast 的既有语义一致：射线起点位于碰撞体内部时不报告命中。
            if (constant < FixedPoint64.Zero)
            {
                return false;
            }

            var discriminant = linear * linear - constant;

            if (discriminant < FixedPoint64.Zero)
            {
                return false;
            }

            var root = FixedPointMath.Sqrt(discriminant);
            var distance = -linear - root;

            if (distance < FixedPoint64.Zero)
            {
                distance = -linear + root;
            }

            if (distance < FixedPoint64.Zero || distance > length)
            {
                return false;
            }

            intersection.hit = true;
            intersection.t = distance;
            intersection.closestPoint = origin + normalizedDirection * distance;
            intersection.normal = (intersection.closestPoint - center).normalized;
            return true;
        }

        /// <summary>检测有限射线与球体的相交。</summary>
        /// <remarks>方法名中的 <c>Interset</c> 是历史公开 API 拼写，为兼容现有调用而保留。</remarks>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线方向。</param>
        /// <param name="length">最大检测距离。</param>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="intersection">命中信息。</param>
        /// <returns>有限射线从球体外部命中球面时返回 <see langword="true"/>；起点在球内时不命中。</returns>
        public static bool IntersetWithRayAndSphereFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 center,
            FixedPoint64 radius,
            out FPCollision intersection)
        {
            return IntersectRayAndSphereFixedPoint(
                origin,
                direct,
                length,
                center,
                radius,
                out intersection);
        }

        /// <summary>检测有限射线与球体的相交并仅返回交点。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线方向。</param>
        /// <param name="length">最大检测距离。</param>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <param name="intersection">命中时返回交点。</param>
        /// <returns>命中时返回 <see langword="true"/>。</returns>
        [Obsolete("请使用返回 FPCollision 的 IntersetWithRayAndSphereFixedPoint 兼容重载。")]
        public static bool IntersectWithRayAndSphereFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPoint64 length,
            FixedPointVector3 center,
            FixedPoint64 radius,
            out FixedPointVector3 intersection)
        {
            var hit = IntersectRayAndSphereFixedPoint(
                origin,
                direct,
                length,
                center,
                radius,
                out FPCollision collision);
            intersection = hit ? collision.closestPoint : FixedPointVector3.zero;
            return hit;
        }

        /// <summary>判断无限射线是否与球体相交。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="center">球心。</param>
        /// <param name="radius">球半径。</param>
        /// <returns>射线正向半轴存在球面交点时返回 <see langword="true"/>。</returns>
        [Obsolete("请使用返回 FPCollision 的有限射线重载。")]
        public static bool IntersectWithRayAndSphereFixedPoint(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPointVector3 center,
            FixedPoint64 radius)
        {
            if (direct.IsZero())
            {
                return false;
            }

            var normalizedDirection = direct.normalized;
            var offset = origin - center;
            var linear = FixedPointVector3.Dot(offset, normalizedDirection);
            var constant = offset.sqrMagnitude - radius * radius;

            if (constant < FixedPoint64.Zero)
            {
                return false;
            }

            var discriminant = linear * linear - constant;
            return discriminant >= FixedPoint64.Zero &&
                   -linear + FixedPointMath.Sqrt(discriminant) >= FixedPoint64.Zero;
        }

        /// <summary>使用分离区间算法计算无限直线与轴对齐包围盒的参数区间。</summary>
        private static bool TryIntersectLineWithAABB(
            FixedPointVector3 origin,
            FixedPointVector3 direction,
            FixedPointVector3 min,
            FixedPointVector3 max,
            out FixedPoint64 entryParameter,
            out FixedPoint64 exitParameter,
            out FixedPointVector3 entryNormal,
            out FixedPointVector3 exitNormal)
        {
            entryParameter = FixedPoint64.MinValue;
            exitParameter = FixedPoint64.MaxValue;
            entryNormal = FixedPointVector3.zero;
            exitNormal = FixedPointVector3.zero;

            return UpdateSlab(
                       origin.x,
                       direction.x,
                       min.x,
                       max.x,
                       FixedPointVector3.left,
                       FixedPointVector3.right,
                       ref entryParameter,
                       ref exitParameter,
                       ref entryNormal,
                       ref exitNormal) &&
                   UpdateSlab(
                       origin.y,
                       direction.y,
                       min.y,
                       max.y,
                       FixedPointVector3.down,
                       FixedPointVector3.up,
                       ref entryParameter,
                       ref exitParameter,
                       ref entryNormal,
                       ref exitNormal) &&
                   UpdateSlab(
                       origin.z,
                       direction.z,
                       min.z,
                       max.z,
                       FixedPointVector3.back,
                       FixedPointVector3.forward,
                       ref entryParameter,
                       ref exitParameter,
                       ref entryNormal,
                       ref exitNormal);
        }

        private static bool UpdateSlab(
            FixedPoint64 origin,
            FixedPoint64 direction,
            FixedPoint64 min,
            FixedPoint64 max,
            FixedPointVector3 minNormal,
            FixedPointVector3 maxNormal,
            ref FixedPoint64 entryParameter,
            ref FixedPoint64 exitParameter,
            ref FixedPointVector3 entryNormal,
            ref FixedPointVector3 exitNormal)
        {
            if (direction == FixedPoint64.Zero)
            {
                return origin >= min && origin <= max;
            }

            var first = (min - origin) / direction;
            var second = (max - origin) / direction;
            var firstNormal = minNormal;
            var secondNormal = maxNormal;

            if (first > second)
            {
                (first, second) = (second, first);
                (firstNormal, secondNormal) = (secondNormal, firstNormal);
            }

            if (first > entryParameter)
            {
                entryParameter = first;
                entryNormal = firstNormal;
            }

            if (second < exitParameter)
            {
                exitParameter = second;
                exitNormal = secondNormal;
            }

            return entryParameter <= exitParameter;
        }
    }
}
