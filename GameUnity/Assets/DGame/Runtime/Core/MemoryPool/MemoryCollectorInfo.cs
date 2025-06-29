using System;
using System.Runtime.InteropServices;

namespace DGame
{
    /// <summary>
    /// 内存收集器的信息
    /// LayoutKind.Auto 是 默认行为（CLR 自动优化内存布局）。
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public class MemoryCollectorInfo
    {
        /// <summary>
        /// 内存收集器的类型
        /// </summary>
        public Type ClassType { get; private set; }

        /// <summary>
        /// 未使用的内存对象数量
        /// </summary>
        public int UnusedCount{ get; private set; }

        /// <summary>
        /// 正在使用的内存对象数量
        /// </summary>
        public int UsingCount{ get; private set; }

        /// <summary>
        /// 对象池总容量
        /// </summary>
        public int Capacity{ get; private set; }

        public MemoryCollectorInfo(Type classType, int unusedCount, int usingCount, int capacity)
        {
            ClassType = classType;
            UnusedCount = unusedCount;
            UsingCount = usingCount;
            Capacity = capacity;
        }
    }
}