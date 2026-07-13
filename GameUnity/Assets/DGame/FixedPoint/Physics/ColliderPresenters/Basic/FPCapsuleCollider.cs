#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 代表一个使用固定点数学进行精确物理模拟的胶囊碰撞器的表示器。
    /// 这个类扩展了AACapsuleCollider以实现特定于胶囊的逻辑。
    /// </summary>
    public class FPCapsuleCollider : FPAACapsuleCollider
    {
        /// <summary>
        /// 获取胶囊的起始位置。
        /// </summary>
        internal override FixedPointVector3 startPos => position - fpTransform.rotation * new FixedPointVector3(0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 获取胶囊的结束位置。
        /// </summary>
        internal override FixedPointVector3 endPos => position + fpTransform.rotation * new FixedPointVector3(0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 定义碰撞器类型为胶囊。
        /// </summary>
        public override ColliderType colliderType => ColliderType.Capsule;

        /// <summary>
        /// 从其所属的所有影响节点中移除此碰撞器，确保它不再参与碰撞检查。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpCapsuleColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将此碰撞器添加到指定的八叉树节点的胶囊碰撞器列表中，启用碰撞检查。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpCapsuleColliders ??= new FPOctreeColliderSet<FPCapsuleCollider>(node);
            node.FpCapsuleColliders.Add(this);
            targetNode = node;
        }

        /// <summary>
        /// 定义一个包围盒的八个角点。
        /// </summary>
        private FixedPointVector3[] points { get; } = new FixedPointVector3[8];

        /// <summary>
        /// 计算胶囊的AABB。
        /// </summary>
        internal override void UpdateAABB()
        {
            var halfWidth = scaledRadius;
            var halfSize = new FixedPointVector3(halfWidth, FixedPointMath.Max(halfWidth, scaledHalfHeight - halfWidth), halfWidth);

            var orientation = fpTransform.rotation;
            // 计算概念上包含胶囊的包围盒的位置
            var pos = position;
            points[0] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, halfSize.z);
            points[1] = pos + orientation * new FixedPointVector3(halfSize.x, halfSize.y, -halfSize.z);
            points[2] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, -halfSize.z);
            points[3] = pos + orientation * new FixedPointVector3(halfSize.x, -halfSize.y, halfSize.z);
            points[4] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, halfSize.z);
            points[5] = pos + orientation * new FixedPointVector3(-halfSize.x, halfSize.y, -halfSize.z);
            points[6] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, -halfSize.z);
            points[7] = pos + orientation * new FixedPointVector3(-halfSize.x, -halfSize.y, halfSize.z);
            // 初始化AABB的_min和_max
            _min = points[0];
            _max = points[0];

            // 扩展AABB以包括球形端点
            for (var i = 1; i < 8; i++)
            {
                _min = FixedPointVector3.Min(_min, points[i]);
                _max = FixedPointVector3.Max(_max, points[i]);
            }

            // 调整AABB以包括胶囊的球形端点
            pos = startPos;
            _min = FixedPointVector3.Min(_min, pos - new FixedPointVector3(halfWidth, halfWidth, halfWidth));
            _max = FixedPointVector3.Max(_max, pos + new FixedPointVector3(halfWidth, halfWidth, halfWidth));

            pos = endPos;
            _min = FixedPointVector3.Min(_min, pos - new FixedPointVector3(halfWidth, halfWidth, halfWidth));
            _max = FixedPointVector3.Max(_max, pos + new FixedPointVector3(halfWidth, halfWidth, halfWidth));
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 在Unity编辑器中绘制胶囊碰撞器的线框表示。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            DrawWireCapsule(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
        }

        /// <summary>
        /// 在两点之间绘制一个指定半径的线框胶囊。
        /// </summary>
        public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
#if UNITY_EDITOR
            // 处理两个点在同一位置的特殊情况。
            if (p1 == p2)
            {
                // 在这种情况下，简单地在位置处绘制一个线框球体。
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }
            // 使用Unity编辑器的Handles来绘制复杂形状。
            using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                var p1Rotation = Quaternion.LookRotation(p1 - p2);
                var p2Rotation = Quaternion.LookRotation(p2 - p1);
                // 检查胶囊的方向是否与Vector3.up方向共线。
                var c = Vector3.Dot((p1 - p2).normalized, Vector3.up);
                if (System.Math.Abs(c - 1f) < 0.00001f || System.Math.Abs(c + 1f) < 0.00001f)
                {
                    // 修正旋转
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }
                // 第一侧
                // 绘制弧线和线条来表示胶囊的线框。
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // 第二侧
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // 线条
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius, p2 + p2Rotation * Vector3.down * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius, p2 + p2Rotation * Vector3.right * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius, p2 + p2Rotation * Vector3.up * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius, p2 + p2Rotation * Vector3.left * radius);
            }
#endif
        }
#endif
    }
}
