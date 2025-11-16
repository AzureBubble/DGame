#if ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
    /// <summary>
    /// 资源模块的加密类型
    /// </summary>
    public enum EncryptionType : byte
    {
        /// <summary>
        /// 无加密
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("无加密")]
#endif
        None = 0,

        /// <summary>
        /// 文件偏移加密
        /// 通过在文件开头添加偏移量来隐藏真实文件内容的加密方式
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("文件偏移加密")]
#endif
        FileOffset = 1,

        /// <summary>
        /// 文件流加密
        /// 使用加密流对文件内容进行加密处理的加密方式
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("文件流加密")]
#endif
        FileStream = 2,
    }
}