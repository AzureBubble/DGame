using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if ODIN_INSPECTOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
    public enum MemoryStrictCheckType : byte
    {
        /// <summary>
        /// 总是启用
        /// </summary>
        [Tooltip("总是启用")]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [LabelText("总是启用")]
#endif
        AlwaysEnable = 0,

        /// <summary>
        /// 仅在开发模式启用
        /// </summary>
        [Tooltip("仅在开发模式启用")]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [LabelText("仅在开发模式启用")]
#endif
        OnlyEnableWhenDevelopment,

        /// <summary>
        /// 仅在编辑器中启用
        /// </summary>
        [Tooltip("仅在编辑器中启用")]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [LabelText("仅在编辑器中启用")]
#endif
        OnlyEnableInEditor,

        /// <summary>
        /// 总是禁用
        /// </summary>
        [Tooltip("总是禁用")]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [LabelText("总是禁用")]
#endif
        AlwaysDisable,
    }

    /// <summary>
    /// 内存池设置相关
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MemoryPoolSetting : MonoBehaviour
    {
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [LabelText("内存池强制检查开启模式")]
#endif
        [SerializeField]
        private MemoryStrictCheckType m_memoryStrictCheckType = MemoryStrictCheckType.OnlyEnableInEditor;

        public bool EnableStrictCheck
        {
            get => MemoryPool.EnableStrictCheck;
            set
            {
                MemoryPool.EnableStrictCheck = value;
                if (value)
                {
                    Debugger.Info("内存池已启用严格检查，这将极大程序影响性能。");
                }
            }
        }

        private void Awake()
        {
            switch (m_memoryStrictCheckType)
            {
                case MemoryStrictCheckType.AlwaysEnable:
                    EnableStrictCheck = true;
                    break;

                case MemoryStrictCheckType.OnlyEnableInEditor:
                    EnableStrictCheck = Application.isEditor;
                    break;

                case MemoryStrictCheckType.AlwaysDisable:
                    EnableStrictCheck = false;
                    break;

                case MemoryStrictCheckType.OnlyEnableWhenDevelopment:
                    EnableStrictCheck = Debug.isDebugBuild;
                    break;
            }
            Destroy(gameObject);
        }
    }
}