using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class SettingsWindow : ScrollableDebuggerWindowBase
        {
            private DebuggerDriver m_debuggerDriver;
            private float m_lastIconX;
            private float m_lastIconY;
            private float m_lastWindowX;
            private float m_lastWindowY;
            private float m_lastWindowWidth;
            private float m_lastWindowHeight;
            private float m_lastWindowScale;

            public override void Initialize(params object[] args)
            {
                m_debuggerDriver = DebuggerDriver.Instance;

                if (m_debuggerDriver == null)
                {
                    Debugger.Fatal("[DGame] DebuggerDriver is null");
                    return;
                }

                m_lastIconX = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, DefaultIconRect.x);
                m_lastIconY = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, DefaultIconRect.y);
                m_lastWindowX = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, DefaultWindowRect.x);
                m_lastWindowY = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, DefaultWindowRect.y);
                m_lastWindowWidth = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, DefaultWindowRect.width);
                m_lastWindowHeight = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, DefaultWindowRect.height);
                m_lastWindowScale = Utility.PlayerPrefsUtil.GetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, DefaultWindowScale);
                m_debuggerDriver.IconRect = new Rect(m_lastIconX, m_lastIconY, DefaultIconRect.width, DefaultIconRect.height);
                m_debuggerDriver.WindowRect = new Rect(m_lastWindowX, m_lastWindowY, m_lastWindowWidth, m_lastWindowHeight);
                m_debuggerDriver.WindowScale = m_lastWindowScale;
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
                if (Mathf.Abs(m_lastIconX - m_debuggerDriver.IconRect.x) > 0.01f)
                {
                    m_lastIconX = m_debuggerDriver.IconRect.x;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_X, m_lastIconX);
                }

                if (Mathf.Abs(m_lastIconY - m_debuggerDriver.IconRect.y) > 0.01f)
                {
                    m_lastIconY = m_debuggerDriver.IconRect.y;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_ICON_Y, m_lastIconY);
                }

                if (Mathf.Abs(m_lastWindowX - m_debuggerDriver.WindowRect.x) > 0.01f)
                {
                    m_lastWindowX = m_debuggerDriver.WindowRect.x;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_X, m_lastWindowX);
                }

                if (Mathf.Abs(m_lastWindowY - m_debuggerDriver.WindowRect.y) > 0.01f)
                {
                    m_lastWindowY = m_debuggerDriver.WindowRect.y;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_Y, m_lastWindowY);
                }

                if (Mathf.Abs(m_lastWindowWidth - m_debuggerDriver.WindowRect.width) > 0.01f)
                {
                    m_lastWindowWidth = m_debuggerDriver.WindowRect.width;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_WIDTH, m_lastWindowWidth);
                }

                if (Mathf.Abs(m_lastWindowHeight - m_debuggerDriver.WindowRect.height) > 0.01f)
                {
                    m_lastWindowHeight = m_debuggerDriver.WindowRect.height;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_HEIGHT, m_lastWindowHeight);
                }

                if (Mathf.Abs(m_lastWindowScale - m_debuggerDriver.WindowScale) > 0.01f)
                {
                    m_lastWindowScale = m_debuggerDriver.WindowScale;
                    Utility.PlayerPrefsUtil.SetFloat(Constant.DEFAULT_DEBUGGER_WINDOW_WINDOW_SCALE, m_lastWindowScale);
                }
            }

            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Window Settings</b>");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Position:", GUILayout.Width(60f));
                        GUILayout.Label("Drag window caption to move position.");
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        float width = m_debuggerDriver.WindowRect.width;
                        GUILayout.Label("Width:", GUILayout.Width(60f));
                        if (GUILayout.RepeatButton("-", GUILayout.Width(30f)))
                        {
                            width--;
                        }

                        width = GUILayout.HorizontalSlider(width, 100f, Screen.width - 20f);
                        if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                        {
                            width++;
                        }

                        width = Mathf.Clamp(width, 100f, Screen.width - 20f);
                        if (Mathf.Abs(width - m_debuggerDriver.WindowRect.width) > 0.01f)
                        {
                            m_debuggerDriver.WindowRect = new Rect(m_debuggerDriver.WindowRect.x, m_debuggerDriver.WindowRect.y, width,
                                m_debuggerDriver.WindowRect.height);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        float height = m_debuggerDriver.WindowRect.height;
                        GUILayout.Label("Height:", GUILayout.Width(60f));
                        if (GUILayout.RepeatButton("-", GUILayout.Width(30f)))
                        {
                            height--;
                        }

                        height = GUILayout.HorizontalSlider(height, 100f, Screen.height - 20f);
                        if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                        {
                            height++;
                        }

                        height = Mathf.Clamp(height, 100f, Screen.height - 20f);
                        if (Mathf.Abs(height - m_debuggerDriver.WindowRect.height) > 0.01f)
                        {
                            m_debuggerDriver.WindowRect = new Rect(m_debuggerDriver.WindowRect.x, m_debuggerDriver.WindowRect.y,
                                m_debuggerDriver.WindowRect.width, height);
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        float scale = m_debuggerDriver.WindowScale;
                        GUILayout.Label("Scale:", GUILayout.Width(60f));
                        if (GUILayout.RepeatButton("-", GUILayout.Width(30f)))
                        {
                            scale -= 0.01f;
                        }

                        scale = GUILayout.HorizontalSlider(scale, 0.5f, 4f);
                        if (GUILayout.RepeatButton("+", GUILayout.Width(30f)))
                        {
                            scale += 0.01f;
                        }

                        scale = Mathf.Clamp(scale, 0.5f, 4f);
                        if (Mathf.Abs(scale - m_debuggerDriver.WindowScale) > 0.01f)
                        {
                            m_debuggerDriver.WindowScale = scale;
                        }
                    }
                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button("0.5x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 0.5f;
                        }

                        if (GUILayout.Button("1.0x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 1f;
                        }

                        if (GUILayout.Button("1.5x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 1.5f;
                        }

                        if (GUILayout.Button("2.0x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 2f;
                        }

                        if (GUILayout.Button("2.5x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 2.5f;
                        }

                        if (GUILayout.Button("3.0x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 3f;
                        }

                        if (GUILayout.Button("3.5x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 3.5f;
                        }

                        if (GUILayout.Button("4.0x", GUILayout.Height(60f)))
                        {
                            m_debuggerDriver.WindowScale = 4f;
                        }
                    }
                    GUILayout.EndHorizontal();

                    if (GUILayout.Button("Reset Layout", GUILayout.Height(30f)))
                    {
                        m_debuggerDriver.ResetWindowLayout();
                    }
                }
                GUILayout.EndVertical();
            }

            public override void OnExit()
            {
                Utility.PlayerPrefsUtil.Save();
                base.OnExit();
            }
        }
    }
}