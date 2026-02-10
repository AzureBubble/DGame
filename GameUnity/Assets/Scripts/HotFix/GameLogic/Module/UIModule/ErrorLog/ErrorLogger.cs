using System;
using UnityEngine;

namespace GameLogic
{
    public class ErrorLogger : IDisposable
    {
        private readonly UIModule m_uiModule;

        public ErrorLogger(UIModule uiModule)
        {
            m_uiModule = uiModule;
            Application.logMessageReceived += LogHandler;
        }

        private void LogHandler(string condition, string stacktrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                string des = $"客户端报错, \n#内容#：---{condition} \n#位置#：---{stacktrace}";
                m_uiModule.ShowWindowAsync<LogUI>(des);
            }
        }

        public void Dispose()
        {
            Application.logMessageReceived -= LogHandler;
        }
    }
}