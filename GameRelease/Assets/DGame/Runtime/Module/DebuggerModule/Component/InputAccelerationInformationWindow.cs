using UnityEngine;

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class InputAccelerationInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Input Acceleration Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Acceleration", Input.acceleration.ToString(), "当前设备的瞬时加速度向量");
                    DrawItem("Acceleration Event Count", Input.accelerationEventCount.ToString(), "当前帧中累积的加速度事件数量");
                    DrawItem("Acceleration Events", GetAccelerationEventsString(Input.accelerationEvents),
                        "当前帧内所有加速度事件的详细数组");
                }
                GUILayout.EndVertical();
            }

            private string GetAccelerationEventString(AccelerationEvent accelerationEvent)
            {
                return Utility.StringUtil.Format("{0}, {1}", accelerationEvent.acceleration,
                    accelerationEvent.deltaTime);
            }

            private string GetAccelerationEventsString(AccelerationEvent[] accelerationEvents)
            {
                string[] accelerationEventStrings = new string[accelerationEvents.Length];

                for (int i = 0; i < accelerationEvents.Length; i++)
                {
                    accelerationEventStrings[i] = GetAccelerationEventString(accelerationEvents[i]);
                }

                return string.Join("\n", accelerationEventStrings);
            }
        }
    }
}