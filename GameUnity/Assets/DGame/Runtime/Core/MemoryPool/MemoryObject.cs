namespace DGame
{
    /// <summary>
    /// 内存池对象抽象基类
    /// </summary>
    public abstract class MemoryObject : IMemory
    {
        /// <summary>
        /// 从对象池中取出的操作
        /// </summary>
        public abstract void OnSpawn();

        /// <summary>
        /// 清理内存 返回内存池
        /// </summary>
        public abstract void OnRecycle();

        public static void Recycle(MemoryObject memoryObject)
        {
            MemoryPool.Recycle(memoryObject);
        }

        public static T Spawn<T>() where T : MemoryObject, new()
        {
            return MemoryPool.Spawn<T>();
        }
    }
}