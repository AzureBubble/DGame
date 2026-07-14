#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 中心轴可随定点数变换旋转的胶囊碰撞器。
    /// </summary>
    public class FPCapsuleCollider : FPAACapsuleCollider
    {
        /// <summary>
        /// 获取胶囊中心轴的世界坐标起点。
        /// </summary>
        internal override FixedPointVector3 startPos => position - fpTransform.rotation * new FixedPointVector3(
            0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 获取胶囊中心轴的世界坐标终点。
        /// </summary>
        internal override FixedPointVector3 endPos => position + fpTransform.rotation * new FixedPointVector3(
            0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 获取可旋转胶囊碰撞器类型。
        /// </summary>
        public override ColliderType colliderType => ColliderType.Capsule;

        /// <summary>
        /// 将碰撞器从当前八叉树节点的胶囊集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpCapsuleColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的胶囊集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpCapsuleColliders ??= new FPOctreeColliderSet<FPCapsuleCollider>(node);
            node.FpCapsuleColliders.Add(this);
            targetNode = node;
        }

        /// <summary>
        /// 根据胶囊中心轴端点和世界半径更新轴对齐包围盒。
        /// </summary>
        internal override void UpdateAABB()
        {
            var radiusVector = new FixedPointVector3(scaledRadius, scaledRadius, scaledRadius);
            _min = FixedPointVector3.Min(startPos, endPos) - radiusVector;
            _max = FixedPointVector3.Max(startPos, endPos) + radiusVector;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 在 Unity 场景视图中绘制胶囊碰撞器线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            DrawWireCapsule(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
        }

        /// <summary>
        /// 在两个中心轴端点之间绘制指定半径的线框胶囊。
        /// </summary>
        /// <param name="p1">中心轴起点。</param>
        /// <param name="p2">中心轴终点。</param>
        /// <param name="radius">胶囊半径。</param>
        public static void DrawWireCapsule(Vector3 p1, Vector3 p2, float radius)
        {
#if UNITY_EDITOR
            // 中心轴退化为一点时，胶囊等价于球体。
            if (p1 == p2)
            {
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }

            // 使用 Unity Handles 绘制半球端盖和连接线。
            using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                var p1Rotation = Quaternion.LookRotation(p1 - p2);
                var p2Rotation = Quaternion.LookRotation(p2 - p1);
                // 胶囊轴与纵轴共线时修正第二个端盖的朝向。
                var c = Vector3.Dot((p1 - p2).normalized, Vector3.up);

                if (System.Math.Abs(c - 1f) < 0.00001f || System.Math.Abs(c + 1f) < 0.00001f)
                {
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x,
                        p2Rotation.eulerAngles.y + 180f, p2Rotation.eulerAngles.z);
                }

                // 绘制起点半球端盖。
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.left, p1Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p1, p1Rotation * Vector3.up, p1Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                // 绘制终点半球端盖。
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.left, p2Rotation * Vector3.down, 180f, radius);
                UnityEditor.Handles.DrawWireArc(p2, p2Rotation * Vector3.up, p2Rotation * Vector3.left, 180f, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);
                // 绘制胶囊圆柱段的四条母线。
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.down * radius,
                    p2 + p2Rotation * Vector3.down * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.left * radius,
                    p2 + p2Rotation * Vector3.right * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.up * radius,
                    p2 + p2Rotation * Vector3.up * radius);
                UnityEditor.Handles.DrawLine(p1 + p1Rotation * Vector3.right * radius,
                    p2 + p2Rotation * Vector3.left * radius);
            }
#endif
        }
#endif
    }
}