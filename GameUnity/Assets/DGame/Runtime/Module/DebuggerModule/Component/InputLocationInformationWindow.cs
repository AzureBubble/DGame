using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputLocationInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Location Information</b>");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启动定位服务
                        if (GUILayout.Button("Enable", GUILayout.Height(30f)))
                        {
                            Input.location.Start();
                        }

                        // 停止定位服务
                        if (GUILayout.Button("Disable", GUILayout.Height(30f)))
                        {
                            Input.location.Stop();
                        }
                    }
                    GUILayout.EndHorizontal();

                    DrawItem("Is Enabled By User", Input.location.isEnabledByUser.ToString(), "用户是否在系统设置中启用了定位服务");
                    DrawItem("Status", Input.location.status.ToString(), "定位服务当前状态");

                    if (Input.location.status == LocationServiceStatus.Running)
                    {
                        DrawItem("Horizontal Accuracy", Input.location.lastData.horizontalAccuracy.ToString(), "水平精度（米）");
                        DrawItem("Vertical Accuracy", Input.location.lastData.verticalAccuracy.ToString(), "垂直精度（米）");
                        DrawItem("Longitude", Input.location.lastData.longitude.ToString(), "经度");
                        DrawItem("Latitude", Input.location.lastData.latitude.ToString(), "纬度");
                        DrawItem("Altitude", Input.location.lastData.altitude.ToString(), "海拔高度");
                        DrawItem("Timestamp", Input.location.lastData.timestamp.ToString(), "时间戳");
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}