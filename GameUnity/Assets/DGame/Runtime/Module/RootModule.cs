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

        private void Awake()
        {
            m_instance = this;
            InitStringUtilHelper();
            GameTime.StartFrame();
        }

        private void InitStringUtilHelper()
        {
            if (string.IsNullOrEmpty(stringUtilHelperTypeName))
            {
                return;
            }
            Type type = Utility.AssemblyUtil.GetType(stringUtilHelperTypeName);

            if (type.IsNullableType())
            {
                return;
            }

            if (type == null)
            {
                Debugger.Error("查找不到默认的StringUtilHelper类型：'{0}'", stringUtilHelperTypeName);
                return;
            }

            Utility.StringUtil.IStringUtilHelper stringUtilHelper = Activator.CreateInstance(type) as Utility.StringUtil.IStringUtilHelper;

            if (stringUtilHelper == null)
            {
                Debugger.Error("无法创建StringUtilHelper类型实例：'{0}'", stringUtilHelperTypeName);
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
#if !UNITY_EDITOR
            ModuleSystem.OnDestroy();
#endif
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

#endif

        #endregion
    }
}