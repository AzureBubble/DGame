using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 内存池收集器
    /// </summary>
    internal sealed class MemoryCollector
    {
        private readonly Queue<IMemory> m_collector;
        public Type ClassType { get; private set; }
        public int UsingCount { get; private set; }

        /// <summary>
        /// 当前未在使用中的容量
        /// </summary>
        public int Count => m_collector == null ? 0 : m_collector.Count;

        /// <summary>
        /// 对象池总容量 = 当前未在使用中的容量 + 正在使用中的数量
        /// </summary>
        public int Capacity => m_collector == null ? 0 : m_collector.Count + UsingCount;

        #region Constructor

        public MemoryCollector(Type classType)
        {
            m_collector = new Queue<IMemory>();
            InitCollector(classType);
        }

        public MemoryCollector(Type classType, int count)
        {
            m_collector = new Queue<IMemory>(count);
            InitCollector(classType);
        }

        private void InitCollector(Type classType)
        {
            ClassType = classType;
            UsingCount = 0;
        }

        #endregion

        #region Spawn

        public T Spawn<T>() where T : class, IMemory, new()
        {
            if (!typeof(IMemory).IsAssignableFrom(ClassType))
            {
                throw new DGameException($"内存池类型不匹配，无法取出对象：{ClassType.Name}");
            }

            UsingCount++;

            T memory;
            lock (m_collector)
            {
                if (m_collector.Count > 0)
                {
                    memory = (T)m_collector.Dequeue();
                }
                else
                {
                    memory = new T();
                }
            }
            memory.OnSpawn();
            return memory;
        }

        public IMemory Spawn()
        {
            UsingCount++;
            IMemory memory;
            lock (m_collector)
            {
                if (m_collector.Count > 0)
                {
                    return m_collector.Dequeue();
                }
                else
                {
                    memory = Activator.CreateInstance(ClassType) as IMemory;
                }
            }

            memory?.OnSpawn();
            return memory;
        }

        #endregion

        #region Recycle

        public void Recycle(IMemory memory)
        {
            memory.OnRecycle();
            lock (m_collector)
            {
                if (MemoryPool.EnableStrictCheck && m_collector.Contains(memory))
                {
                    throw new DGameException("内存对象已被释放过");
                }

                m_collector.Enqueue(memory);
            }

            UsingCount--;
        }

        #endregion

        #region Add

        public void Add<T>(int count) where T : class, IMemory, new()
        {
            if (!typeof(IMemory).IsAssignableFrom(ClassType))
            {
                throw new DGameException($"类型不匹配：{typeof(T).Name} != {ClassType.Name}");
            }

            lock (m_collector)
            {
                while (count-- > 0)
                {
                    m_collector.Enqueue(new T());
                }
            }
        }

        public void Add(int count)
        {
            lock (m_collector)
            {
                while (count-- > 0)
                {
                    m_collector.Enqueue(Activator.CreateInstance(ClassType) as IMemory);
                }
            }
        }

        #endregion

        #region Remove

        public void Remove(int count)
        {
            lock (m_collector)
            {
                count = Capacity < count ? Capacity : count;

                while (count-- > 0)
                {
                    m_collector.Dequeue();
                }
            }
        }

        #endregion

        public void Clear()
        {
            lock (m_collector)
            {
                m_collector.Clear();
            }
        }
    }
}