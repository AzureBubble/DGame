using System;
using UnityEngine;

namespace DGame
{
    public sealed partial class DebuggerDriver
    {
        public sealed class LogNode : IMemory
        {
            private DateTime m_logTime = default(DateTime);
            private int m_logFrameCount;
            private LogType m_logType = LogType.Error;
            private string m_logMessage;
            private string m_stackTrace;

            public DateTime LogTime => m_logTime;
            public int LogFrameCount => m_logFrameCount;
            public LogType LogType => m_logType;
            public string LogMessage => m_logMessage;
            public string StackTrace => m_stackTrace;

            public static LogNode Create(LogType logType, string logMessage, string stackTrace)
            {
                LogNode logNode = MemoryPool.Spawn<LogNode>();
                logNode.m_logTime = DateTime.Now;
                logNode.m_logFrameCount = Time.frameCount;
                logNode.m_logType = logType;
                logNode.m_logMessage = logMessage;
                logNode.m_stackTrace = stackTrace;
                return logNode;
            }

            public void OnSpawnFromMemoryPool()
            {
            }

            public void OnRecycleToMemoryPool()
            {
                m_logTime = default(DateTime);
                m_logFrameCount = 0;
                m_logType = LogType.Error;
                m_logMessage = null;
                m_stackTrace = null;
            }
        }

        public sealed class PrintLogNode
        {
            private readonly DateTime m_logTime = default(DateTime);
            private readonly int m_logFrameCount;
            private readonly LogType m_logType = LogType.Error;
            private readonly string m_logMessage;
            private readonly string m_stackTrace;

            public DateTime LogTime => m_logTime;
            public int LogFrameCount => m_logFrameCount;
            public LogType LogType => m_logType;
            public string LogMessage => m_logMessage;
            public string StackTrace => m_stackTrace;

            public PrintLogNode(LogType logType, string logMessage, string stackTrace)
            {
                m_logTime = DateTime.Now;
                m_logFrameCount = Time.frameCount;
                m_logType = logType;
                m_logMessage = logMessage;
                m_stackTrace = stackTrace;
            }
        }
    }
}