using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private abstract class ScrollableDebuggerWindowBase : IDebuggerWindow
        {
            private Vector2 m_scrollPosition = Vector2.zero;

            public virtual void Initialize(params object[] args)
            {
            }

            public virtual void OnEnter()
            {
            }

            public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public virtual void OnDraw()
            {
                m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);
                {
                    OnDrawScrollableWindow();
                }
                GUILayout.EndScrollView();
            }

            protected abstract void OnDrawScrollableWindow();

            protected static void DrawItem(string title, string content)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label(title, GUILayout.Width(Constant.TITLE_WIDTH));

                    if (GUILayout.Button(content, "label"))
                    {
                        CopyToClipboard(content);
                    }
                }
                GUILayout.EndHorizontal();
            }

            protected static void DrawItem(string title, string content, string tooltip)
            {
                GUILayout.BeginHorizontal();
                {
                    // 使用Box或Button来支持tooltip
                    GUIContent buttonContent = new GUIContent(title, tooltip);
                    GUILayout.Label(buttonContent, GUILayout.Width(Constant.TITLE_WIDTH));
                    if (GUILayout.Button(content, "label"))
                    {
                        CopyToClipboard(content);
                    }

                    if (!string.IsNullOrEmpty(GUI.tooltip))
                    {
                        GUI.Label(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - 20, 500, 20), GUI.tooltip);
                    }
                }
                GUILayout.EndHorizontal();
            }

            protected static string GetByteLengthString(long byteLength)
            {
                if (byteLength < 1024L) // 2 ^ 10
                {
                    return Utility.StringUtil.Format("{0} Bytes", byteLength);
                }

                if (byteLength < 1048576L) // 2 ^ 20
                {
                    return Utility.StringUtil.Format("{0:F2} KB", byteLength / 1024f);
                }

                if (byteLength < 1073741824L) // 2 ^ 30
                {
                    return Utility.StringUtil.Format("{0:F2} MB", byteLength / 1048576f);
                }

                if (byteLength < 1099511627776L) // 2 ^ 40
                {
                    return Utility.StringUtil.Format("{0:F2} GB", byteLength / 1073741824f);
                }

                if (byteLength < 1125899906842624L) // 2 ^ 50
                {
                    return Utility.StringUtil.Format("{0:F2} TB", byteLength / 1099511627776f);
                }

                if (byteLength < 1152921504606846976L) // 2 ^ 60
                {
                    return Utility.StringUtil.Format("{0:F2} PB", byteLength / 1125899906842624f);
                }

                return Utility.StringUtil.Format("{0:F2} EB", byteLength / 1152921504606846976f);
            }

            public virtual void OnExit()
            {
            }

            public virtual void OnDestroy()
            {
            }
        }
    }
}