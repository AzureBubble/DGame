namespace DGame.FixedPoint
{
    /// <summary>
    /// 定点数八叉树节点，用于描述空间分区及该分区内按类型存放的碰撞器。
    /// </summary>
    public class FPOctreeNode
    {
        /// <summary>
        /// 父节点；根节点的父节点为 <see langword="null"/>。
        /// </summary>
        public FPOctreeNode parentNode;

        /// <summary>
        /// 八个子节点；为 <see langword="null"/> 时表示当前节点是叶节点。
        /// </summary>
        public FPOctreeNode[] nodes;

        /// <summary>
        /// 构建八叉树层级时使用的逻辑尺寸。
        /// 根节点沿用八叉树初始化尺寸，子节点递归取父节点逻辑尺寸的一半。
        /// </summary>
        public readonly int size;

        /// <summary>
        /// 当前节点轴对齐包围盒在各坐标轴上的半范围，由八叉树构建算法计算。
        /// </summary>
        public readonly int halfSize;

        /// <summary>
        /// 节点覆盖立方体的中心坐标。
        /// </summary>
        public readonly FixedPointVector3 pos;

        /// <summary>
        /// 当前节点直接包含的球形碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPSphereCollider> FpSphereColliders;

        /// <summary>
        /// 当前节点直接包含的轴对齐包围盒碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPAABBCollider> FpAABBColliders;

        /// <summary>
        /// 当前节点直接包含的有向包围盒碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPBoxCollider> FpObbColliders;

        /// <summary>
        /// 当前节点直接包含的胶囊体碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPCapsuleCollider> FpCapsuleColliders;

        /// <summary>
        /// 当前节点直接包含的圆柱体碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPCylinderCollider> FpCylinderColliders;

        /// <summary>
        /// 当前节点直接包含的轴对齐胶囊体碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPAACapsuleCollider> FpAACapsuleColliders;

        /// <summary>
        /// 当前节点直接包含的网格碰撞器集合。
        /// </summary>
        public FPOctreeColliderSet<FPMeshCollider> FpMeshColliders;

        /// <summary>
        /// 当前节点直接包含的角色控制器集合。
        /// </summary>
        public FPOctreeColliderSet<FPCharacterController> FpCharacterColliders;

        /// <summary>
        /// 根据节点中心和半边长在构造时生成的轴对齐包围盒。
        /// </summary>
        public readonly FixedPointAABB fixedPointAABB;

        /// <summary>
        /// 当前节点及其全部后代节点包含的碰撞器总数。
        /// </summary>
        public int colliderCount;

        /// <summary>
        /// 创建定点数八叉树节点。
        /// </summary>
        /// <param name="halfSize">节点轴对齐包围盒在各坐标轴上的半范围。</param>
        /// <param name="pos">节点覆盖立方体的中心坐标。</param>
        /// <param name="size">构建节点层级时使用的逻辑尺寸。</param>
        public FPOctreeNode(int halfSize, FixedPointVector3 pos,int size)
        {
            this.halfSize = halfSize;
            this.pos = pos;
            var minX = pos.x - halfSize;
            var maxX = pos.x + halfSize;
            var minY = pos.y - halfSize;
            var maxY = pos.y + halfSize;
            var minZ = pos.z - halfSize;
            var maxZ = pos.z + halfSize;
            var min = new FixedPointVector3(minX, minY, minZ);
            var max = new FixedPointVector3(maxX, maxY, maxZ);
            fixedPointAABB = new FixedPointAABB(min, max);
            this.size = size;
        }

        /// <summary>
        /// 检查指定坐标是否位于当前节点的半开包围盒内。
        /// 最小边界包含，最大边界排除，以避免相邻子节点重复包含边界坐标。
        /// </summary>
        /// <param name="position">需要检查的坐标。</param>
        /// <returns>坐标位于当前节点内时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
        public bool VerifyInside(FixedPointVector3 position)
        {
            return position.x >= fixedPointAABB.Min.x && position.x < fixedPointAABB.Max.x && position.y >= fixedPointAABB.Min.y && position.y < fixedPointAABB.Max.y && position.z >= fixedPointAABB.Min.z && position.z < fixedPointAABB.Max.z;
        }
    }
}
