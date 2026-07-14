#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 中心轴始终与世界纵轴平行的定点数胶囊碰撞器。
    /// </summary>
    public class FPAACapsuleCollider : FPCollider
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
        /// 获取或设置胶囊的本地半径。
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
        /// 获取或设置胶囊包含两个半球端盖的本地总高度。
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
        /// 获取胶囊中心轴的起点。
        /// </summary>
        internal virtual FixedPointVector3 startPos => position - new FixedPointVector3(
            0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 获取胶囊中心轴的终点。
        /// </summary>
        internal virtual FixedPointVector3 endPos => position + new FixedPointVector3(
            0, FixedPointMath.Max(0, scaledHalfHeight - scaledRadius), 0);

        /// <summary>
        /// 获取轴对齐胶囊碰撞器类型。
        /// </summary>
        public override ColliderType colliderType => ColliderType.AACapsule;

        /// <summary>
        /// 根据胶囊当前世界尺寸和位置更新轴对齐包围盒。
        /// </summary>
        internal override void UpdateAABB()
        {
            var width = scaledRadius;
            var halfSize = new FixedPointVector3(width, FixedPointMath.Max(scaledHalfHeight, width), width);
            _min = position - halfSize;
            _max = position + halfSize;
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点的轴对齐胶囊集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpAACapsuleColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的轴对齐胶囊集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpAACapsuleColliders ??= new FPOctreeColliderSet<FPAACapsuleCollider>(node);
            node.FpAACapsuleColliders.Add(this);
            targetNode = node;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 根据当前对象的网格边界初始化本地半径和总高度。
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
            var boundHeight = Mathf.Max(2 * boundRadius, bounds.size.y);
            _radius = boundRadius;
            _height = boundHeight;
        }

        /// <summary>
        /// 在 Unity 场景视图中绘制胶囊碰撞器线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            FPCapsuleCollider.DrawWireCapsule(startPos.ToVector3(), endPos.ToVector3(), scaledRadius.AsFloat());
        }

        /// <summary>
        /// 绘制胶囊轴端点及轴对齐边界的调试标记。
        /// </summary>
        protected override void OnDrawDebugInfo()
        {
            Gizmos.DrawWireCube(startPos.ToVector3(), Vector3.one * 0.1f);
            Gizmos.DrawWireCube(endPos.ToVector3(), Vector3.one * 0.1f);
            Gizmos.DrawWireCube(min.ToVector3(), Vector3.one * 0.1f);
            Gizmos.DrawWireCube(max.ToVector3(), Vector3.one * 0.1f);
        }
#endif
    }
}