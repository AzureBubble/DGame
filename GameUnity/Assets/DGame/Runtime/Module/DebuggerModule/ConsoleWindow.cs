using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        [Serializable]
        public class ConsoleWindow : IDebuggerWindow
        {
            private readonly Queue<LogNode> m_logNodeQueue = new Queue<LogNode>();
            private readonly List<PrintLogNode> m_logNodes = new List<PrintLogNode>();

            private Vector2 m_logScrollPosition = Vector2.zero;
            private Vector2 m_stackTraceScrollPosition = Vector2.zero;
            private int m_infoCount = 0;
            private int m_warningCount = 0;
            private int m_errorCount = 0;
            private int m_fatalCount = 0;
            private LogNode m_selectedLogNode = null;
            private bool m_lastLockScroll = true;
            private bool m_lastInfoFilter = true;
            private bool m_lastWarningFilter = true;
            private bool m_lastErrorFilter = true;
            private bool m_lastFatalFilter = true;

            [SerializeField] private bool needPrintLog = false;
            [SerializeField] private bool lockScroll = true;
            [SerializeField] private int maxLine = 100;
            [SerializeField] private bool infoFilter = true;
            [SerializeField] private bool warningFilter = true;
            [SerializeField] private bool errorFilter = true;
            [SerializeField] private bool fatalFilter = true;
            [SerializeField] private Color infoColor = Color.white;
            [SerializeField] private Color warningColor = Color.yellow;
            [SerializeField] private Color errorColor = Color.red;
            [SerializeField] private Color fatalColor = new Color(0.7f, 0.2f, 0.2f);

            public bool LockScroll { get => lockScroll; set => lockScroll = value; }
            public int MaxLine { get => maxLine; set => maxLine = value; }
            public bool InfoFilter { get => infoFilter; set => infoFilter = value; }
            public bool WarningFilter { get => warningFilter; set => warningFilter = value; }
            public bool ErrorFilter { get => errorFilter; set => errorFilter = value; }
            public bool FatalFilter { get => fatalFilter; set => fatalFilter = value; }

            public int InfoCount => m_infoCount;
            public int WarningCount => m_warningCount;
            public int ErrorCount => m_errorCount;
            public int FatalCount => m_fatalCount;

            public Color InfoColor { get => infoColor; set => infoColor = value; }
            public Color WarningColor { get => warningColor; set => warningColor = value; }
            public Color ErrorColor { get => errorColor; set => errorColor = value; }
            public Color FatalColor { get => fatalColor; set => fatalColor = value; }

            public void Initialize(params object[] args)
            {
                Application.logMessageReceived += OnLogMessageReceive;
                lockScroll = m_lastLockScroll = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_LOCK_SCROLL);
                infoFilter = m_lastInfoFilter =  Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_INFO_FILTER);
                warningFilter = m_lastWarningFilter = Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_WARNING_FILTER);
                fatalFilter = m_lastFatalFilter =  Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_FATAL_FILTER);
                errorFilter = m_lastFatalFilter =  Utility.PlayerPrefsUtil.GetBool(Constant.CONSOLE_WINDOW_ERROR_FILTER);
            }

            private void OnLogMessageReceive(string logMessage, string stacktrace, LogType type)
            {
                if (type == LogType.Assert)
                {
                    type = LogType.Error;
                }

                if (needPrintLog)
                {
                    PrintLogNode logNode = new PrintLogNode(type, logMessage, stacktrace);
                    m_logNodes.Add(logNode);
                }

                m_logNodeQueue.Enqueue(LogNode.Create(type, logMessage, stacktrace));
                while (m_logNodeQueue.Count > maxLine)
                {
                    MemoryPool.Release(m_logNodeQueue.Dequeue());
                }
            }

            public void OnEnter()
            {
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (m_lastLockScroll != lockScroll)
                {
                    m_lastLockScroll = lockScroll;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_LOCK_SCROLL, m_lastLockScroll);
                }

                if (m_lastInfoFilter != infoFilter)
                {
                    m_lastInfoFilter = infoFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_INFO_FILTER, m_lastInfoFilter);
                }

                if (m_lastWarningFilter != warningFilter)
                {
                    m_lastWarningFilter = warningFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_WARNING_FILTER, m_lastWarningFilter);
                }

                if (m_lastErrorFilter != errorFilter)
                {
                    m_lastErrorFilter = errorFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_ERROR_FILTER, m_lastWarningFilter);
                }

                if (m_lastFatalFilter != fatalFilter)
                {
                    m_lastFatalFilter = fatalFilter;
                    Utility.PlayerPrefsUtil.SetBool(Constant.CONSOLE_WINDOW_FATAL_FILTER, m_lastFatalFilter);
                }
            }

            public void OnDraw()
            {
                RefreshCount();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button("Clear All", GUILayout.Width(100f)))
                    {
                        Clear();
                    }

                    if (needPrintLog && GUILayout.Button("Save Logs", GUILayout.Width(100f)))
                    {
                        SaveLogsToDevice();
                    }

                    lockScroll = GUILayout.Toggle(lockScroll, "Lock Scroll", GUILayout.Width(90f));
                    GUILayout.FlexibleSpace();
                    infoFilter = GUILayout.Toggle(infoFilter, Utility.StringUtil.Format("Info ({0})", m_infoCount), GUILayout.Width(90f));
                    warningFilter = GUILayout.Toggle(warningFilter, Utility.StringUtil.Format("Warning ({0})", m_warningCount), GUILayout.Width(90f));
                    errorFilter = GUILayout.Toggle(errorFilter, Utility.StringUtil.Format("Error ({0})", m_errorCount), GUILayout.Width(90f));
                    fatalFilter = GUILayout.Toggle(fatalFilter, Utility.StringUtil.Format("Fatal ({0})", m_fatalCount), GUILayout.Width(90f));
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical("box");
                {
                    if (lockScroll)
                    {
                        m_logScrollPosition.y = float.MaxValue;
                    }
                    m_logScrollPosition = GUILayout.BeginScrollView(m_logScrollPosition);
                    {
                        bool selected = false;

                        foreach (var logNode in m_logNodeQueue)
                        {
                            switch (logNode.LogType)
                            {
                                case LogType.Error:
                                    if (!errorFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Warning:
                                    if (!warningFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Log:
                                    if (!infoFilter)
                                    {
                                        continue;
                                    }
                                    break;

                                case LogType.Exception:
                                    if (!fatalFilter)
                                    {
                                        continue;
                                    }
                                    break;
                            }
                            if (GUILayout.Toggle(m_selectedLogNode == logNode, GetLogString(logNode)))
                            {
                                selected = true;
                                if (m_selectedLogNode != logNode)
                                {
                                    m_selectedLogNode = logNode;
                                    m_stackTraceScrollPosition = Vector2.zero;
                                }
                            }
                        }

                        if (!selected)
                        {
                            m_selectedLogNode = null;
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical("box");
                {
                    m_stackTraceScrollPosition = GUILayout.BeginScrollView(m_stackTraceScrollPosition, GUILayout.Height(100f));

                    if (m_selectedLogNode != null)
                    {
                        Color32 color = GetLogStringColor(m_selectedLogNode.LogType);

                        if (GUILayout.Button(
                                Utility.StringUtil.Format(
                                    Constant.CONSOLE_WINDOW_LOG_DETAILS_MESSAGE_STRING, color.r, color.g,
                                    color.b, color.a, m_selectedLogNode.LogMessage, m_selectedLogNode.StackTrace,
                                    Environment.NewLine), "label"))
                        {
                            CopyToClipboard(Utility.StringUtil.Format("{0}{2}{2}{1}", m_selectedLogNode.LogMessage,
                                m_selectedLogNode.StackTrace, Environment.NewLine));
                        }
                    }
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }

            public void OnExit()
            {
            }

            public void OnDestroy()
            {
                Application.logMessageReceived -= OnLogMessageReceive;
                Clear();
            }

            private void Clear()
            {
                m_logNodeQueue.Clear();
                m_logNodes.Clear();
            }

            public void RefreshCount()
            {
                m_infoCount = 0;
                m_warningCount = 0;
                m_errorCount = 0;
                m_fatalCount = 0;
                foreach (LogNode logNode in m_logNodeQueue)
                {
                    switch (logNode.LogType)
                    {
                        case LogType.Log:
                            m_infoCount++;
                            break;

                        case LogType.Warning:
                            m_warningCount++;
                            break;

                        case LogType.Error:
                            m_errorCount++;
                            break;

                        case LogType.Exception:
                            m_fatalCount++;
                            break;
                    }
                }
            }

            public void GetRecentLogs(List<LogNode> result)
            {
                if (result == null)
                {
                    DLogger.Error("result无效");
                    return;
                }
                result.Clear();

                foreach (var logNode in m_logNodeQueue)
                {
                    result.Add(logNode);
                }
            }

            public void GetRecentLogs(List<LogNode> results, int count)
            {
                if (results == null)
                {
                    DLogger.Error("result无效");
                    return;
                }

                if (count <= 0)
                {
                    DLogger.Error("数量无效");
                    return;
                }

                int position = m_logNodeQueue.Count - count;
                if (position < 0)
                {
                    position = 0;
                }

                int index = 0;
                results.Clear();
                foreach (LogNode logNode in m_logNodeQueue)
                {
                    if (index++ < position)
                    {
                        continue;
                    }

                    results.Add(logNode);
                }
            }

            private string GetLogString(LogNode logNode)
            {
                Color32 color = GetLogStringColor(logNode.LogType);
                return Utility.StringUtil.Format(Constant.CONSOLE_WINDOW_LOG_SINGLE_MESSAGE_STRING, color.r, color.g, color.b, color.a, logNode.LogTime.ToLocalTime(), logNode.LogFrameCount, logNode.LogMessage);
            }

            internal Color32 GetLogStringColor(LogType logType)
            {
                switch (logType)
                {
                    case LogType.Log:
                        return infoColor;

                    case LogType.Warning:
                        return warningColor;

                    case LogType.Error:
                        return errorColor;

                    case LogType.Exception:
                        return fatalColor;
                }

                return Color.white;
            }

            private void SaveLogsToDevice()
            {
                if (m_logNodes == null || m_logNodes.Count == 0)
                {
                    return;
                }
                string filePath = Application.persistentDataPath + "/game_logs.txt";
                List<string> fileContentsList = new List<string>();
                DLogger.Info("Saving logs to " + filePath);
                File.Delete(filePath);
                for (int i = 0; i < m_logNodes.Count; i++)
                {
                    fileContentsList.Add(m_logNodes[i].LogType + "\n" + m_logNodes[i].LogMessage + "\n" + m_logNodes[i].StackTrace);
                }
                File.WriteAllLines(filePath, fileContentsList.ToArray());
            }
        }
    }
}