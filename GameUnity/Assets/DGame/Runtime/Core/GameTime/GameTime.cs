using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public static class GameTime
    {
        /// <summary>
        /// 从游戏开始到现在经过的时间（秒）
        /// 受到 Time.timeScale 影响
        /// </summary>
        public static float Time { get; private set; }

        /// <summary>
        /// 从游戏开始到现在经过的时间（秒）
        /// 不受 Time.timeScale 影响
        /// </summary>
        public static float UnscaledTime { get; private set; }

        /// <summary>
        /// 从上一帧到当前帧的间隔时间（秒）
        /// 受 Time.timeScale 影响 适用于平滑的帧率无关运动
        /// </summary>
        public static float DeltaTime { get; private set; }

        /// <summary>
        /// timeScale 从上一帧到当前帧的独立时间间隔（秒）
        /// 不受 Time.timeScale 影响
        /// </summary>
        public static float UnscaledDeltaTime { get; private set; }

        /// <summary>
        /// 执行物理和其他固定帧速率更新的时间间隔（秒）（默认 0.02s）
        /// </summary>
        public static float FixedDeltaTime { get; private set; }

        /// <summary>
        /// 自游戏启动以来渲染的总帧数
        /// </summary>
        public static float FrameCount { get; private set; }

        /// <summary>
        /// 程序启动以来的真实时间
        /// </summary>
        public static float RealtimeSinceStartup => UnityEngine.Time.realtimeSinceStartup;

        /// <summary>
        /// 采样一帧的时间
        /// </summary>
        public static void StartFrame()
        {
            Time = UnityEngine.Time.time;
            DeltaTime = UnityEngine.Time.deltaTime;
            UnscaledDeltaTime = UnityEngine.Time.unscaledDeltaTime;
            FixedDeltaTime = UnityEngine.Time.fixedDeltaTime;
            FrameCount = UnityEngine.Time.frameCount;
            UnscaledTime = UnityEngine.Time.unscaledTime;
        }
    }
}