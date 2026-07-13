using System;
using System.Collections.Generic;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 使用列表保存元素，并通过索引字典提供平均 O(1) 的添加、查找和无序移除操作。
    /// </summary>
    /// <remarks>
    /// 元素唯一性由 <see cref="EqualityComparer{T}.Default"/> 判定。移除元素时会用末尾元素填补空位，
    /// 因此不会保持元素顺序。枚举期间不应修改集合；当前类型不是线程安全的。
    /// </remarks>
    /// <typeparam name="T">列表中元素的类型。</typeparam>
    public class FPFastList<T>
    {
        /// <summary>
        /// 按索引保存列表元素。
        /// </summary>
        private List<T> m_items { get; } = new();

        /// <summary>
        /// 保存元素与其当前索引的映射。
        /// </summary>
        private Dictionary<T, int> m_itemDic { get; } = new();

        /// <summary>
        /// 获取当前有效元素数量。
        /// </summary>
        public int Count => m_items.Count;

        /// <summary>
        /// 移除指定元素，并使用末尾元素填补被移除元素的位置。
        /// </summary>
        /// <param name="t">需要移除的元素。</param>
        /// <returns>成功移除返回 <see langword="true"/>；列表不包含该元素时返回 <see langword="false"/>。</returns>
        public virtual bool Remove(T t)
        {
            if (!m_itemDic.TryGetValue(t, out var index))
            {
                return false;
            }

            var lastIndex = Count - 1;

            if (index != lastIndex)
            {
                var lastItem = m_items[lastIndex];
                m_items[index] = lastItem;
                m_itemDic[lastItem] = index;
            }

            m_items.RemoveAt(lastIndex);
            m_itemDic.Remove(t);
            return true;
        }

        /// <summary>
        /// 将尚未存在的元素添加到列表末尾。
        /// </summary>
        /// <param name="t">需要添加的元素。</param>
        /// <returns>成功添加返回 <see langword="true"/>；列表已包含等值元素时返回 <see langword="false"/>。</returns>
        public virtual bool Add(T t)
        {
            if (m_itemDic.ContainsKey(t))
            {
                return false;
            }

            var index = m_items.Count;
            m_items.Add(t);
            m_itemDic.Add(t, index);
            return true;
        }

        /// <summary>
        /// 判断列表是否包含与指定元素相等的元素。
        /// </summary>
        /// <param name="t">需要查找的元素。</param>
        /// <returns>列表包含该元素时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
        public bool Contains(T t)
        {
            return m_itemDic.ContainsKey(t);
        }

        /// <summary>
        /// 移除全部元素并重置元素数量。
        /// </summary>
        public virtual void Clear()
        {
            m_items.Clear();
            m_itemDic.Clear();
        }

        /// <summary>
        /// 获取指定索引处的元素。
        /// </summary>
        /// <param name="index">从零开始的元素索引。</param>
        /// <returns>指定索引处的元素。</returns>
        /// <exception cref="ArgumentOutOfRangeException">索引小于零或大于等于 <see cref="Count"/>。</exception>
        public T this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围。");
                }

                return m_items[index];
            }
        }

        /// <summary>
        /// 返回用于遍历当前有效元素的独立枚举器。
        /// </summary>
        /// <returns>当前集合的结构体枚举器。</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 用于遍历 <see cref="FPFastList{T}"/> 有效元素的零分配结构体枚举器。
        /// </summary>
        public struct Enumerator
        {
            private readonly FPFastList<T> m_list;
            private readonly int m_count;
            private int m_index;

            internal Enumerator(FPFastList<T> list)
            {
                m_list = list;
                m_count = list.Count;
                m_index = -1;
            }

            /// <summary>
            /// 获取枚举器当前位置的元素。
            /// </summary>
            public T Current => m_list.m_items[m_index];

            /// <summary>
            /// 将枚举器移动到下一个元素。
            /// </summary>
            /// <returns>成功移动到有效元素时返回 <see langword="true"/>；到达集合末尾时返回 <see langword="false"/>。</returns>
            public bool MoveNext()
            {
                m_index++;
                return m_index < m_count;
            }
        }
    }
}
