using System;
using System.Runtime.CompilerServices;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供点包含、点位于边界以及点到几何体最近点的定点数查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>判断点是否位于球体内部或表面。</summary>
        /// <param name="point">待检测点。</param>
        /// <param name="position">球心。</param>
        /// <param name="radius">非负球半径。</param>
        /// <returns>点位于球体内部或表面时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInSphere(FixedPointVector3 point, FixedPointVector3 position, FixedPoint64 radius)
        {
            return (point - position).sqrMagnitude <= radius * radius;
        }

        /// <summary>获取球体内部或表面上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="position">球心。</param>
        /// <param name="radius">非负球半径。</param>
        /// <returns>输入点在球内时返回原点，否则返回球面最近点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndSphere(
            FixedPointVector3 point,
            FixedPointVector3 position,
            FixedPoint64 radius)
        {
            var offset = point - position;

            if (offset.sqrMagnitude <= radius * radius)
            {
                return point;
            }

            return offset.normalized * radius + position;
        }

        /// <summary>判断点是否位于平面上。</summary>
        /// <param name="point">待检测点。</param>
        /// <param name="plane">待检测平面。</param>
        /// <returns>点满足平面方程时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointOnPlane(FixedPointVector3 point, FixedPointPlane plane)
        {
            return FixedPointVector3.Dot(point, plane.normal) - plane.distance == FixedPoint64.Zero;
        }

        /// <summary>获取平面上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="plane">待查询平面。</param>
        /// <returns>点在平面上的正交投影。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndPlane(
            FixedPointVector3 point,
            FixedPointPlane plane)
        {
            return ClosestPointWithPointAndPlane(point, plane.distance, plane.normal);
        }

        /// <summary>获取平面上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="planeDistance">平面方程 <c>dot(normal, x) = distance</c> 中的距离。</param>
        /// <param name="planeNormal">平面的非零法线，无须归一化。</param>
        /// <returns>点在平面上的正交投影；法线为零时返回输入点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndPlane(
            FixedPointVector3 point,
            FixedPoint64 planeDistance,
            FixedPointVector3 planeNormal)
        {
            var normalLengthSquared = planeNormal.sqrMagnitude;

            if (normalLengthSquared == FixedPoint64.Zero)
            {
                return point;
            }

            var signedScale = (FixedPointVector3.Dot(planeNormal, point) - planeDistance) / normalLengthSquared;
            return point - planeNormal * signedScale;
        }

        /// <summary>判断点是否位于轴对齐包围盒内部或表面。</summary>
        /// <param name="point">待检测点。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>点位于三个闭区间内时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInAABB(FixedPointVector3 point, FixedPointVector3 min, FixedPointVector3 max)
        {
            return point.x >= min.x && point.x <= max.x &&
                   point.y >= min.y && point.y <= max.y &&
                   point.z >= min.z && point.z <= max.z;
        }

        /// <summary>获取轴对齐包围盒内部或表面上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="min">包围盒逐分量最小点。</param>
        /// <param name="max">包围盒逐分量最大点。</param>
        /// <returns>将点的三个坐标分别限制到包围盒区间后的结果。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndAABB(
            FixedPointVector3 point,
            FixedPointVector3 min,
            FixedPointVector3 max)
        {
            return new FixedPointVector3(
                FixedPointMath.Clamp(point.x, min.x, max.x),
                FixedPointMath.Clamp(point.y, min.y, max.y),
                FixedPointMath.Clamp(point.z, min.z, max.z));
        }

        /// <summary>判断点是否位于有向包围盒内部或表面。</summary>
        /// <param name="point">待检测点。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="fixedPointMatrix">包含三个单位局部轴的旋转矩阵。</param>
        /// <returns>点在三个局部轴上的投影均未超出半尺寸时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInOBB(
            FixedPointVector3 point,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix)
        {
            var offset = point - position;
            return !PointOutsideOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13),
                       halfSize.x) &&
                   !PointOutsideOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23),
                       halfSize.y) &&
                   !PointOutsideOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33),
                       halfSize.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool PointOutsideOBBAxis(
            FixedPointVector3 offset,
            FixedPointVector3 axis,
            FixedPoint64 extent)
        {
            var distance = FixedPointVector3.Dot(offset, axis);
            return distance < -extent || distance > extent;
        }

        /// <summary>获取有向包围盒内部或表面上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="position">有向包围盒中心。</param>
        /// <param name="halfSize">有向包围盒在三个局部轴上的半尺寸。</param>
        /// <param name="fixedPointMatrix">包含三个单位局部轴的旋转矩阵。</param>
        /// <returns>点在有向包围盒上的最近位置；点位于盒内时返回原点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndOBB(
            FixedPointVector3 point,
            FixedPointVector3 position,
            FixedPointVector3 halfSize,
            FixedPointMatrix fixedPointMatrix)
        {
            var offset = point - position;
            return position +
                   ProjectPointToOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M11, fixedPointMatrix.M12, fixedPointMatrix.M13),
                       halfSize.x) +
                   ProjectPointToOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M21, fixedPointMatrix.M22, fixedPointMatrix.M23),
                       halfSize.y) +
                   ProjectPointToOBBAxis(
                       offset,
                       new FixedPointVector3(fixedPointMatrix.M31, fixedPointMatrix.M32, fixedPointMatrix.M33),
                       halfSize.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static FixedPointVector3 ProjectPointToOBBAxis(
            FixedPointVector3 offset,
            FixedPointVector3 axis,
            FixedPoint64 extent)
        {
            var distance = FixedPointVector3.Dot(offset, axis);
            return FixedPointMath.Clamp(distance, -extent, extent) * axis;
        }

        /// <summary>获取闭线段上距离指定点最近的位置。</summary>
        /// <param name="start">线段起点。</param>
        /// <param name="end">线段终点。</param>
        /// <param name="point">待查询点。</param>
        /// <returns>线段上的最近点；退化线段返回起点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndLine(
            FixedPointVector3 start,
            FixedPointVector3 end,
            FixedPointVector3 point)
        {
            var direction = end - start;
            var lengthSquared = direction.sqrMagnitude;

            if (lengthSquared == FixedPoint64.Zero)
            {
                return start;
            }

            var parameter = FixedPointVector3.Dot(point - start, direction) / lengthSquared;
            parameter = FixedPointMath.Clamp(parameter, FixedPoint64.Zero, FixedPoint64.One);
            return start + direction * parameter;
        }

        /// <summary>判断点是否位于闭线段上。</summary>
        /// <param name="start">线段起点。</param>
        /// <param name="end">线段终点。</param>
        /// <param name="point">待检测点。</param>
        /// <returns>点与线段最近点完全重合时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointOnLine(FixedPointVector3 start, FixedPointVector3 end, FixedPointVector3 point)
        {
            return (ClosestPointWithPointAndLine(start, end, point) - point).sqrMagnitude == FixedPoint64.Zero;
        }

        /// <summary>判断点是否位于射线上。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="point">待检测点。</param>
        /// <returns>点与射线共线且位于正向半轴时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointOnRay(FixedPointVector3 origin, FixedPointVector3 direct, FixedPointVector3 point)
        {
            var offset = point - origin;

            if (offset.IsZero())
            {
                return true;
            }

            if (direct.IsZero() || FixedPointVector3.Dot(offset, direct) < FixedPoint64.Zero)
            {
                return false;
            }

            return FixedPointVector3.Cross(offset, direct).IsZero();
        }

        /// <summary>获取射线上距离指定点最近的位置。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线的非零方向，无须归一化。</param>
        /// <param name="point">待查询点。</param>
        /// <returns>射线上的最近点；方向为零时返回射线起点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndRay(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPointVector3 point)
        {
            var directionLengthSquared = direct.sqrMagnitude;

            if (directionLengthSquared == FixedPoint64.Zero)
            {
                return origin;
            }

            var parameter = FixedPointVector3.Dot(point - origin, direct) / directionLengthSquared;
            parameter = FixedPointMath.Max(parameter, FixedPoint64.Zero);
            return origin + direct * parameter;
        }

        /// <summary>获取射线上距离指定点最近的位置。</summary>
        /// <param name="origin">射线起点。</param>
        /// <param name="direct">射线方向。</param>
        /// <param name="point">待查询点。</param>
        /// <returns>射线上的最近点。</returns>
        [Obsolete("请使用 ClosestPointWithPointAndRay。")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClostPointWithPointAndRay(
            FixedPointVector3 origin,
            FixedPointVector3 direct,
            FixedPointVector3 point)
        {
            return ClosestPointWithPointAndRay(origin, direct, point);
        }

        /// <summary>判断点是否位于带平移中心的三角形内部或边界。</summary>
        /// <param name="point">世界坐标中的待检测点。</param>
        /// <param name="center">三角形局部顶点的世界平移量。</param>
        /// <param name="min">三角形的世界坐标包围盒最小点。</param>
        /// <param name="max">三角形的世界坐标包围盒最大点。</param>
        /// <param name="aVertex">三角形局部顶点 A。</param>
        /// <param name="bVertex">三角形局部顶点 B。</param>
        /// <param name="cVertex">三角形局部顶点 C。</param>
        /// <returns>点位于三角形平面内且落在三条边内侧时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInTriangle(
            FixedPointVector3 point,
            FixedPointVector3 center,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 aVertex,
            FixedPointVector3 bVertex,
            FixedPointVector3 cVertex)
        {
            if (!PointInAABB(point, min, max))
            {
                return false;
            }

            return PointInTriangle(point, aVertex + center, bVertex + center, cVertex + center);
        }

        /// <summary>判断点是否位于三角形内部或边界。</summary>
        /// <param name="point">待检测点。</param>
        /// <param name="vertex">三角形顶点 A。</param>
        /// <param name="vertex1">三角形顶点 B。</param>
        /// <param name="vertex2">三角形顶点 C。</param>
        /// <returns>点与三角形共面且位于三条边内侧时返回 <see langword="true"/>。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool PointInTriangle(
            FixedPointVector3 point,
            FixedPointVector3 vertex,
            FixedPointVector3 vertex1,
            FixedPointVector3 vertex2)
        {
            var edge0 = vertex1 - vertex;
            var edge1 = vertex2 - vertex;
            var normal = FixedPointVector3.Cross(edge0, edge1);

            if (normal.IsZero() ||
                FixedPointMath.Abs(FixedPointVector3.Dot(point - vertex, normal.normalized)) >
                FixedPoint64.Epsilon)
            {
                return false;
            }

            var offset = point - vertex;
            var dot00 = FixedPointVector3.Dot(edge0, edge0);
            var dot01 = FixedPointVector3.Dot(edge0, edge1);
            var dot11 = FixedPointVector3.Dot(edge1, edge1);
            var dot20 = FixedPointVector3.Dot(offset, edge0);
            var dot21 = FixedPointVector3.Dot(offset, edge1);
            var denominator = dot00 * dot11 - dot01 * dot01;

            if (denominator == FixedPoint64.Zero)
            {
                return false;
            }

            var coordinate1 = (dot11 * dot20 - dot01 * dot21) / denominator;
            var coordinate2 = (dot00 * dot21 - dot01 * dot20) / denominator;
            return coordinate1 >= FixedPoint64.Zero && coordinate2 >= FixedPoint64.Zero &&
                   coordinate1 + coordinate2 <= FixedPoint64.One;
        }

        /// <summary>获取三角形上距离指定点最近的位置。</summary>
        /// <param name="point">待查询点。</param>
        /// <param name="vertex">三角形顶点 A。</param>
        /// <param name="vertex1">三角形顶点 B。</param>
        /// <param name="vertex2">三角形顶点 C。</param>
        /// <param name="normal">三角形的单位法线。</param>
        /// <param name="normalDistance">三角形平面到原点的有符号距离。</param>
        /// <param name="pointInTriangle">点在平面上的投影是否位于三角形内部。</param>
        /// <returns>三角形内部或三条边上的最近点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndTriangle(
            FixedPointVector3 point,
            FixedPointVector3 vertex,
            FixedPointVector3 vertex1,
            FixedPointVector3 vertex2,
            FixedPointVector3 normal,
            FixedPoint64 normalDistance,
            bool pointInTriangle)
        {
            var projectedPoint = ClosestPointWithPointAndPlane(point, normalDistance, normal);

            if (pointInTriangle)
            {
                return projectedPoint;
            }

            return ClosestPointOnTriangleEdges(point, vertex, vertex1, vertex2);
        }

        /// <summary>获取带平移中心的三角形上距离指定点最近的位置。</summary>
        /// <param name="point">世界坐标中的待查询点。</param>
        /// <param name="center">三角形局部顶点的世界平移量。</param>
        /// <param name="min">三角形的世界坐标包围盒最小点。</param>
        /// <param name="max">三角形的世界坐标包围盒最大点。</param>
        /// <param name="a">三角形局部顶点 A。</param>
        /// <param name="b">三角形局部顶点 B。</param>
        /// <param name="c">三角形局部顶点 C。</param>
        /// <returns>三角形内部或三条边上的世界坐标最近点。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPointVector3 ClosestPointWithPointAndTriangle(
            FixedPointVector3 point,
            FixedPointVector3 center,
            FixedPointVector3 min,
            FixedPointVector3 max,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            var worldA = a + center;
            var worldB = b + center;
            var worldC = c + center;
            var plane = FromTriangle(FixedPointVector3.zero, worldA, worldB, worldC);
            var projectedPoint = ClosestPointWithPointAndPlane(point, plane);

            if (PointInAABB(projectedPoint, min, max) && PointInTriangle(projectedPoint, worldA, worldB, worldC))
            {
                return projectedPoint;
            }

            return ClosestPointOnTriangleEdges(point, worldA, worldB, worldC);
        }

        private static FixedPointVector3 ClosestPointOnTriangleEdges(
            FixedPointVector3 point,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            var closestAB = ClosestPointWithPointAndLine(a, b, point);
            var closestBC = ClosestPointWithPointAndLine(b, c, point);
            var closestCA = ClosestPointWithPointAndLine(c, a, point);
            var distanceAB = (point - closestAB).sqrMagnitude;
            var distanceBC = (point - closestBC).sqrMagnitude;
            var distanceCA = (point - closestCA).sqrMagnitude;

            if (distanceAB <= distanceBC && distanceAB <= distanceCA)
            {
                return closestAB;
            }

            return distanceBC <= distanceCA ? closestBC : closestCA;
        }

        /// <summary>根据三个局部顶点和平移中心创建平面。</summary>
        private static FixedPointPlane FromTriangle(
            FixedPointVector3 center,
            FixedPointVector3 a,
            FixedPointVector3 b,
            FixedPointVector3 c)
        {
            var normal = FixedPointVector3.Normalize(FixedPointVector3.Cross(b - a, c - a));
            return new FixedPointPlane
            {
                normal = normal,
                distance = FixedPointVector3.Dot(normal, a + center)
            };
        }
    }
}