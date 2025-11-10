using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    public interface IInputModule
    {
        void RegisterInputCommand(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action);
        void RegisterInputCommandDown(KeyCode keyCode, UnityAction<KeyCode> action);
        void RegisterInputCommandStay(KeyCode keyCode, UnityAction<KeyCode> action);
        void RegisterInputCommandUp(KeyCode keyCode, UnityAction<KeyCode> action);

        void RemoveInputCommand(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action, bool isRemove = true);

        void RemoveInputCommandDown(KeyCode keyCode, UnityAction<KeyCode> action);
        void RemoveInputCommandStay(KeyCode keyCode, UnityAction<KeyCode> action);
        void RemoveInputCommandUp(KeyCode keyCode, UnityAction<KeyCode> action);

        void RebindInputCommand(KeyCode newKeyCode, KeyCode oldKeyCode);

        bool TryGetRebindKeyCode(KeyCode oldKeyCode, out KeyCode newKeyCode);

        void RegisterInputCommand(int mouseBtn, KeyPressType type, UnityAction<int> action);
        void RegisterInputCommandDown(int mouseBtn, UnityAction<int> action);
        void RegisterInputCommandStay(int mouseBtn, UnityAction<int> action);
        void RegisterInputCommandUp(int mouseBtn, UnityAction<int> action);

        void RemoveInputCommand(int mouseBtn, KeyPressType type, UnityAction<int> action, bool isRemove = true);
        void RemoveInputCommandDown(int mouseBtn, UnityAction<int> action);
        void RemoveInputCommandStay(int mouseBtn, UnityAction<int> action);
        void RemoveInputCommandUp(int mouseBtn, UnityAction<int> action);

        void RegisterInputCommandAxis(HotKeyNameType hotKeyType, UnityAction<float> action);

        void RegisterInputCommandAxisRaw(HotKeyNameType hotKeyType, UnityAction<float> action);

        void RegisterInputCommand(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action);

        void RemoveInputCommandAxis(HotKeyNameType hotKeyType, UnityAction<float> action, bool isRemove = true);

        void RemoveInputCommandAxisRaw(HotKeyNameType hotKeyType, UnityAction<float> action, bool isRemove = true);

        void RemoveInputCommand(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action, bool isRemove = true);

        void RegisterInputCommand(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action);

        void RegisterInputCommandDown(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);
        void RegisterInputCommandStay(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);
        void RegisterInputCommandUp(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);

        void RemoveInputCommand(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action, bool isRemove = true);
        void RemoveInputCommandDown(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);
        void RemoveInputCommandStay(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);
        void RemoveInputCommandUp(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action);

        void SaveRebindKeyMap();
    }
}