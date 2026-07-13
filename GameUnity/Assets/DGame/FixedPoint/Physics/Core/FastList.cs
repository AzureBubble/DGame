using System;

namespace DGame.FixedPoint
{
    /// <summary>
    /// 定义可存储在 <see cref="FastList{T}"/> 中的元素契约。
    /// </summary>
    internal interface FastListItem
    {
        /// <summary>
        /// 获取或设置元素在所属 <see cref="FastList{T}"/> 中的索引。
        /// </summary>
        public int index { get; set; }
    }

    /// <summary>
    /// 使用元素自身保存的索引提供按索引访问和无序快速删除的数组列表。
    /// </summary>
    /// <remarks>
    /// 一个元素同一时间只能属于一个 <see cref="FastList{T}"/>，调用方不得修改元素的
    /// <see cref="FastListItem.index"/>。删除元素时会用末尾元素填补空位，因此不保持元素顺序。
    /// 枚举期间不应修改列表；当前类型不是线程安全的。
    /// </remarks>
    /// <typeparam name="T">实现 <see cref="FastListItem"/> 的元素类型。</typeparam>
    internal class FastList<T> where T : class, FastListItem
    {
        private const int DefaultCapacity = 1000;
        private const int MinimumCapacity = 4;

        /// <summary>
        /// 保存列表元素的内部数组。
        /// </summary>
        private T[] m_items;

        /// <summary>
        /// 当前最后一个有效元素的索引；-1 表示列表为空。
        /// </summary>
        private int m_index = -1;

        /// <summary>
        /// 创建一个具有默认初始容量的空列表。
        /// </summary>
        public FastList() : this(DefaultCapacity)
        {
        }

        /// <summary>
        /// 创建一个具有指定初始容量的空列表。
        /// </summary>
        /// <param name="initialCapacity">初始容量；0 表示延迟到首次添加元素时分配。</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="initialCapacity"/> 小于 0。</exception>
        public FastList(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量不能小于 0。");
            }

            m_items = initialCapacity == 0 ? Array.Empty<T>() : new T[initialCapacity];
        }

        /// <summary>
        /// 将元素添加到列表末尾，并记录元素在列表中的索引。
        /// </summary>
        /// <param name="t">需要添加的元素；已属于任意快速列表的元素不会被重复添加。</param>
        /// <exception cref="ArgumentNullException"><paramref name="t"/> 为 <see langword="null"/>。</exception>
        public void Add(T t)
        {
            if (t == null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            if (t.index != -1)
            {
                return;
            }

            if (Count >= m_items.Length)
            {
                var newCapacity = m_items.Length == 0 ? MinimumCapacity : m_items.Length * 2;
                Array.Resize(ref m_items, newCapacity);
            }

            m_index++;
            m_items[m_index] = t;
            t.index = m_index;
        }

        /// <summary>
        /// 从列表中移除指定元素，并使用末尾元素填补被移除元素的位置。
        /// </summary>
        /// <param name="t">需要移除的元素。</param>
        public void Remove(T t)
        {
            if (t == null)
            {
                return;
            }

            var index = t.index;
            if (index < 0 || index > m_index || !ReferenceEquals(m_items[index], t))
            {
                return;
            }

            var lastIndex = m_index;
            if (index != lastIndex)
            {
                var lastItem = m_items[lastIndex];
                m_items[index] = lastItem;
                lastItem.index = index;
            }

            m_items[lastIndex] = null;
            t.index = -1;
            m_index--;
        }

        /// <summary>
        /// 移除全部元素、重置成员索引并保留当前数组容量以供复用。
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i <= m_index; i++)
            {
                m_items[i].index = -1;
            }

            Array.Clear(m_items, 0, Count);
            m_index = -1;
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
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "索引超出范围。");
                }

                return m_items[index];
            }
        }

        /// <summary>
        /// 获取当前有效元素数量。
        /// </summary>
        public int Count => m_index + 1;

        /// <summary>
        /// 返回用于遍历当前列表有效元素的独立枚举器。
        /// </summary>
        /// <returns>当前列表的结构体枚举器。</returns>
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        /// 用于遍历 <see cref="FastList{T}"/> 有效元素的零分配结构体枚举器。
        /// </summary>
        public struct Enumerator
        {
            private readonly FastList<T> m_list;
            private readonly int m_count;
            private int m_index;

            internal Enumerator(FastList<T> list)
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
            /// <returns>成功移动到有效元素时返回 <see langword="true"/>；到达列表末尾时返回 <see langword="false"/>。</returns>
            public bool MoveNext()
            {
                m_index++;
                return m_index < m_count;
            }
        }
    }
}
