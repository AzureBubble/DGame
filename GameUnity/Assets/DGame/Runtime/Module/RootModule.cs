using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities;
using UnityEngine;

#if ODIN_INSPECTOR && UNITY_EDITOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
    [DisallowMultipleComponent]
    public sealed class RootModule : MonoBehaviour
    {
        // [SerializeField] private bool m_isShowGlobalHelperSetting = false;

        private static RootModule m_instance = null;

        public static RootModule Instance => m_instance == null ? UnityEngine.Object.FindObjectOfType<RootModule>() : m_instance;

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ValueDropdown("GetIStringUtilHelperImplementations"), LabelText("字符串辅助器"), FoldoutGroup("全局辅助器设置", true), DisableInPlayMode]
#endif
        private string stringUtilHelperTypeName = "DGame.DGameStringUtilHelper";

        [SerializeField]
#if ODIN_INSPECTOR && ENABLE_ODIN_INSPECTOR
        [ValueDropdown("GetILogHelperImplementations"), LabelText("日志系统辅助器"), FoldoutGroup("全局辅助器设置", true), DisableInPlayMode]
#endif
        private string logHelperTypeName = "DGame.DGameLogHelper";

        private void Awake()
        {
            m_instance = this;
            InitStringUtilHelper();
            InitLogHelper();
            GameTime.StartFrame();
        }

        private void InitLogHelper()
        {
            if (string.IsNullOrEmpty(logHelperTypeName))
            {
                return;
            }

            Type logHelperType = Utility.AssemblyUtil.GetType(logHelperTypeName);

            if (logHelperType == typeof(Nullable))
            {
                return;
            }

            if (logHelperType == null)
            {
                Debugger.Error("查找不到默认的ILogHelper类型：'{0}'", logHelperTypeName);
                return;
            }

            DGameLog.ILogHelper logHelper = Activator.CreateInstance(logHelperType) as DGameLog.ILogHelper;

            if (logHelper == null)
            {
                Debugger.Error("无法创建ILogHelper类型实例：'{0}'", stringUtilHelperTypeName);
                return;
            }

            DGameLog.SetLogHelper(logHelper);
        }

        private void InitStringUtilHelper()
        {
            if (string.IsNullOrEmpty(stringUtilHelperTypeName))
            {
                return;
            }
            Type type = Utility.AssemblyUtil.GetType(stringUtilHelperTypeName);

            if (type == typeof(Nullable))
            {
                return;
            }

            if (type == null)
            {
                Debugger.Error("查找不到默认的IStringUtilHelper类型：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.IStringUtilHelper stringUtilHelper = Activator.CreateInstance(type) as Utility.StringUtil.IStringUtilHelper;

            if (stringUtilHelper == null)
            {
                Debugger.Error("无法创建IStringUtilHelper类型实例：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.SetStringHelper(stringUtilHelper);
        }

        private void Update()
        {
            GameTime.StartFrame();
            ModuleSystem.Update(GameTime.DeltaTime, GameTime.UnscaledDeltaTime);
        }

        private void FixedUpdate()
        {
            GameTime.StartFrame();
        }

        private void LateUpdate()
        {
            GameTime.StartFrame();
        }

        private void OnDestroy()
        {
// #if !UNITY_EDITOR
            ModuleSystem.OnDestroy();
// #endif
        }

        internal void Destroy()
        {
            Destroy(gameObject);
        }

        #region Odin 相关处理

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR

        private IEnumerable<ValueDropdownItem> GetIStringUtilHelperImplementations()
        {
            var types = Utility.AssemblyUtil.GetTypes(typeof(Utility.StringUtil.IStringUtilHelper));

            for (int i = 0; i < types.Count + 1; i++)
            {
                if (i == 0)
                {
                    var type = typeof(Nullable);
                    yield return new ValueDropdownItem(
                        text: "<None>",
                        value: type.AssemblyQualifiedName
                    );
                }
                else
                {
                    var type = types[i - 1];
                    yield return new ValueDropdownItem(
                        text: type.FullName,
                        value: type.AssemblyQualifiedName
                    );
                }
            }
        }

        private IEnumerable<ValueDropdownItem> GetILogHelperImplementations()
        {
            var types = Utility.AssemblyUtil.GetTypes(typeof(DGameLog.ILogHelper));

            for (int i = 0; i < types.Count + 1; i++)
            {
                if (i == 0)
                {
                    var type = typeof(Nullable);
                    yield return new ValueDropdownItem(
                        text: "<None>",
                        value: type.AssemblyQualifiedName
                    );
                }
                else
                {
                    var type = types[i - 1];
                    yield return new ValueDropdownItem(
                        text: type.FullName,
                        value: type.AssemblyQualifiedName
                    );
                }
            }
        }

#endif

        #endregion
    }
}