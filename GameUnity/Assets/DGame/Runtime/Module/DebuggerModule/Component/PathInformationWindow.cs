using System;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class PathInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Path Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Current Directory", Utility.PathUtil.GetRegularPath(Environment.CurrentDirectory), "当前工作目录");
                    DrawItem("Data Path", Utility.PathUtil.GetRegularPath(Application.dataPath), "数据资源路径");
                    DrawItem("Persistent Data Path", Utility.PathUtil.GetRegularPath(Application.persistentDataPath), "持久化数据路径");
                    DrawItem("Streaming Assets Path", Utility.PathUtil.GetRegularPath(Application.streamingAssetsPath), "流式资源路径");
                    DrawItem("Temporary Cache Path", Utility.PathUtil.GetRegularPath(Application.temporaryCachePath), "临时缓存路径");
#if UNITY_2018_3_OR_NEWER
                    DrawItem("Console Log Path", Utility.PathUtil.GetRegularPath(Application.consoleLogPath), "控制台日志文件路径");
#endif
                }
                GUILayout.EndVertical();
            }
        }
    }
}