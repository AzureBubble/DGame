using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal partial class InputModule
    {
        private class MouseButtonCommand : ICommand
        {
            protected override IInputModule InputModule { get; }

            private int MouseBtn { get; }

            private KeyPressType KeyPressType { get; }

            private UnityAction<int> OnClickMouseBtn { get; set; }

            public MouseButtonCommand(IInputModule inputModule, int mouseButton, KeyPressType type, UnityAction<int> action)
            {
                MouseBtn = mouseButton;
                KeyPressType = type;
                OnClickMouseBtn = action;
                InputModule = inputModule;
            }

            public override void Execute()
            {
                switch (KeyPressType)
                {
                    case KeyPressType.Down:
                        if (Input.GetMouseButtonDown(MouseBtn))
                        {
                            OnClickMouseBtn?.Invoke(MouseBtn);
                        }

                        break;

                    case KeyPressType.Stay:
                        if (Input.GetMouseButton(MouseBtn))
                        {
                            OnClickMouseBtn?.Invoke(MouseBtn);
                        }

                        break;

                    case KeyPressType.Up:
                        if (Input.GetMouseButtonUp(MouseBtn))
                        {
                            OnClickMouseBtn?.Invoke(MouseBtn);
                        }

                        break;
                }
            }

            public override bool AddListener(int mouseBtn, KeyPressType type, UnityAction<int> action)
            {
                if (MouseBtn == mouseBtn && KeyPressType == type)
                {
                    OnClickMouseBtn += action;
                    return true;
                }

                return false;
            }

            public override ICommand RemoveListener(int mouseBtn, KeyPressType type, UnityAction<int> action)
            {
                if (MouseBtn == mouseBtn && KeyPressType == type)
                {
                    if (OnClickMouseBtn != null)
                    {
                        OnClickMouseBtn -= action;
                    }
                }

                if (OnClickMouseBtn == null || OnClickMouseBtn.GetInvocationList().Length == 0)
                {
                    return this;
                }

                return null;
            }
        }
    }
}