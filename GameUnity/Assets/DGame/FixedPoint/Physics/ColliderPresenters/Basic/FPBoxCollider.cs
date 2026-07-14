#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 可随定点数变换旋转的有向包围盒碰撞器。
    /// </summary>
    public class FPBoxCollider : FPAABBCollider
    {
        /// <summary>获取有向包围盒碰撞器类型。</summary>
        public override ColliderType colliderType => ColliderType.OBB;

        /// <summary>
        /// 将碰撞器从当前八叉树节点的有向包围盒集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpObbColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的有向包围盒集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpObbColliders ??= new FPOctreeColliderSet<FPBoxCollider>(node);
            node.FpObbColliders.Add(this);
            targetNode = node;
        }

        /// <summary>
        /// 保存有向包围盒的八个世界坐标角点。
        /// </summary>
        private FixedPointVector3[] points { get; } = new FixedPointVector3[8];

        /// <summary>
        /// 重新计算碰撞器的轴对齐包围盒（AABB），以包含其当前方向和尺寸。
        /// </summary>
        internal override void UpdateAABB()
        {
            var orientation = fpTransform.rotation;
            // 计算有向包围盒的八个世界坐标角点。
            var pos = position;
            points[0] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, halfSize.z);
            points[1] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, -halfSize.z);
            points[2] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, -halfSize.z);
            points[3] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, halfSize.z);
            points[4] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, halfSize.z);
            points[5] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, -halfSize.z);
            points[6] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, -halfSize.z);
            points[7] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, halfSize.z);
            // 使用第一个角点初始化轴对齐边界。
            _min = points[0];
            _max = points[0];

            // 合并其余角点得到包围有向盒的轴对齐边界。
            for (var i = 1; i < 8; i++)
            {
                _min = FixedPointVector3.Min(_min, points[i]);
                _max = FixedPointVector3.Max(_max, points[i]);
            }
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 在 Unity 场景视图中绘制有向包围盒线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            var matrix = Gizmos.matrix;
            var qua = fpTransform.rotation;
            Gizmos.matrix = Matrix4x4.TRS(
                position.ToVector3(),
                qua.ToQuaternion(),
                size.ToVector3());
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = matrix;
        }
#endif
    }
}