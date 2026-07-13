#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 在固定点物理系统中处理定向包围盒（OBB）碰撞器，扩展轴对齐包围盒（AABB）的功能，
    /// 用于更精确和多样化的碰撞检测。
    /// </summary>
    public class FPBoxCollider : FPAABBCollider
    {
        public override ColliderType colliderType => ColliderType.OBB;

        /// <summary>
        /// 从其所属的所有影响节点中移除此碰撞器，确保它不再参与碰撞检查。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpObbColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将此碰撞器添加到指定的八叉树节点的AABB碰撞器列表中，启用碰撞检查。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpObbColliders ??= new FPOctreeColliderSet<FPBoxCollider>(node);
            node.FpObbColliders.Add(this);
            targetNode = node;
        }

        /// <summary>
        /// 存储定义OBB角落的八个顶点，
        /// 允许根据其当前方向和大小精确计算碰撞器边界。
        /// </summary>
        private FixedPointVector3[] points { get; } = new FixedPointVector3[8];

        /// <summary>
        /// 重新计算碰撞器的轴对齐包围盒（AABB），以包含其当前方向和尺寸。
        /// </summary>
        internal override void UpdateAABB()
        {
            var orientation = fpTransform.rotation;
            // 计算OBB的八个角落的位置。
            var pos = position;
            points[0] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, halfSize.z);
            points[1] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, -halfSize.z);
            points[2] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, -halfSize.z);
            points[3] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, halfSize.z);
            points[4] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, halfSize.z);
            points[5] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, -halfSize.z);
            points[6] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, -halfSize.z);
            points[7] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, halfSize.z);
            // 将_min和_max初始化为第一个点。
            _min = points[0];
            _max = points[0];

            // 找到形成AABB的最小和最大点。
            for (var i = 1; i < 8; i++)
            {
                _min = FixedPointVector3.Min(_min, points[i]);
                _max = FixedPointVector3.Max(_max, points[i]);
            }
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 在Unity编辑器中可视化OBB碰撞器，通过绘制与碰撞器当前方向和缩放匹配的线框表示。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            // 保存原始Gizmos矩阵。
            var matrix = Gizmos.matrix;
            // 设置Gizmos矩阵以匹配碰撞器变换。
            var qua = fpTransform.rotation;
            Gizmos.matrix = Matrix4x4.TRS(
                fpTransform.position.ToVector3(),
                qua.ToQuaternion(),
                size.ToVector3());

            // 绘制单位大小的线框立方体，通过Gizmos矩阵缩放以匹配碰撞器大小。
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            // 恢复原始Gizmos矩阵。
            Gizmos.matrix = matrix;
        }
#endif
    }
}
