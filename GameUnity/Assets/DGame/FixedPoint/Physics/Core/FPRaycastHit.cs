namespace DGame.FixedPoint
{
    /// <summary>
    /// 表示定点物理射线查询命中碰撞器后返回的结果。
    /// </summary>
    /// <remarks>
    /// 该类型由 <see cref="FPPhysicsContext.Raycast"/> 在命中时创建，用于向调用方提供命中碰撞器、
    /// 命中位置、表面法线以及可选的另一侧边界交点。所有空间数据均使用定点向量表示。
    /// </remarks>
    public class FPRaycastHit
    {
        /// <summary>
        /// 获取射线命中的碰撞器。
        /// </summary>
        public FPCollider fpCollider { private set; get; }

        /// <summary>
        /// 获取射线沿查询方向首先命中的边界点。
        /// </summary>
        public FixedPointVector3 point { private set; get; }

        /// <summary>
        /// 获取射线与命中形状边界的另一个交点。
        /// </summary>
        /// <remarks>
        /// 当前仅 OBB 射线检测路径提供该值；其他碰撞器类型保持为 <see cref="FixedPointVector3.zero"/>。
        /// </remarks>
        public FixedPointVector3 outPoint { private set; get; }

        /// <summary>
        /// 获取命中点处指向碰撞器外部的表面法线。
        /// </summary>
        public FixedPointVector3 normal { private set; get; }

        /// <summary>获取从查询起点到命中点的世界距离。</summary>
        public FixedPoint64 distance { private set; get; }

        /// <summary>
        /// 创建一个定点物理射线命中结果。
        /// </summary>
        /// <param name="fpCollider">射线命中的碰撞器。</param>
        /// <param name="point">射线首先命中的边界点。</param>
        /// <param name="normal">命中点处的表面法线。</param>
        /// <param name="outPoint">射线与命中形状边界的另一个交点；未提供时为零向量。</param>
        public FPRaycastHit(FPCollider fpCollider, FixedPointVector3 point, FixedPointVector3 normal,
            FixedPointVector3 outPoint, FixedPoint64 distance)
        {
            Set(fpCollider, point, normal, outPoint, distance);
        }

        internal void Set(FPCollider collider, FixedPointVector3 hitPoint,
            FixedPointVector3 hitNormal, FixedPointVector3 exitPoint, FixedPoint64 hitDistance)
        {
            fpCollider = collider;
            point = hitPoint;
            normal = hitNormal;
            outPoint = exitPoint;
            distance = hitDistance;
        }
    }
}
