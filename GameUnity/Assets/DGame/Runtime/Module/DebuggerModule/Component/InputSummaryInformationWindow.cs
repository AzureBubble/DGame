using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputSummaryInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Summary Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Back Button Leaves App", Input.backButtonLeavesApp.ToString(), "返回按钮是否退出应用");
                    DrawItem("Device Orientation", Input.deviceOrientation.ToString(), "设备物理方向");
                    DrawItem("Mouse Present", Input.mousePresent.ToString(), "是否存在鼠标设备");
                    DrawItem("Mouse Position", Input.mousePosition.ToString(), "当前鼠标位置（屏幕坐标）");
                    DrawItem("Mouse Scroll Delta", Input.mouseScrollDelta.ToString(), "鼠标滚轮滚动增量");
                    DrawItem("Any Key", Input.anyKey.ToString(), "当前是否有任何按键被按住");
                    DrawItem("Any Key Down", Input.anyKeyDown.ToString(), "当前帧是否有按键按下");
                    DrawItem("Input String", Input.inputString, "当前输入的字符序列");
                    DrawItem("IME Is Selected", Input.imeIsSelected.ToString(), "输入法是否被激活");
                    DrawItem("IME Composition Mode", Input.imeCompositionMode.ToString(), "输入法组合模式");
                    DrawItem("Compensate Sensors", Input.compensateSensors.ToString(), "是否补偿传感器数据");
                    DrawItem("Composition Cursor Position", Input.compositionCursorPos.ToString(), "输入法组合光标位置");
                    DrawItem("Composition String", Input.compositionString, "输入法组合字符串");
                }
                GUILayout.EndVertical();
            }
        }
    }
}