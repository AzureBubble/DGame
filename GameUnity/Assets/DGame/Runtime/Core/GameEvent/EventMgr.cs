using System.Collections.Generic;

namespace DGame
{
    public class EventMgr
    {
        private class EventEntryData
        {
            public object InterfaceWrap;

            public EventEntryData(object interfaceWrap)
            {
                InterfaceWrap = interfaceWrap;
            }
        }

        private Dictionary<string, EventEntryData> m_eventEntryMap = new Dictionary<string, EventEntryData>();

        public EventDispatcher Dispatcher { get; private set; } = new EventDispatcher();

        public T GetInterface<T>()
        {
            string interfaceName = typeof(T).FullName;

            if (!string.IsNullOrEmpty(interfaceName) && m_eventEntryMap.TryGetValue(interfaceName, out var eventData))
            {
                return (T)eventData.InterfaceWrap;
            }
            return default(T);
        }

        public void RegWrapInterface<T>(T interfaceWrap)
        {
            if (interfaceWrap == null)
            {
                Debugger.Fatal("Wrap接口类型无效");
                return;
            }
            string interfaceName = typeof(T).FullName;
            RegWrapInterface(interfaceName, interfaceWrap);
        }

        public void RegWrapInterface(string interfaceName, object interfaceWrap)
        {
            if (interfaceWrap == null)
            {
                Debugger.Fatal("Wrap接口类型无效");
                return;
            }
            if (!string.IsNullOrEmpty(interfaceName))
            {
                var entry = new EventEntryData(interfaceWrap);
                m_eventEntryMap[interfaceName] = entry;
            }
        }

        public void Destroy()
        {
            m_eventEntryMap.Clear();
            Dispatcher?.Destroy();
        }
    }
}