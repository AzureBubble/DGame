using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputTouchInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Touch Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Touch Supported", Input.touchSupported.ToString(), "设备是否支持触摸输入");
                    DrawItem("Touch Pressure Supported", Input.touchPressureSupported.ToString(), "是否支持触摸压力感应");
                    DrawItem("Stylus Touch Supported", Input.stylusTouchSupported.ToString(), "是否支持触控笔输入");
                    DrawItem("Simulate Mouse With Touches", Input.simulateMouseWithTouches.ToString(), "是否用触摸模拟鼠标输入");
                    DrawItem("Multi Touch Enabled", Input.multiTouchEnabled.ToString(), "是否启用多点触控");
                    DrawItem("Touch Count", Input.touchCount.ToString(), "当前屏幕上的触摸点数量");
                    DrawItem("Touches", GetTouchesString(Input.touches), "所有触摸点的详细信息数组");
                }
                GUILayout.EndVertical();
            }

            private string GetTouchString(Touch touch)
            {
                return Utility.StringUtil.Format("{0}, {1}, {2}, {3}, {4}", touch.position, touch.deltaPosition,
                    touch.rawPosition,
                    touch.pressure, touch.phase);
            }

            private string GetTouchesString(Touch[] touches)
            {
                string[] touchStrings = new string[touches.Length];

                for (int i = 0; i < touches.Length; i++)
                {
                    touchStrings[i] = GetTouchString(touches[i]);
                }

                return string.Join("\n", touchStrings);
            }
        }
    }
}