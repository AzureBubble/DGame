using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 本地化语言
    /// </summary>
    public enum Language : byte
    {
        /// <summary>
        /// 中国
        /// </summary>
        CN = 0,

        /// <summary>
        /// 欧美
        /// </summary>
        EN = 1,

        /// <summary>
        /// 港澳台
        /// </summary>
        GAT = 2,

        /// <summary>
        /// 韩国
        /// </summary>
        KR = 3,

        /// <summary>
        /// 日本
        /// </summary>
        JP = 4,

        /// <summary>
        /// 越南
        /// </summary>
        VN = 5,

        /// <summary>
        /// 印尼
        /// </summary>
        INDO = 6,

        /// <summary>
        /// 占位符（新增+1）
        /// </summary>
        MAX = 7,
    }
}