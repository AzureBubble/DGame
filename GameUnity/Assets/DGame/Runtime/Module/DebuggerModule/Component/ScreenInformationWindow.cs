using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class ScreenInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Screen Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Current Resolution", GetResolutionString(Screen.currentResolution), "当前屏幕分辨率");
                    DrawItem("Screen Width",
                        Utility.StringUtil.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.width,
                            Utility.Converter.GetInchesFromPixels(Screen.width),
                            Utility.Converter.GetCentimetersFromPixels(Screen.width)), "屏幕宽度（像素 + 英寸 + 厘米转换）");
                    DrawItem("Screen Height",
                        Utility.StringUtil.Format("{0} px / {1:F2} in / {2:F2} cm", Screen.height,
                            Utility.Converter.GetInchesFromPixels(Screen.height),
                            Utility.Converter.GetCentimetersFromPixels(Screen.height)), "屏幕高度（像素 + 英寸 + 厘米转换）");
                    DrawItem("Screen DPI", Screen.dpi.ToString("F2"), "屏幕每英寸像素密度");
                    DrawItem("Screen Orientation", Screen.orientation.ToString(), "屏幕方向（横屏、竖屏等）");
                    DrawItem("Is Full Screen", Screen.fullScreen.ToString(), "是否全屏模式");
#if UNITY_2018_1_OR_NEWER
                    DrawItem("Full Screen Mode", Screen.fullScreenMode.ToString(), "全屏模式类型");
#endif
                    DrawItem("Sleep Timeout", GetSleepTimeoutDescription(Screen.sleepTimeout), "屏幕休眠超时设置");
#if UNITY_2019_2_OR_NEWER
                    DrawItem("Brightness", Screen.brightness.ToString("F2"), "屏幕亮度（0.0-1.0）");
#endif
                    DrawItem("Cursor Visible", Cursor.visible.ToString(), "光标是否可见");
                    DrawItem("Cursor Lock State", Cursor.lockState.ToString(), "光标锁定状态");
                    DrawItem("Auto Landscape Left", Screen.autorotateToLandscapeLeft.ToString(), "是否自动旋转到左横屏");
                    DrawItem("Auto Landscape Right", Screen.autorotateToLandscapeRight.ToString(), "是否自动旋转到右横屏");
                    DrawItem("Auto Portrait", Screen.autorotateToPortrait.ToString(), "是否自动旋转到竖屏");
                    DrawItem("Auto Portrait Upside Down", Screen.autorotateToPortraitUpsideDown.ToString(), "是否自动旋转到倒竖屏");
#if UNITY_2017_2_OR_NEWER && !UNITY_2017_2_0
                    DrawItem("Safe Area", Screen.safeArea.ToString(), "安全显示区域（避开刘海、圆角）");
#endif
#if UNITY_2019_2_OR_NEWER
                    DrawItem("Cutouts", GetCutoutsString(Screen.cutouts), "屏幕缺口信息（刘海屏、挖孔屏）");
#endif
                    DrawItem("Support Resolutions", GetResolutionsString(Screen.resolutions), "设备支持的所有分辨率列表");
                }
                GUILayout.EndVertical();
            }

            private string GetSleepTimeoutDescription(int sleepTimeout)
            {
                if (sleepTimeout == SleepTimeout.NeverSleep)
                {
                    return "Never Sleep";
                }

                if (sleepTimeout == SleepTimeout.SystemSetting)
                {
                    return "System Setting";
                }

                return sleepTimeout.ToString();
            }

            private string GetResolutionString(Resolution resolution)
            {
#if UNITY_6000_0_OR_NEWER
                return Utility.StringUtil.Format("{0} x {1} @ {2}Hz", resolution.width, resolution.height, resolution.refreshRateRatio);
#else
                return Utility.StringUtil.Format("{0} x {1} @ {2}Hz", resolution.width, resolution.height, resolution.refreshRate);
#endif
            }

            private string GetCutoutsString(Rect[] cutouts)
            {
                string[] cutoutStrings = new string[cutouts.Length];
                for (int i = 0; i < cutouts.Length; i++)
                {
                    cutoutStrings[i] = cutouts[i].ToString();
                }
                return string.Join("\n", cutoutStrings);
            }

            private string GetResolutionsString(Resolution[] resolutions)
            {
                string[] resolutionStrings = new string[resolutions.Length];
                for (int i = 0; i < resolutions.Length; i++)
                {
                    resolutionStrings[i] = GetResolutionString(resolutions[i]);
                }

                return string.Join("\n", resolutionStrings);
            }
        }
    }
}