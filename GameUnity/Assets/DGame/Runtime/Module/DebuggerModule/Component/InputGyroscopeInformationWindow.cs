using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputGyroscopeInformationWindow: ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Gyroscope Information</b>");
                GUILayout.BeginVertical("box");
                {
                    GUILayout.BeginHorizontal();
                    {
                        // 启用陀螺仪传感器
                        if (GUILayout.Button("Enable", GUILayout.Height(30f)))
                        {
                            Input.gyro.enabled = true;
                        }
                        // 禁用陀螺仪传感器
                        if (GUILayout.Button("Disable", GUILayout.Height(30f)))
                        {
                            Input.gyro.enabled = false;
                        }
                    }
                    GUILayout.EndHorizontal();

                    DrawItem("Enabled", Input.gyro.enabled.ToString(), "陀螺仪是否已启用");
                    if (Input.gyro.enabled)
                    {
                        DrawItem("Update Interval", Input.gyro.updateInterval.ToString(), "更新间隔（秒）");
                        DrawItem("Attitude", Input.gyro.attitude.eulerAngles.ToString(), "设备姿态（欧拉角）");
                        DrawItem("Gravity", Input.gyro.gravity.ToString(), "重力加速度向量");
                        DrawItem("Rotation Rate", Input.gyro.rotationRate.ToString(), "旋转速率");
                        DrawItem("Rotation Rate Unbiased", Input.gyro.rotationRateUnbiased.ToString(), "无偏旋转速率");
                        DrawItem("User Acceleration", Input.gyro.userAcceleration.ToString(), "用户加速度");
                    }
                }
                GUILayout.EndVertical();
            }
        }
    }
}