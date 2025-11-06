using UnityEngine;
#if UNITY_EDITOR && ENABLE_ODIN_INSPECTOR && ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace DGame
{
    public enum DebuggerActiveWindowType : byte
    {
        /// <summary>
        /// 总是打开
        /// </summary>
#if UNITY_EDITOR && ENABLE_ODIN_INSPECTOR && ODIN_INSPECTOR
        [LabelText("总是打开")]
#endif
        [InspectorName("总是打开")]
        AlwaysOpen = 0,

        /// <summary>
        /// 仅在开发模式时打开
        /// </summary>
#if UNITY_EDITOR && ENABLE_ODIN_INSPECTOR && ODIN_INSPECTOR
        [LabelText("仅在开发模式时打开")]
#endif
        [InspectorName("仅在开发模式时打开")]
        OnlyOpenWhenDevelopment = 1,

        /// <summary>
        /// 仅在编辑器中打开
        /// </summary>
#if UNITY_EDITOR && ENABLE_ODIN_INSPECTOR && ODIN_INSPECTOR
        [LabelText("仅在编辑器中打开")]
#endif
        [InspectorName("仅在编辑器中打开")]
        OnlyOpenInEditor = 2,

        /// <summary>
        /// 总是关闭
        /// </summary>
#if UNITY_EDITOR && ENABLE_ODIN_INSPECTOR && ODIN_INSPECTOR
        [LabelText("总是关闭")]
#endif
        [InspectorName("总是关闭")]
        AlwaysClose = 3,
    }
}