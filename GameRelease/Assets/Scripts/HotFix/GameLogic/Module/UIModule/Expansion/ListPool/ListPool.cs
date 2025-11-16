using System.Collections.Generic;

namespace GameLogic
{
    internal static class ListPool<T>
    {
        private static readonly Pool<List<T>> m_listPool
            = new Pool<List<T>>(null, Clear);

        public static List<T> Get()
            => m_listPool.Get();

        public static void Recycle(List<T> element)
            => m_listPool.Recycle(element);

        private static void Clear(List<T> list)
        {
            list?.Clear();
        }
    }
}