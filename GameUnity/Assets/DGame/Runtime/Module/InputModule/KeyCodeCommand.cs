using System;
using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal partial class InputModule
    {
        private class KeyCodeCommand : ICommand
        {
            protected override IInputModule InputModule { get; }
            private KeyCode KeyCode { get; }
            private KeyPressType KeyPressType { get; }
            private UnityAction<KeyCode> OnClickAction { get; set; }

            public KeyCodeCommand(IInputModule inputModule, KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action)
            {
                InputModule = inputModule;
                KeyCode = keyCode;
                KeyPressType = type;
                OnClickAction = action;
            }

            public override void Execute()
            {
                InputModule.TryGetRebindKeyCode(this.KeyCode, out var rebindKeyCode);

                switch (KeyPressType)
                {
                    case KeyPressType.Down:
                        if (Input.GetKeyDown(rebindKeyCode))
                        {
                            OnClickAction?.Invoke(rebindKeyCode);
                        }

                        break;

                    case KeyPressType.Stay:
                        if (Input.GetKey(rebindKeyCode))
                        {
                            OnClickAction?.Invoke(rebindKeyCode);
                        }

                        break;

                    case KeyPressType.Up:
                        if (Input.GetKeyUp(rebindKeyCode))
                        {
                            OnClickAction?.Invoke(rebindKeyCode);
                        }

                        break;
                }
            }

            public override bool AddListener(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action)
            {
                if (keyCode == KeyCode && KeyPressType == type)
                {
                    OnClickAction += action;
                    return true;
                }

                return false;
            }

            public override ICommand RemoveListener(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action)
            {
                if (keyCode == KeyCode && KeyPressType == type)
                {
                    if (OnClickAction != null)
                    {
                        OnClickAction -= action;
                    }
                }

                if (OnClickAction == null || OnClickAction.GetInvocationList().Length == 0)
                {
                    return this;
                }

                return null;
            }
        }
    }
}