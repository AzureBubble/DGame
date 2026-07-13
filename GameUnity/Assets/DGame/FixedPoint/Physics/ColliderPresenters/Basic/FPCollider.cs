#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif
using System;

namespace DGame.FixedPoint
{
    public enum ColliderType
    {
        Sphere,
        AABB,
        OBB,
        Capsule,
        Cylinder,
        AACapsule,
        Mesh,
        CharacterController
    }

    /// <summary>
    /// 为定点碰撞器提供抽象基类。纯逻辑部分双端共用；Unity 表现/序列化部分见 FPCollider.Unity.cs。
    /// 通过 <see cref="context"/> 引用其所属的物理上下文（构造或注入时传入），不依赖静态单例。
    /// </summary>
    public abstract partial class FPCollider
    {
        public FPTransform fpTransform; // 固定点变换的引用，用于物理计算。

        /// <summary>
        /// 所属物理上下文。Unity 侧 Awake 从 FPPhysicsPresenter 取，服务端显式注入。
        /// </summary>
        public FPPhysicsContext context { get; internal set; }

        protected FixedPointVector3 _min; // 碰撞器的最小边界。
        protected FixedPointVector3 _max; // 碰撞器的最大边界。

        /// <summary>
        /// 获取碰撞器的最小边界向量。
        /// </summary>
        public FixedPointVector3 min => _min;

        /// <summary>
        /// 获取碰撞器的最大边界向量。
        /// </summary>
        public FixedPointVector3 max => _max;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        public FixedPointVector3 center; // 碰撞器的中心，用于定位。

        /// <summary>
        /// 使用其中心和当前位置计算碰撞器的位置。
        /// </summary>
        public FixedPointVector3 position => fpTransform.Position(center);

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        public bool isDynamic; // 指示碰撞器是动态的还是静态的。

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        public int layer; // 碰撞器所属的物理层。

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        public bool isTrigger; // 确定碰撞器是否充当触发器。

        public Action<FPCollision> onCharacterCollide; // 与角色碰撞器发生碰撞的事件。

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        public FixedPoint64 rebound; // 碰撞器的反弹性。

        internal int castIndex { get; set; } // 在碰撞检测算法中的优化索引。

        /// <summary>
        /// 指定碰撞器的类型（例如，Sphere, AABB, OBB等）。
        /// </summary>
        public abstract ColliderType colliderType { get; }

        protected FPOctreeNode targetNode;

#if !UNITY_2021_3_OR_NEWER
        // 服务端无 MonoBehaviour，自行声明 enabled（内核 ~42 处依赖）。
        public bool enabled { get; set; } = true;
#endif

        public void SetFpPhysicsContext(FPPhysicsContext physicsContext)
        {
            this.context = physicsContext;
        }

        /// <summary>
        /// 更新碰撞器在空间划分结构中的位置和大小。
        /// 纯逻辑：以 context.fpOctree 是否就绪作为生效条件（替代旧 Application.isPlaying 判断）。
        /// </summary>
        internal void UpdateCollider()
        {
            if (context?.fpOctree == null) return;

            UpdateAABB();
            var octree = context.fpOctree;
            var node = octree.root;
            if (targetNode != null)
            {
                // 如果碰撞器仍然包含在当前节点内，只需要以当前节点为根进行搜索。
                if (FixedPointIntersection.IsAABBInsideAABB(min, max, targetNode.fixedPointAABB.Min,
                        targetNode.fixedPointAABB.Max))
                {
                    node = targetNode;
                }
                RemoveFromImpactNotes();
            }

            while (true)
            {
                if (node.nodes == null)
                {
                    AddToImpactNote(node);
                    break;
                }
                var inside = false;
                foreach (var item in node.nodes)
                {
                    if (FixedPointIntersection.IsAABBInsideAABB(min, max,item.fixedPointAABB.Min, item.fixedPointAABB.Max))
                    {
                        node = item;
                        inside = true;
                        break;
                    }
                }
                // 如果没有被任何子节点完全包含，但被父节点包围，则与这个父节点关联。
                if (inside) continue;
                AddToImpactNote(node);
                break;
            }
        }

        /// <summary>
        /// 更新碰撞器的轴对齐包围盒（AABB）的虚拟方法。
        /// </summary>
        internal abstract void UpdateAABB();

        /// <summary>
        /// 将碰撞器从当前所属八叉树节点移除，并同步更新节点碰撞器计数。
        /// </summary>
        internal void DetachFromOctree()
        {
            RemoveFromImpactNotes();
        }

        protected abstract void RemoveFromImpactNotes();

        protected abstract void AddToImpactNote(FPOctreeNode node);
    }
}
