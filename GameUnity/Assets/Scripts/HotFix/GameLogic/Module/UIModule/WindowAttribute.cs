using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    public class WindowAttribute : Attribute
    {
        /// <summary>
        /// UI层级
        /// </summary>
        public readonly byte UILayer;

        /// <summary>
        /// 资源定位地址
        /// </summary>
        public readonly string Location;

        /// <summary>
        /// 全屏窗口标记
        /// </summary>
        public readonly bool FullScreen;

        /// <summary>
        /// 内部资源 无需AB包加载
        /// </summary>
        public readonly bool IsResources;

        /// <summary>
        /// 自动关闭时间
        /// </summary>
        public readonly int HideTimeToClose;

        /// <summary>
        /// 是否动画弹出
        /// </summary>
        public readonly bool NeedTweenPop;



        public WindowAttribute(byte uiLayer, string location = "", bool fullScreen = false, bool needTweenPop = true, int hideTimeToClose = 10)
        {
            UILayer = uiLayer;
            Location = location;
            FullScreen = fullScreen;
            HideTimeToClose = hideTimeToClose;
            NeedTweenPop = needTweenPop && !fullScreen;
        }

        public WindowAttribute(UILayer uiLayer, string location = "", bool fullScreen = false, bool needTweenPop = true, int hideTimeToClose = 10)
        {
            UILayer = (byte)uiLayer;
            Location = location;
            FullScreen = fullScreen;
            HideTimeToClose = hideTimeToClose;
            NeedTweenPop = needTweenPop && !fullScreen;
        }

        public WindowAttribute(UILayer uiLayer, string location, bool isResources, bool fullScreen = false, bool needTweenPop = true, int hideTimeToClose = 10)
        {
            UILayer = (byte)uiLayer;
            Location = location;
            FullScreen = fullScreen;
            HideTimeToClose = hideTimeToClose;
            IsResources = isResources;
            NeedTweenPop = needTweenPop && !fullScreen;
        }

        public WindowAttribute(UILayer uiLayer, bool isResources, bool fullScreen = false, bool needTweenPop = true, int hideTimeToClose = 10)
        {
            UILayer = (byte)uiLayer;
            FullScreen = fullScreen;
            HideTimeToClose = hideTimeToClose;
            IsResources = isResources;
            NeedTweenPop = needTweenPop && !fullScreen;
        }
    }
}