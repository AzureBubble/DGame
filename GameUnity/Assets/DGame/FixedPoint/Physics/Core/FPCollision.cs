namespace DGame.FixedPoint
{
    /// <summary>
    /// 表示一次定点物理碰撞检测或相交查询的结果。
    /// 不同查询只会填充其需要的字段，读取其他字段前应先确认对应查询的返回约定。
    /// </summary>
    public struct FPCollision
    {
        /// <summary>
        /// 查询是否命中或检测到重叠。
        /// </summary>
        public bool hit;

        /// <summary>
        /// 命中的碰撞器。基础几何相交方法可能不设置此字段，由上层空间查询补充。
        /// </summary>
        public FPCollider collider;
        
        /// <summary>
        /// 碰撞检测计算得到的主要表面点或射线命中点。
        /// </summary>
        public FixedPointVector3 closestPoint;
        
        /// <summary>
        /// 与 <see cref="closestPoint"/> 成对的另一个边界点。
        /// 在重叠查询中通常是查询形状上的表面点，在射线与体积相交时可能是另一个交点。
        /// </summary>
        public FixedPointVector3 outsidePoint;
        
        /// <summary>
        /// 代表性接触点，通常取 <see cref="closestPoint"/> 与 <see cref="outsidePoint"/> 的中点。
        /// </summary>
        public FixedPointVector3 contactPoint;
        
        /// <summary>
        /// 碰撞法线，应为单位向量。重叠查询中指向查询形状的分离方向；
        /// 射线查询中表示命中表面的法线。
        /// </summary>
        public FixedPointVector3 normal;
        
        /// <summary>
        /// 沿射线或线段方向到命中点的参数。仅在明确返回该参数的相交查询中有效。
        /// </summary>
        public FixedPoint64 t;
        
        /// <summary>
        /// 非负的半穿透深度。当一个形状需要承担全部位置修正时，完整分离距离为此值的两倍。
        /// </summary>
        public FixedPoint64 depth;

#if UNITY_EDITOR
        /// <summary>
        /// 仅在 Unity Editor 中保留的算法调试点。
        /// </summary>
        public FixedPointVector3? debugInfo;

        /// <summary>
        /// 仅在 Unity Editor 中保留的第二个算法调试点。
        /// </summary>
        public FixedPointVector3? debugInfo1;
#endif
    }
}
