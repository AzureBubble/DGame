using System;
using DGame;

namespace GameLogic
{
    public sealed class DefaultBLoggerHelper : GameBattle.ILogger
    {
        private bool m_enableDebugLog = true;
        private bool m_enableInfoLog = true;  
        private bool m_enableWarningLog = true;
        private bool m_enableErrorLog = true;
        
        public void Log(object message)
        {
            if (m_enableDebugLog)
            {
                DLogger.Log(message);
            }
        }

        public void Info(object message)
        {
            if (m_enableInfoLog)
            {
                DLogger.Info(message);
            }
        }

        public void Warning(object message)
        {
            if (m_enableWarningLog)
            {
                DLogger.Warning(message);
            }
        }

        public void Error(object message)
        {
            if (m_enableErrorLog)
            {
                DLogger.Error(message);
            }
        }

        public void Exception(Exception exception)
        {
            if (m_enableErrorLog)
            {
                DLogger.Fatal(exception);
            }
        }

        public void SetLogLevel(bool debug, bool info, bool warning, bool error)
        {
            m_enableDebugLog = debug;
            m_enableInfoLog = info;
            m_enableWarningLog = warning;
            m_enableErrorLog = error;
        }
    }
}