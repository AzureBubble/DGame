using System;
using System.Collections.Generic;

namespace DGame
{
    public class GameEventMgr : IMemory
    {
        private class UIEventRecord
        {
            public int EventID;
            public Delegate Handler;

            public UIEventRecord(int eventID, Delegate handler)
            {
                EventID = eventID;
                Handler = handler;
            }
        }

        private readonly List<UIEventRecord> m_eventRecords = new List<UIEventRecord>();
        private readonly bool m_isInitialized = false;

        public GameEventMgr()
        {
            if (m_isInitialized)
            {
                return;
            }

            m_isInitialized = true;
            m_eventRecords = new List<UIEventRecord>();
        }

        public void OnSpawn()
        {
        }

        public void OnRecycle()
        {
            if (!m_isInitialized)
            {
                return;
            }

            for (int i = 0; i < m_eventRecords.Count; i++)
            {
                var record = m_eventRecords[i];
                GameEvent.RemoveEventListener(record.EventID, record.Handler);
            }

            m_eventRecords.Clear();
        }

        #region UIEvent

        public void AddUIEventImp(int eventID, Delegate handler)
        {
            UIEventRecord record = new UIEventRecord(eventID, handler);
            m_eventRecords.Add(record);
        }

        public void AddUIEvent(int eventID, Action handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T>(int eventID, Action<T> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        public void AddUIEvent<T1, T2, T3, T4, T5, T6>(int eventID, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (handler == null)
            {
                return;
            }

            if (GameEvent.AddEventListener(eventID, handler))
            {
                AddUIEventImp(eventID, handler);
            }
        }

        #endregion
    }
}