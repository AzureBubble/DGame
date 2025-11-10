using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal partial class InputModule
    {
        private class HotKeyCommand : ICommand
        {
            protected override IInputModule InputModule { get; }

            private HotKeyNameType HotKeyName { get; }
            private KeyPressType KeyPressType { get; }
            private UnityAction<float> OnClickHotKey { get; set; }

            public HotKeyCommand(IInputModule inputModule, HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action)
            {
                InputModule = inputModule;
                HotKeyName = hotKeyType;
                KeyPressType = type;
                OnClickHotKey = action;
            }

            public override void Execute()
            {
                switch (KeyPressType)
                {
                    case KeyPressType.Axis:
                        OnClickHotKey?.Invoke(Input.GetAxis(HotKeyName.ToString()));
                        break;

                    case KeyPressType.AxisRaw:
                        OnClickHotKey?.Invoke(Input.GetAxisRaw(HotKeyName.ToString()));
                        break;
                }
            }

            public override bool AddListener(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action)
            {
                if (HotKeyName == hotKeyType && KeyPressType == type)
                {
                    OnClickHotKey += action;
                    return true;
                }

                return false;
            }

            public override ICommand RemoveListener(HotKeyNameType hotKeyType, KeyPressType type,
                UnityAction<float> action)
            {
                if (HotKeyName == hotKeyType && KeyPressType == type)
                {
                    if (OnClickHotKey != null)
                    {
                        OnClickHotKey -= action;
                    }
                }

                if (OnClickHotKey == null || OnClickHotKey.GetInvocationList().Length == 0)
                {
                    return this;
                }

                return null;
            }
        }
    }
}