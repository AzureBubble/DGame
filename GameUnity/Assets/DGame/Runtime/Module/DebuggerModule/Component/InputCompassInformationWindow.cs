using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputCompassInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Compass Information</b>");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启用电子罗盘传感器
                        if (GUILayout.Button("Enable", GUILayout.Height(30f)))
                        {
                            Input.compass.enabled = true;
                        }
                        // 禁用电子罗盘传感器
                        if (GUILayout.Button("Disable", GUILayout.Height(30f)))
                        {
                            Input.compass.enabled = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    DrawItem("Enabled", Input.compass.enabled.ToString(), "罗盘是否已启用");
                    if (Input.compass.enabled)
                    {
                        DrawItem("Heading Accuracy", Input.compass.headingAccuracy.ToString(), "朝向精度（度）");
                        DrawItem("Magnetic Heading", Input.compass.magneticHeading.ToString(), "磁北方向（度）");
                        DrawItem("True Heading", Input.compass.trueHeading.ToString(), "真北方向（度）");
                        DrawItem("Raw Vector", Input.compass.rawVector.ToString(), "原始磁场向量");
                        DrawItem("Timestamp", Input.compass.timestamp.ToString(), "时间戳");
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}