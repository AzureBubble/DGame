using System.Runtime.CompilerServices;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 提供定点数几何体之间的相交、最近点和距离查询。
    /// </summary>
    public static partial class FixedPointIntersection
    {
        /// <summary>
        /// 计算无限直线与平面的交点。
        /// </summary>
        /// <param name="point">直线上的一点。</param>
        /// <param name="direct">直线的非零方向。</param>
        /// <param name="planeNormal">平面的非零法线。</param>
        /// <param name="planePoint">平面上的一点。</param>
        /// <param name="intersection">计算成功时返回交点。</param>
        /// <returns>直线与平面不平行时返回 <see langword="true"/>。</returns>
        public static bool IntersectWithLineAndPlaneFixedPoint(
            FixedPointVector3 point,
            FixedPointVector3 direct,
            FixedPointVector3 planeNormal,
            FixedPointVector3 planePoint,
            out FixedPointVector3 intersection)
        {
            intersection = FixedPointVector3.zero;
            var denominator = FixedPointVector3.Dot(direct, planeNormal);

            if (denominator == FixedPoint64.Zero)
            {
                return false;
            }

            var distance = FixedPointVector3.Dot(planePoint - point, planeNormal) / denominator;
            intersection = point + direct * distance;
            return true;
        }

        /// <summary>
        /// 判断射线是否与轴对齐包围盒相交。
        /// </summary>
        /// <param name="ray">待检测的射线。</param>
        /// <param name="aabb">待检测的轴对齐包围盒。</param>
        /// <returns>射线在正向范围内命中或起点位于包围盒内时返回 <see langword="true"/>。</returns>
        /// <remarks>
        /// 使用分离区间（slab）算法。射线方向及其倒数由 <see cref="FixedPointRay"/> 预先计算。
        /// </remarks>
        public static bool Intersects(FixedPointRay ray, FixedPointAABB aabb)
        {
            var tMin = ((ray.Sign.x == FixedPoint64.Zero ? aabb.Min.x : aabb.Max.x) - ray.Point.x) * ray.InvDir.x;
            var tMax = ((ray.Sign.x == FixedPoint64.Zero ? aabb.Max.x : aabb.Min.x) - ray.Point.x) * ray.InvDir.x;
            var tyMin = ((ray.Sign.y == FixedPoint64.Zero ? aabb.Min.y : aabb.Max.y) - ray.Point.y) * ray.InvDir.y;
            var tyMax = ((ray.Sign.y == FixedPoint64.Zero ? aabb.Max.y : aabb.Min.y) - ray.Point.y) * ray.InvDir.y;

            if (tMin > tyMax || tyMin > tMax)
            {
                return false;
            }

            if (tyMin > tMin)
            {
                tMin = tyMin;
            }

            if (tyMax < tMax)
            {
                tMax = tyMax;
            }

            var tzMin = ((ray.Sign.z == FixedPoint64.Zero ? aabb.Min.z : aabb.Max.z) - ray.Point.z) * ray.InvDir.z;
            var tzMax = ((ray.Sign.z == FixedPoint64.Zero ? aabb.Max.z : aabb.Min.z) - ray.Point.z) * ray.InvDir.z;

            if (tMin > tzMax || tzMin > tMax)
            {
                return false;
            }

            if (tzMin > tMin)
            {
                tMin = tzMin;
            }

            if (tzMax < tMax)
            {
                tMax = tzMax;
            }

            // 整个相交区间都位于射线起点后方时，不属于射线命中。
            return tMax >= FixedPoint64.Zero;
        }

        /// <summary>
        /// 计算点到射线所在无限直线的平方距离。
        /// </summary>
        /// <param name="ray">方向已归一化的射线。</param>
        /// <param name="point">待计算的点。</param>
        /// <returns>点到射线所在无限直线的平方距离。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedPoint64 SqrDistanceToLine(FixedPointRay ray, FixedPointVector3 point)
        {
            return FixedPointVector3.Cross(ray.Dir, point - ray.Point).sqrMagnitude;
        }
    }
}