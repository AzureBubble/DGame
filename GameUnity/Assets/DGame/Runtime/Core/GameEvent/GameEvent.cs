using System;

namespace DGame
{
    public class GameEvent
    {
        public static EventMgr EventMgr { get; } = new EventMgr();

        #region AddEventListener

        public static bool AddEventListener(int eventID, Action handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T>(int eventID, Action<T> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        public static bool AddEventListener<T1, T2, T3, T4, T5, T6>(int eventID, Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                return EventMgr.Dispatcher.AddEventListener(eventID, handler);
            }

            return false;
        }

        #endregion

        #region RemoveEventListener

        public static void RemoveEventListener(int eventID, Delegate handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener(int eventID, Action handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T>(int eventID, Action<T> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2>(int eventID, Action<T1, T2> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3>(int eventID, Action<T1, T2, T3> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4>(int eventID, Action<T1, T2, T3, T4> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4, T5>(int eventID, Action<T1, T2, T3, T4, T5> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        public static void RemoveEventListener<T1, T2, T3, T4, T5, T6>(int eventID,
            Action<T1, T2, T3, T4, T5, T6> handler)
        {
            if (EventMgr != null && EventMgr.Dispatcher != null)
            {
                EventMgr.Dispatcher.RemoveEventListener(eventID, handler);
            }
        }

        #endregion

        #region 分发消息接口

        public static T Get<T>()
        {
            return EventMgr.GetInterface<T>();
        }

        #endregion

        public void Destroy()
        {
            EventMgr?.Destroy();
        }
    }
}