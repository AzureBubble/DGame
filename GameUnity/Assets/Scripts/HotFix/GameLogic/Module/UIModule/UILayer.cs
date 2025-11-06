using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// UI的层级枚举
    /// </summary>
    public enum UILayer : byte
    {
        Bottom = 0,
        UI = 1,
        Top = 2,
        Tips = 3,

        /// <summary>
        /// 系统级
        /// </summary>
        System = 4,
    }
}