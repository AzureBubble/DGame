#if UNITY_2021_3_OR_NEWER
using UnityEngine;
#endif
using System;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 定点数碰撞器类型。
    /// </summary>
    public enum ColliderType
    {
        /// <summary>球形碰撞器。</summary>
        Sphere,

        /// <summary>轴对齐包围盒碰撞器。</summary>
        AABB,

        /// <summary>有向包围盒碰撞器。</summary>
        OBB,

        /// <summary>可旋转胶囊碰撞器。</summary>
        Capsule,

        /// <summary>圆柱碰撞器。</summary>
        Cylinder,

        /// <summary>轴对齐胶囊碰撞器。</summary>
        AACapsule,

        /// <summary>网格碰撞器。</summary>
        Mesh,

        /// <summary>角色控制器碰撞器。</summary>
        CharacterController
    }

    /// <summary>
    /// 为定点碰撞器提供抽象基类。纯逻辑部分双端共用；Unity 表现/序列化部分见 FPCollider.Unity.cs。
    /// 通过 <see cref="context"/> 引用其所属的物理上下文（构造或注入时传入），不依赖静态单例。
    /// </summary>
    public abstract partial class FPCollider
    {
        /// <summary>碰撞器使用的定点数变换。</summary>
        public FPTransform fpTransform;

        /// <summary>
        /// 所属物理上下文。Unity 侧 Awake 从 FPPhysicsPresenter 取，服务端显式注入。
        /// </summary>
        public FPPhysicsContext context { get; internal set; }

        /// <summary>轴对齐包围盒的最小点。</summary>
        protected FixedPointVector3 _min;

        /// <summary>轴对齐包围盒的最大点。</summary>
        protected FixedPointVector3 _max;

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
        /// <summary>碰撞器相对于定点数变换的位置偏移。</summary>
        public FixedPointVector3 center;

        /// <summary>
        /// 使用其中心和当前位置计算碰撞器的位置。
        /// </summary>
        public FixedPointVector3 position => fpTransform.Position(center);

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>碰撞器是否会在模拟过程中移动。</summary>
        public bool isDynamic;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>碰撞器所属的物理层编号。</summary>
        public int layer;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>碰撞器是否仅产生触发结果而不参与实体阻挡。</summary>
        public bool isTrigger;

        /// <summary>角色控制器与该碰撞器发生接触时触发的回调。</summary>
        public Action<FPCollision> onCharacterCollide;

#if UNITY_2021_3_OR_NEWER
        [SerializeField]
#endif
        /// <summary>碰撞器的反弹系数。</summary>
        public FixedPoint64 rebound;

        /// <summary>空间查询使用的去重编号。</summary>
        internal int castIndex { get; set; }

        /// <summary>
        /// 获取当前碰撞器类型。
        /// </summary>
        public abstract ColliderType colliderType { get; }

        /// <summary>当前收纳该碰撞器的八叉树节点。</summary>
        protected FPOctreeNode targetNode;

#if !UNITY_2021_3_OR_NEWER
        /// <summary>服务端碰撞器是否启用。</summary>
        public bool enabled { get; set; } = true;
#endif

        /// <summary>
        /// 注入碰撞器所属的物理上下文。
        /// </summary>
        /// <param name="physicsContext">物理上下文；传入 <see langword="null"/> 可解除关联。</param>
        public void SetFpPhysicsContext(FPPhysicsContext physicsContext)
        {
            this.context = physicsContext;
        }

        /// <summary>
        /// 更新碰撞器在空间划分结构中的位置和大小。
        /// 仅在物理上下文及其八叉树就绪时执行更新。
        /// </summary>
        internal void UpdateCollider()
        {
            if (context?.fpOctree == null)
            {
                return;
            }

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
                    if (FixedPointIntersection.IsAABBInsideAABB(min, max, item.fixedPointAABB.Min,
                            item.fixedPointAABB.Max))
                    {
                        node = item;
                        inside = true;
                        break;
                    }
                }

                // 如果没有被任何子节点完全包含，但被父节点包围，则与这个父节点关联。
                if (inside)
                {
                    continue;
                }

                AddToImpactNote(node);
                break;
            }
        }

        /// <summary>
        /// 根据当前形状和变换更新轴对齐包围盒。
        /// </summary>
        internal abstract void UpdateAABB();

        /// <summary>
        /// 将碰撞器从当前所属八叉树节点移除，并同步更新节点碰撞器计数。
        /// </summary>
        internal void DetachFromOctree()
        {
            RemoveFromImpactNotes();
        }

        /// <summary>
        /// 将碰撞器从当前八叉树节点中的对应形状集合移除。
        /// </summary>
        protected abstract void RemoveFromImpactNotes();

        /// <summary>
        /// 将碰撞器加入指定八叉树节点中的对应形状集合。
        /// </summary>
        /// <param name="node">目标八叉树节点。</param>
        protected abstract void AddToImpactNote(FPOctreeNode node);

        /// <summary>
        /// 获取定点数变换各缩放分量的绝对值。
        /// </summary>
        protected FixedPointVector3 absoluteScale => new FixedPointVector3(
            FixedPointMath.Abs(fpTransform.scale.x),
            FixedPointMath.Abs(fpTransform.scale.y),
            FixedPointMath.Abs(fpTransform.scale.z));

        /// <summary>
        /// 验证尺寸值不小于零。
        /// </summary>
        /// <param name="value">待验证的尺寸。</param>
        /// <param name="parameterName">异常中使用的参数名称。</param>
        /// <returns>原始尺寸值。</returns>
        /// <exception cref="ArgumentOutOfRangeException">尺寸值小于零。</exception>
        protected static FixedPoint64 ValidateNonNegative(FixedPoint64 value, string parameterName)
        {
            if (value < FixedPoint64.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName, "碰撞器尺寸不能小于零。");
            }

            return value;
        }

        /// <summary>
        /// 验证向量的每个尺寸分量均不小于零。
        /// </summary>
        /// <param name="value">待验证的尺寸向量。</param>
        /// <param name="parameterName">异常中使用的参数名称。</param>
        /// <returns>原始尺寸向量。</returns>
        /// <exception cref="ArgumentOutOfRangeException">任意尺寸分量小于零。</exception>
        protected static FixedPointVector3 ValidateNonNegative(FixedPointVector3 value, string parameterName)
        {
            if (value.x < FixedPoint64.Zero || value.y < FixedPoint64.Zero || value.z < FixedPoint64.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName, "碰撞器尺寸分量不能小于零。");
            }

            return value;
        }
    }
}