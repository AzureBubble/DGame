using System.Globalization;
using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class TimeInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Time Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Time Scale",
                        Utility.StringUtil.Format("{0}x [{1}]", Time.timeScale, GetTimeScaleDescription(Time.timeScale)), "时间缩放因子");
                    DrawItem("Realtime Since Startup",
                        Time.realtimeSinceStartup.ToString(CultureInfo.InvariantCulture), "从应用启动开始的真实时间（秒）");
                    DrawItem("Time Since Level Load", Time.timeSinceLevelLoad.ToString(CultureInfo.InvariantCulture), "从当前场景加载开始的时间（秒）");
                    DrawItem("Time", Time.time.ToString(CultureInfo.InvariantCulture), "从游戏开始的总时间（秒）");
                    DrawItem("Fixed Time", Time.fixedTime.ToString(CultureInfo.InvariantCulture), "FixedUpdate使用的累计时间（秒）");
                    DrawItem("Unscaled Time", Time.unscaledTime.ToString(CultureInfo.InvariantCulture), "不受时间缩放影响的总时间（秒）");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Fixed Unscaled Time", Time.fixedUnscaledTime.ToString(CultureInfo.InvariantCulture), "不受时间缩放影响的FixedUpdate使用的累计时间（秒）");
#endif
                    DrawItem("Delta Time", Time.deltaTime.ToString(CultureInfo.InvariantCulture), "上一帧到当前帧的时间间隔（秒）");
                    DrawItem("Fixed Delta Time", Time.fixedDeltaTime.ToString(CultureInfo.InvariantCulture), "FixedUpdate的固定时间间隔（秒）");
                    DrawItem("Unscaled Delta Time", Time.unscaledDeltaTime.ToString(CultureInfo.InvariantCulture), "不受时间缩放影响的帧间隔（秒）");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Fixed Unscaled Delta Time",
                        Time.fixedUnscaledDeltaTime.ToString(CultureInfo.InvariantCulture), "不受时间缩放影响FixedUpdate的固定时间间隔（秒）");
#endif
                    DrawItem("Smooth Delta Time", Time.smoothDeltaTime.ToString(CultureInfo.InvariantCulture), "平滑处理的时间增量");
                    DrawItem("Maximum Delta Time", Time.maximumDeltaTime.ToString(CultureInfo.InvariantCulture), "最大允许的帧时间间隔（秒）");
#if UNITY_5_5_OR_NEWER
                    DrawItem("Maximum Particle Delta Time",
                        Time.maximumParticleDeltaTime.ToString(CultureInfo.InvariantCulture), "粒子系统的最大时间间隔（秒）");
#endif
                    DrawItem("Frame Count", Time.frameCount.ToString(), "游戏运行的总帧数");
                    DrawItem("Rendered Frame Count", Time.renderedFrameCount.ToString(), "实际渲染的帧数");
                    DrawItem("Capture Framerate", Time.captureFramerate.ToString(), "截图/录像的固定帧率");
#if UNITY_2019_2_OR_NEWER
                    DrawItem("Capture Delta Time", Time.captureDeltaTime.ToString(CultureInfo.InvariantCulture), "截图/录像的固定帧时间间隔");
#endif
#if UNITY_5_6_OR_NEWER
                    DrawItem("In Fixed Time Step", Time.inFixedTimeStep.ToString(), "当前是否在FixedUpdate中执行");
#endif
                }
                GUILayout.EndVertical();
            }

            private string GetTimeScaleDescription(float timeScale)
            {
                if (timeScale <= 0f)
                {
                    return "Pause";
                }

                if (timeScale < 1f)
                {
                    return "Slower";
                }

                if (timeScale > 1f)
                {
                    return "Faster";
                }

                return "Normal";
            }
        }
    }
}