#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 中心轴可随定点数变换旋转的圆柱碰撞器。
    /// </summary>
    public class FPCylinderCollider : FPCollider
    {
#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>未应用变换缩放的本地半径。</summary>
        protected FixedPoint64 _radius;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>未应用变换缩放的本地总高度。</summary>
        protected FixedPoint64 _height;

        /// <summary>
        /// 获取或设置圆柱的本地半径。
        /// </summary>
        public FixedPoint64 radius
        {
            get => _radius;
            set
            {
                _radius = ValidateNonNegative(value, nameof(value));
                UpdateCollider();
            }
        }

        /// <summary>
        /// 获取或设置圆柱的本地总高度。
        /// </summary>
        public FixedPoint64 height
        {
            get => _height;
            set
            {
                _height = ValidateNonNegative(value, nameof(value));
                UpdateCollider();
            }
        }

        /// <summary>
        /// 获取应用纵轴绝对缩放后的世界半高。
        /// </summary>
        internal FixedPoint64 scaledHalfHeight => height * 0.5 * absoluteScale.y;

        /// <summary>
        /// 获取应用横向最大绝对缩放后的世界半径。
        /// </summary>
        internal FixedPoint64 scaledRadius => _radius * FixedPointMath.Max(absoluteScale.x, absoluteScale.z);

        /// <summary>
        /// 获取圆柱底面中心的世界坐标。
        /// </summary>
        internal FixedPointVector3 startPos => position -
                                               fpTransform.rotation * new FixedPointVector3(0, scaledHalfHeight, 0);

        /// <summary>
        /// 获取圆柱顶面中心的世界坐标。
        /// </summary>
        internal FixedPointVector3 endPos => position +
                                             fpTransform.rotation * new FixedPointVector3(0, scaledHalfHeight, 0);

        /// <summary>获取圆柱碰撞器类型。</summary>
        public override ColliderType colliderType => ColliderType.Cylinder;

        /// <summary>
        /// 保存圆柱局部包围盒的八个世界坐标角点。
        /// </summary>
        private FixedPointVector3[] points { get; } = new FixedPointVector3[8];

        /// <summary>根据圆柱当前的世界尺寸和旋转更新轴对齐包围盒。</summary>
        internal override void UpdateAABB()
        {
            var halfSize = new FixedPointVector3(scaledRadius, scaledHalfHeight, scaledRadius);
            var orientation = fpTransform.rotation;
            // 计算包围圆柱的局部盒体八个世界坐标角点。
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

            // 合并其余角点得到轴对齐边界。
            for (var i = 1; i < 8; i++)
            {
                _min = FixedPointVector3.Min(_min, points[i]);
                _max = FixedPointVector3.Max(_max, points[i]);
            }
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点的圆柱集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpCylinderColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的圆柱集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpCylinderColliders ??= new FPOctreeColliderSet<FPCylinderCollider>(node);
            node.FpCylinderColliders.Add(this);
            targetNode = node;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 根据当前对象的网格边界初始化圆柱本地半径和总高度。
        /// </summary>
        protected override void InitColliderSize()
        {
            var mesh = GetComponent<MeshFilter>();

            if (mesh == null || mesh.sharedMesh == null)
            {
                return;
            }

            var bounds = mesh.sharedMesh.bounds;
            var boundRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            _radius = boundRadius;
            _height = bounds.size.y;
        }

        /// <summary>
        /// 在 Unity 场景视图中绘制圆柱碰撞器线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            DrawWireCylinder(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
        }

        /// <summary>
        /// 在两个端面中心之间绘制指定半径的线框圆柱。
        /// </summary>
        private static void DrawWireCylinder(Vector3 p1, Vector3 p2, float radius)
        {
#if UNITY_EDITOR
            // 中心轴退化时使用球形标记表示其位置和半径。
            if (p1 == p2)
            {
                Gizmos.DrawWireSphere(p1, radius);
                return;
            }

            using (new UnityEditor.Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                // 计算两个端面的绘制朝向。
                var p1Rotation = Quaternion.LookRotation(p1 - p2);
                var p2Rotation = Quaternion.LookRotation(p2 - p1);

                // 圆柱轴与纵轴共线时修正第二个端面的朝向。
                var c = Vector3.Dot((p1 - p2).normalized, Vector3.up);

                if (System.Math.Abs(c - 1f) < 0.000001 || System.Math.Abs(c - (-1f)) < 0.000001)
                {
                    p2Rotation = Quaternion.Euler(p2Rotation.eulerAngles.x, p2Rotation.eulerAngles.y + 180f,
                        p2Rotation.eulerAngles.z);
                }

                // 绘制两个圆形端面。
                UnityEditor.Handles.DrawWireDisc(p1, (p2 - p1).normalized, radius);
                UnityEditor.Handles.DrawWireDisc(p2, (p1 - p2).normalized, radius);

                // 绘制连接两个端面的四条母线。
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