using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public static partial class MemoryPool
    {
        private static readonly Dictionary<Type, MemoryCollector> m_memoryCollectorPool = new Dictionary<Type, MemoryCollector>();

        /// <summary>
        /// 强制检查开关
        /// </summary>
        public static bool EnableStrictCheck { get; set; } = false;

        /// <summary>
        /// 获取内存收集对象数量
        /// </summary>
        public static int Capacity => m_memoryCollectorPool.Count;

        /// <summary>
        /// 获取所有的内存收集器信息
        /// </summary>
        /// <returns></returns>
        public static MemoryCollectorInfo[] GetAllMemoryCollectorInfos()
        {
            int index = 0;
            MemoryCollectorInfo[] results;

            lock (m_memoryCollectorPool)
            {
                results = new MemoryCollectorInfo[m_memoryCollectorPool.Count];

                foreach (var memoryCollector in m_memoryCollectorPool.Values)
                {
                    results[index++] = new MemoryCollectorInfo(memoryCollector.ClassType, memoryCollector.UnusedCount,
                        memoryCollector.UsingCount, memoryCollector.SpawnCount, memoryCollector.RecycleCount, memoryCollector.AddCount,
                        memoryCollector.RemoveCount, memoryCollector.Capacity);
                }
            }
            return results;
        }

        /// <summary>
        /// 清空所有的内存池
        /// </summary>
        public static void ClearAll()
        {
            lock (m_memoryCollectorPool)
            {
                foreach (var memoryCollector in m_memoryCollectorPool.Values)
                {
                    memoryCollector.Clear();
                }
                m_memoryCollectorPool.Clear();
            }
        }

        #region Spawn

        /// <summary>
        /// 从内存池中获取内存对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Spawn<T>() where T : class, IMemory, new()
        {
            var classType = typeof(T);
            InternalCheckClassTypeIsValid(classType);
            return GetMemoryCollector(classType)?.Spawn<T>();
        }

        /// <summary>
        /// 从内存池中获取内存对象
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public static IMemory Spawn(Type classType)
        {
            InternalCheckClassTypeIsValid(classType);
            return GetMemoryCollector(classType)?.Spawn();
        }

        #endregion

        #region Recycle

        /// <summary>
        /// 将内存对象归还内存池
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="DGameException"></exception>
        public static void Recycle<T>(List<T> memories) where T : class, IMemory
        {
            var classType = typeof(T);
            if (classType == null)
            {
                throw new DGameException("内存对象类型无效");
            }
            InternalCheckClassTypeIsValid(classType);
            var memoryCollector = GetMemoryCollector(classType);

            if (memoryCollector != null)
            {
                for (int i = 0; i < memories.Count; i++)
                {
                    memoryCollector.Recycle(memories[i]);
                }
            }
        }

        /// <summary>
        /// 将内存对象归还内存池
        /// </summary>
        /// <param name="memory"></param>
        /// <exception cref="DGameException"></exception>
        public static void Recycle(IMemory memory)
        {
            if (memory == null)
            {
                throw new DGameException("内存对象类型无效");
            }

            Type classType = memory.GetType();
            InternalCheckClassTypeIsValid(classType);
            GetMemoryCollector(classType)?.Recycle(memory);
        }

        #endregion

        #region Add MemoryCollector Obj Count

        /// <summary>
        /// 添加内存收集器的对象数量
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="count"></param>
        public static void AddMemoryCollectorCnt(Type classType, int count)
        {
            InternalCheckClassTypeIsValid(classType);
            GetMemoryCollector(classType)?.Add(count);
        }

        /// <summary>
        /// 添加内存收集器的对象数量
        /// </summary>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        public static void AddMemoryCollectorCnt<T>(int count) where T : class, IMemory, new()
        {
            var classType = typeof(T);
            InternalCheckClassTypeIsValid(classType);
            GetMemoryCollector(classType)?.Add<T>(count);
        }

        #endregion

        #region Remove MemoryCollector Obj Count

        /// <summary>
        /// 删除内存收集器的对象数量
        /// </summary>
        /// <param name="classType"></param>
        /// <param name="count"></param>
        public static void RemoveMemoryCollectorCnt(Type classType, int count)
        {
            InternalCheckClassTypeIsValid(classType);
            GetMemoryCollector(classType)?.Remove(count);
        }

        /// <summary>
        /// 删除内存收集器的对象数量
        /// </summary>
        /// <param name="count"></param>
        /// <typeparam name="T"></typeparam>
        public static void RemoveMemoryCollectorCnt<T>(int count) where T : class, IMemory
        {
            RemoveMemoryCollectorCnt(typeof(T), count);
        }

        #endregion

        #region Clear MemoryCollector

        /// <summary>
        /// 清空内存收集的内存对象
        /// </summary>
        /// <param name="classType">对象类型</param>
        public static void ClearMemoryCollector(Type classType)
        {
            InternalCheckClassTypeIsValid(classType);
            GetMemoryCollector(classType)?.Clear();
        }

        /// <summary>
        /// 清空内存收集的内存对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        public static void ClearMemoryCollector<T>() where T : class, IMemory
        {
            ClearMemoryCollector(typeof(T));
        }

        #endregion

        /// <summary>
        /// 内部检查内存池对象类型是否有效
        /// </summary>
        /// <param name="classType"></param>
        /// <exception cref="DGameException"></exception>
        public static void InternalCheckClassTypeIsValid(Type classType)
        {
            if (!EnableStrictCheck)
            {
                return;
            }

            if (classType == null)
            {
                throw new DGameException("内存池对象类型无效");
            }

            if (!classType.IsClass || classType.IsAbstract)
            {
                throw new DGameException("传入的内存池对象类型不能是抽象的或不是class类型");
            }

            if (!typeof(IMemory).IsAssignableFrom(classType))
            {
                throw new DGameException($"内存池对象类型{classType.FullName}无效");
            }
        }

        /// <summary>
        /// 从内存池中获取内存收集器
        /// </summary>
        /// <param name="classType">内存对象类型</param>
        /// <returns>内存收集器</returns>
        /// <exception cref="DGameException"></exception>
        private static MemoryCollector GetMemoryCollector(Type classType)
        {
            if (classType == null)
            {
                throw new DGameException("传入的内存池对象类型为空");
            }

            MemoryCollector memoryCollector = null;

            lock (m_memoryCollectorPool)
            {
                if (!m_memoryCollectorPool.TryGetValue(classType, out memoryCollector))
                {
                    memoryCollector = new MemoryCollector(classType);
                    m_memoryCollectorPool.Add(classType, memoryCollector);
                }
            }

            return memoryCollector;
        }
    }
}