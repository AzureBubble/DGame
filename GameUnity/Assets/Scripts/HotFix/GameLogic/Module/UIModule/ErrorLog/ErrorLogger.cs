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
            if (type == LogType.Exception)
            {
                string des = $"客户端报错, \n#内容#：---{condition} \n#位置#：---{stacktrace}";
            }
        }

        public void Dispose()
        {
            Application.logMessageReceived -= LogHandler;
        }
    }
}