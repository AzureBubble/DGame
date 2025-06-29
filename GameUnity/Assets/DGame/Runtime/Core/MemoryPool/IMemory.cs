namespace DGame
{
    public interface IMemory
    {
        /// <summary>
        /// 从对象池中取出的操作
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 清理内存 返回内存池
        /// </summary>
        void OnRecycle();
    }
}