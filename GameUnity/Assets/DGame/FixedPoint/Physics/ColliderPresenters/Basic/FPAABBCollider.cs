#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用定点数表示的轴对齐包围盒碰撞器。
    /// </summary>
    public class FPAABBCollider : FPCollider
    {
#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>未应用变换缩放的本地尺寸。</summary>
        protected FixedPointVector3 _size;

        /// <summary>
        /// 获取应用变换缩放后的世界尺寸，或设置未缩放的本地尺寸。
        /// </summary>
        public FixedPointVector3 size
        {
            get => FixedPointVector3.Scale(_size, absoluteScale);
            set
            {
                _size = ValidateNonNegative(value, nameof(value));
                UpdateCollider();
            }
        }

        /// <summary>
        /// 获取应用变换缩放后的世界半尺寸，或设置未缩放的本地半尺寸。
        /// </summary>
        public FixedPointVector3 halfSize
        {
            get => size * 0.5;
            set
            {
                _size = ValidateNonNegative(value, nameof(value)) * 2;
                UpdateCollider();
            }
        }

        /// <summary>
        /// 获取轴对齐包围盒碰撞器类型。
        /// </summary>
        public override ColliderType colliderType => ColliderType.AABB;

        /// <summary>
        /// 根据当前位置和世界半尺寸更新轴对齐包围盒。
        /// </summary>
        internal override void UpdateAABB()
        {
            var pos = position;
            _min = pos - halfSize;
            _max = pos + halfSize;
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点的轴对齐包围盒集合中移除。
        /// </summary>
        protected override void RemoveFromImpactNotes()
        {
            targetNode?.FpAABBColliders.Remove(this);
            targetNode = null;
        }

        /// <summary>
        /// 将碰撞器加入指定八叉树节点的轴对齐包围盒集合。
        /// </summary>
        protected override void AddToImpactNote(FPOctreeNode node)
        {
            node.FpAABBColliders ??= new FPOctreeColliderSet<FPAABBCollider>(node);
            node.FpAABBColliders.Add(this);
            targetNode = node;
        }

#if UNITY_2021_3_OR_NEWER
        /// <summary>
        /// 根据当前对象的网格边界初始化本地尺寸。
        /// </summary>
        protected override void InitColliderSize()
        {
            var mesh = GetComponent<MeshFilter>();

            if (mesh == null || mesh.sharedMesh == null)
            {
                return;
            }

            var bounds = mesh.sharedMesh.bounds;
            size = new FixedPointVector3(bounds.size);
        }

        /// <summary>
        /// 在 Unity 场景视图中绘制轴对齐包围盒线框。
        /// </summary>
        protected override void OnDrawGizmosEditor()
        {
            Gizmos.DrawWireCube(position.ToVector3(), size.ToVector3());
        }

        /// <summary>
        /// 绘制包围盒最小点和最大点的调试标记。
        /// </summary>
        protected override void OnDrawDebugInfo()
        {
            var color = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(min.ToVector3(), DebugCubeSize);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(max.ToVector3(), DebugCubeSize);
            Gizmos.color = color;
        }
#endif
    }
}