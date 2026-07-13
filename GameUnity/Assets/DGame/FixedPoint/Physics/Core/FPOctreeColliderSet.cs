using System;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 管理八叉树节点内同一形状类别的碰撞器集合，并维护当前节点及其祖先节点的碰撞器计数。
    /// </summary>
    /// <remarks>
    /// 该集合继承自 <see cref="FPFastList{T}"/>，元素顺序不稳定，也不具备线程安全性。
    /// </remarks>
    /// <typeparam name="T">碰撞器元素类型；当前八叉树仅使用 <see cref="FPCollider"/> 的派生类型。</typeparam>
    public class FPOctreeColliderSet<T> : FPFastList<T> where T : FPCollider
    {
        /// <summary>
        /// 获取当前碰撞器集合所属的八叉树节点。
        /// </summary>
        private readonly FPOctreeNode m_node;

        /// <summary>
        /// 创建与指定八叉树节点关联的碰撞器集合。
        /// </summary>
        /// <param name="node">当前集合所属的八叉树节点。</param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> 为 <see langword="null"/>。</exception>
        public FPOctreeColliderSet(FPOctreeNode node)
        {
            m_node = node ?? throw new ArgumentNullException(nameof(node));
        }

        /// <summary>
        /// 从集合中移除指定碰撞器，并递减当前节点及全部祖先节点的碰撞器计数。
        /// </summary>
        /// <param name="t">需要移除的碰撞器。</param>
        /// <returns>成功移除返回 <see langword="true"/>；集合不包含该碰撞器时返回 <see langword="false"/>。</returns>
        public override bool Remove(T t)
        {
            if (!base.Remove(t)) return false;
            ChangeColliderCount(-1);
            return true;
        }

        /// <summary>
        /// 向集合添加指定碰撞器，并递增当前节点及全部祖先节点的碰撞器计数。
        /// </summary>
        /// <param name="t">需要添加的碰撞器。</param>
        /// <returns>成功添加返回 <see langword="true"/>；集合已包含该碰撞器时返回 <see langword="false"/>。</returns>
        public override bool Add(T t)
        {
            if (!base.Add(t)) return false;
            ChangeColliderCount(1);
            return true;
        }

        /// <summary>
        /// 清空集合，并从当前节点及全部祖先节点的碰撞器计数中扣除已清除的元素数量。
        /// </summary>
        public override void Clear()
        {
            var removedCount = Count;
            if (removedCount == 0)
            {
                return;
            }

            base.Clear();
            ChangeColliderCount(-removedCount);
        }

        private void ChangeColliderCount(int delta)
        {
            var currentNode = m_node;
            while (currentNode != null)
            {
                currentNode.colliderCount += delta;
                currentNode = currentNode.parentNode;
            }
        }
    }
}
