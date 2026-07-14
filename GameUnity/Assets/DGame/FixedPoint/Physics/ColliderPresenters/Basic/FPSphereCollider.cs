#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点数表示的球形碰撞器。
    /// </summary>
    public class FPSphereCollider : FPCollider
    {
#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        private FixedPoint64 _radius;

        /// <summary>
        /// 获取或设置未应用变换缩放的本地半径。
        /// </summary>
        public FixedPoint64 radius
        {
            get => _radius;
            set
            {
                _radius = ValidateNonNegative(value, nameof(value));
                invRadius = _radius > FixedPoint64.Zero ? 1 / _radius : FixedPoint64.Zero;
                UpdateCollider();
            }
        }

        /// <summary>
        /// 获取本地半径的倒数；半径为零时返回零。
        /// </summary>
        public FixedPoint64 invRadius { get; set; }

        /// <summary>
        /// 获取应用三个坐标轴中最大绝对缩放后的世界半径。
        /// </summary>
        internal FixedPoint64 scaledRadius => _radius * FixedPointMath.Max(
            FixedPointMath.Max(absoluteScale.x, absoluteScale.z), absoluteScale.y);

        /// <summary>获取球形碰撞器类型。</summary>
        public override ColliderType colliderType => ColliderType.Sphere;

        /// <summary>根据世界半径更新轴对齐包围盒。</summary>
        internal override void UpdateAABB()
        {
            var width = scaledRadius;
            var halfSize = new FixedPointVector3(width, width, width);
            _min = position - halfSize;
            _max = position + halfSize;
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点的球形碰撞器集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpSphereColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的球形碰撞器集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpSphereColliders ??= new FPOctreeColliderSet<FPSphereCollider>(node);
            node.FpSphereColliders.Add(this);
            targetNode = node;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>根据当前对象的网格边界初始化本地半径。</summary>
        protected override void InitColliderSize()
        {
            var mesh = GetComponent<MeshFilter>();

            if (mesh == null || mesh.sharedMesh == null)
            {
                return;
            }

            var bounds = mesh.sharedMesh.bounds;
            _radius = FixedPointMath.Max(bounds.size.z * 0.5f,
                FixedPointMath.Max(bounds.size.x * 0.5f, bounds.size.y * 0.5f));
            invRadius = _radius > FixedPoint64.Zero ? 1 / _radius : FixedPoint64.Zero;
        }

        /// <summary>
        /// 在 Unity 场景视图中绘制球形碰撞器线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            Gizmos.DrawWireSphere(position.ToVector3(), scaledRadius.AsFloat());
        }
#endif
    }
}