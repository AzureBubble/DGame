using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal partial class InputModule
    {
        private abstract class ICommand
        {
            protected abstract IInputModule InputModule { get; }

            public abstract void Execute();

            public virtual bool AddListener(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action) => false;

            public virtual ICommand RemoveListener(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action) =>
                null;

            public virtual bool AddListener(int mouseBtn, KeyPressType type, UnityAction<int> action) => false;

            public virtual ICommand RemoveListener(int mouseBtn, KeyPressType type, UnityAction<int> action) => null;

            public virtual bool AddListener(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action) =>
                false;

            public virtual ICommand RemoveListener(HotKeyNameType hotKeyType, KeyPressType type,
                UnityAction<float> action) => null;

            public virtual bool AddListener(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action) =>
                false;

            public virtual ICommand RemoveListener(List<KeyCode> keyCodes, KeyPressType type, float continueTime,
                UnityAction<List<KeyCode>> action) => null;
        }
    }
}