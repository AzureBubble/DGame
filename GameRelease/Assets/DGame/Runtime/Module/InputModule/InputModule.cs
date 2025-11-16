using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal sealed partial class InputModule : Module, IInputModule, IUpdateModule
    {
        private const string SAVE_KEY = "INPUT_MODULE_REBIND_KEY_MAP";

        public override int Priority => 999;

        private readonly List<ICommand> m_commands = new List<ICommand>(16);
        // key -> oldKey value -> newKey
        private Dictionary<KeyCode, KeyCode> m_keyRebuildMap;
        private readonly Dictionary<int, List<KeyPressType>> m_mouseButtons = new Dictionary<int, List<KeyPressType>>(8); // 存储已经注册按键
        private readonly Dictionary<HotKeyNameType, List<KeyPressType>> m_hotKeys = new Dictionary<HotKeyNameType, List<KeyPressType>>(4); // 存储已经注册按键
        private readonly Dictionary<KeyCode, List<KeyPressType>> m_keyCodes = new Dictionary<KeyCode, List<KeyPressType>>(16); // 存储已经注册按键
        private readonly Dictionary<int, List<KeyPressType>> m_comboKeyCodes = new Dictionary<int, List<KeyPressType>>(8);
        private readonly List<ICommand> m_toDelete = new List<ICommand>();

        private bool m_canInput = true;
        public bool CanInput { get => m_canInput; set => m_canInput = value; }

        private bool m_isCommandDirty = false;

        public override void OnCreate()
        {
            m_keyRebuildMap = LoadRebindKeyMap();
        }

        public override void OnDestroy()
        {
            m_commands.Clear();
            m_keyRebuildMap.Clear();
            m_mouseButtons.Clear();
            m_hotKeys.Clear();
            m_keyCodes.Clear();
            m_toDelete.Clear();
        }

        public void RegisterInputCommandDown(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Down;
            RegisterInputCommand(keyCode, type, action);
        }

        public void RegisterInputCommandStay(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RegisterInputCommand(keyCode, type, action);
        }

        public void RegisterInputCommandUp(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Up;
            RegisterInputCommand(keyCode, type, action);
        }

        public void RegisterInputCommand(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action)
        {
            if (m_keyCodes.TryGetValue(keyCode, out var tempList) && tempList.Contains(type))
            {
                foreach (ICommand command in m_commands)
                {
                    if (command.AddListener(keyCode, type, action))
                    {
                        return;
                    }
                }
            }

            if (!m_keyCodes.TryGetValue(keyCode, out tempList))
            {
                tempList = new List<KeyPressType>(3) { type };
                m_keyCodes.Add(keyCode, tempList);
            }
            if (!tempList.Contains(type))
            {
                tempList.Add(type);
            }
            // 把按键命令注册到列表容器中
            m_commands.Add(new KeyCodeCommand(this, keyCode, type, action));
        }


        public void RemoveInputCommandDown(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Down;
            RemoveInputCommand(keyCode, type, action);
        }

        public void RemoveInputCommandStay(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RemoveInputCommand(keyCode, type, action);
        }

        public void RemoveInputCommandUp(KeyCode keyCode, UnityAction<KeyCode> action)
        {
            KeyPressType type = KeyPressType.Up;
            RemoveInputCommand(keyCode, type, action);
        }

        public void RemoveInputCommand(KeyCode keyCode, KeyPressType type, UnityAction<KeyCode> action, bool isRemove = true)
        {
            ICommand toDelete = null;

            if (m_keyCodes.TryGetValue(keyCode, out var tempList) && tempList.Contains(type))
            {
                for (var index = 0; index < m_commands.Count; index++)
                {
                    var command = m_commands[index];
                    toDelete = command.RemoveListener(keyCode, type, action);

                    if (toDelete != null)
                    {
                        break;
                    }
                }

                if (toDelete != null && isRemove)
                {
                    m_isCommandDirty = true;
                    m_toDelete.Add(toDelete);
                }
            }
        }

        public void RebindInputCommand(KeyCode newKeyCode, KeyCode oldKeyCode)
        {
            if (m_keyRebuildMap == null)
            {
                m_keyRebuildMap = new Dictionary<KeyCode, KeyCode>(16);
            }

            if (oldKeyCode == newKeyCode)
            {
                m_keyRebuildMap.Remove(oldKeyCode);
                return;
            }
            m_keyRebuildMap[oldKeyCode] = newKeyCode;
        }

        public bool TryGetRebindKeyCode(KeyCode oldKeyCode, out KeyCode newKeyCode)
        {
            if (m_keyRebuildMap == null)
            {
                newKeyCode = oldKeyCode;
                return false;
            }

            bool isRebind = m_keyRebuildMap.TryGetValue(oldKeyCode, out newKeyCode);

            if (!isRebind)
            {
                newKeyCode = oldKeyCode;
            }
            return isRebind;
        }

        public void RegisterInputCommandDown(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Down;
            RegisterInputCommand(mouseBtn, type, action);
        }

        public void RegisterInputCommandStay(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RegisterInputCommand(mouseBtn, type, action);
        }

        public void RegisterInputCommandUp(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Up;
            RegisterInputCommand(mouseBtn, type, action);
        }

        public void RegisterInputCommand(int mouseBtn, KeyPressType type, UnityAction<int> action)
        {
            if (m_mouseButtons.TryGetValue(mouseBtn, out var tempList) && tempList.Contains(type))
            {
                foreach (ICommand command in m_commands)
                {
                    if (command.AddListener(mouseBtn, type, action))
                    {
                        return;
                    }
                }
            }
            if (!m_mouseButtons.TryGetValue(mouseBtn, out tempList))
            {
                tempList = new List<KeyPressType>(3) { type };
                m_mouseButtons.Add(mouseBtn, tempList);
            }
            if (!tempList.Contains(type))
            {
                tempList.Add(type);
            }
            m_commands.Add(new MouseButtonCommand(this, mouseBtn, type, action));
        }

        public void RemoveInputCommandDown(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Down;
            RemoveInputCommand(mouseBtn, type, action);
        }

        public void RemoveInputCommandStay(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RemoveInputCommand(mouseBtn, type, action);
        }

        public void RemoveInputCommandUp(int mouseBtn, UnityAction<int> action)
        {
            KeyPressType type = KeyPressType.Up;
            RemoveInputCommand(mouseBtn, type, action);
        }

        public void RemoveInputCommand(int mouseBtn, KeyPressType type, UnityAction<int> action, bool isRemove = true)
        {
            ICommand toDelete = null;

            if (m_mouseButtons.TryGetValue(mouseBtn, out var tempList) && tempList.Contains(type))
            {
                for (var index = 0; index < m_commands.Count; index++)
                {
                    var command = m_commands[index];
                    toDelete = command.RemoveListener(mouseBtn, type, action);

                    if (toDelete != null)
                    {
                        break;
                    }
                }

                if (toDelete != null && isRemove)
                {
                    m_isCommandDirty = true;
                    m_toDelete.Add(toDelete);
                }
            }
        }

        public void RegisterInputCommandAxis(HotKeyNameType hotKeyType, UnityAction<float> action)
        {
            KeyPressType type = KeyPressType.Axis;
            RegisterInputCommand(hotKeyType, type, action);
        }

        public void RegisterInputCommandAxisRaw(HotKeyNameType hotKeyType, UnityAction<float> action)
        {
            KeyPressType type = KeyPressType.AxisRaw;
            RegisterInputCommand(hotKeyType, type, action);
        }

        public void RegisterInputCommand(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action)
        {
            if (m_hotKeys.TryGetValue(hotKeyType, out var tempList) && tempList.Contains(type))
            {
                foreach (ICommand command in m_commands)
                {
                    if (command.AddListener(hotKeyType, type, action))
                    {
                        return;
                    }
                }
            }
            if (!m_hotKeys.TryGetValue(hotKeyType, out tempList))
            {
                tempList = new List<KeyPressType>(3) { type };
                m_hotKeys.Add(hotKeyType, tempList);
            }
            if (!tempList.Contains(type))
            {
                tempList.Add(type);
            }
            m_commands.Add(new HotKeyCommand(this, hotKeyType, type, action));
        }

        public void RemoveInputCommandAxis(HotKeyNameType hotKeyType, UnityAction<float> action, bool isRemove = true)
        {
            KeyPressType type = KeyPressType.Axis;
            RegisterInputCommand(hotKeyType, type, action);
        }

        public void RemoveInputCommandAxisRaw(HotKeyNameType hotKeyType, UnityAction<float> action, bool isRemove = true)
        {
            KeyPressType type = KeyPressType.AxisRaw;
            RegisterInputCommand(hotKeyType, type, action);
        }

        public void RemoveInputCommand(HotKeyNameType hotKeyType, KeyPressType type, UnityAction<float> action, bool isRemove = true)
        {
            ICommand toDelete = null;

            if (m_hotKeys.TryGetValue(hotKeyType, out var tempList) && tempList.Contains(type))
            {
                for (var index = 0; index < m_commands.Count; index++)
                {
                    var command = m_commands[index];
                    toDelete = command.RemoveListener(hotKeyType, type, action);

                    if (toDelete != null)
                    {
                        break;
                    }
                }

                if (toDelete != null && isRemove)
                {
                    m_isCommandDirty = true;
                    m_toDelete.Add(toDelete);
                }
            }
        }

        public void RegisterInputCommand(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action)
        {
            var hashCode = keyCodes.GetHashCode();
            if (m_comboKeyCodes.TryGetValue(hashCode, out var tempList) && tempList.Contains(type))
            {
                foreach (ICommand command in m_commands)
                {
                    if (command.AddListener(keyCodes, type, continueTime, action))
                    {
                        return;
                    }
                }
            }
            if (!m_comboKeyCodes.TryGetValue(hashCode, out tempList))
            {
                tempList = new List<KeyPressType>(3) { type };
                m_comboKeyCodes.Add(hashCode, tempList);
            }
            if (!tempList.Contains(type))
            {
                tempList.Add(type);
            }
            m_commands.Add(new ComboCommand(this, keyCodes, type, continueTime, action));
        }

        public void RegisterInputCommandDown(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Down;
            RegisterInputCommand(keyCodes, type, continueTime, action);
        }

        public void RegisterInputCommandStay(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RegisterInputCommand(keyCodes, type, continueTime, action);
        }

        public void RegisterInputCommandUp(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Up;
            RegisterInputCommand(keyCodes, type, continueTime, action);
        }

        public void RemoveInputCommand(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action, bool isRemove = true)
        {
            ICommand toDelete = null;
            var hashCode = keyCodes.GetHashCode();
            if (m_comboKeyCodes.TryGetValue(hashCode, out var tempList) && tempList.Contains(type))
            {
                for (var index = 0; index < m_commands.Count; index++)
                {
                    var command = m_commands[index];
                    toDelete = command.RemoveListener(keyCodes, type, continueTime, action);

                    if (toDelete != null)
                    {
                        break;
                    }
                }

                if (toDelete != null && isRemove)
                {
                    m_isCommandDirty = true;
                    m_toDelete.Add(toDelete);
                }
            }
        }

        public void RemoveInputCommandDown(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Down;
            RemoveInputCommand(keyCodes, type, continueTime, action);
        }

        public void RemoveInputCommandStay(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Stay;
            RemoveInputCommand(keyCodes, type, continueTime, action);
        }

        public void RemoveInputCommandUp(List<KeyCode> keyCodes, float continueTime, UnityAction<List<KeyCode>> action)
        {
            KeyPressType type = KeyPressType.Up;
            RemoveInputCommand(keyCodes, type, continueTime, action);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (!CanInput || m_commands.Count <= 0) return;
            for (int i = 0; i < m_commands.Count; i++)
            {
                m_commands[i]?.Execute();
            }

            if (m_isCommandDirty)
            {
                for (int i = 0; i < m_toDelete.Count; i++)
                {
                    if (m_commands.Contains(m_toDelete[i]))
                    {
                        m_commands.Remove(m_toDelete[i]);
                    }
                }
                m_toDelete.Clear();
                m_isCommandDirty = false;
            }
        }

        public void EnableInput() => CanInput = true;

        public void DisableInput() => CanInput = false;

        public void SaveRebindKeyMap()
        {
            if (m_keyRebuildMap == null)
            {
                return;
            }
            string jsonStr = Utility.JsonUtil.ToJson(m_keyRebuildMap);
            Utility.PlayerPrefsUtil.SetString(SAVE_KEY, jsonStr);
        }

        private Dictionary<KeyCode, KeyCode> LoadRebindKeyMap()
        {
            string jsonStr = Utility.PlayerPrefsUtil.GetString(SAVE_KEY);
            return Utility.JsonUtil.ToObject<Dictionary<KeyCode, KeyCode>>(jsonStr);
        }
    }
}