using System;
using System.Collections.Generic;

namespace GameLogic
{
    internal class Pool<T> where T : new()
    {
        private readonly Stack<T> m_stack = new Stack<T>();

        private readonly Action<T> m_actionOnGet;

        private readonly Action<T> m_actionOnRecycle;

        public int count { get; private set; }
        public int activeCount => count - inactiveCount;
        public int inactiveCount => m_stack.Count;

        public Pool(Action<T> actionOnGet, Action<T> actionOnRecycle)
        {
            m_actionOnGet = actionOnGet;
            m_actionOnRecycle = actionOnRecycle;
        }

        public T Get()
        {
            T element;
            if (m_stack.Count == 0)
            {
                element = new T();
                count++;
            }
            else
            {
                element = m_stack.Pop();
            }
            m_actionOnGet?.Invoke(element);
            return element;
        }

        public void Recycle(T element)
        {
            if(element == null) return;
            if (m_stack.Count > 0 && ReferenceEquals(m_stack.Peek(), element))
            {
                throw new Exception("Internal error. Trying to destroy object that is already released to pool.");
            }

            m_actionOnRecycle?.Invoke(element);
            m_stack.Push(element);
        }

        public void Clear()
        {
            m_stack.Clear();
            count = 0;
        }
    }
}