using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DGame
{
    internal partial class InputModule
    {
        private class ComboCommand : ICommand
        {
            protected override IInputModule InputModule { get; }

            private List<KeyCode> KeyCodes { get; }
            private Queue<KeyCode> InputQueue { get; set; }
            private KeyPressType KeyPressType { get; }
            private UnityAction<List<KeyCode>> Action { get; set; }

            private float ContinueTime { get; }

            private float m_timer;

            public ComboCommand(IInputModule inputModule, List<KeyCode> keyCodes, KeyPressType keyPressType, float continueTime, UnityAction<List<KeyCode>> action)
            {
                if (keyCodes == null || inputModule == null)
                {
                    return;
                }
                ContinueTime = continueTime;
                InputModule = inputModule;
                KeyCodes = keyCodes;
                KeyPressType = keyPressType;
                Action = action;
                InputQueue = new Queue<KeyCode>(keyCodes);
            }

            public override void Execute()
            {
                if (InputQueue == null || InputQueue.Count == 0)
                {
                    return;
                }
                var keyCode = InputQueue.Peek();
                bool isReset = false;
                switch (KeyPressType)
                {
                    case KeyPressType.Down:
                        if (Input.GetKeyDown(keyCode))
                        {
                            InputQueue.Dequeue();

                            if (m_timer > 0 && Time.realtimeSinceStartup - m_timer > ContinueTime)
                            {
                                isReset = true;
                                break;
                            }
                            m_timer = Time.realtimeSinceStartup;
                        }
                        break;

                    case KeyPressType.Stay:
                        if (Input.GetKey(keyCode))
                        {
                            InputQueue.Dequeue();
                            if (m_timer > 0 && Time.realtimeSinceStartup - m_timer > ContinueTime)
                            {
                                isReset = true;
                                break;
                            }
                            m_timer = Time.realtimeSinceStartup;
                        }
                        break;

                    case KeyPressType.Up:
                        if (Input.GetKeyUp(keyCode))
                        {
                            InputQueue.Dequeue();
                            if (m_timer > 0 && Time.realtimeSinceStartup - m_timer > ContinueTime)
                            {
                                isReset = true;
                                break;
                            }
                            m_timer = Time.realtimeSinceStartup;
                        }
                        break;
                }

                if (isReset)
                {
                    m_timer = 0;
                    InputQueue.Clear();
                    for (int i = 0; i < KeyCodes.Count; i++)
                    {
                        InputQueue.Enqueue(KeyCodes[i]);
                    }

                    return;
                }

                if (InputQueue.Count <= 0)
                {
                    Action?.Invoke(KeyCodes);

                    for (int i = 0; i < KeyCodes.Count; i++)
                    {
                        InputQueue.Enqueue(KeyCodes[i]);
                    }
                }
            }

            public override bool AddListener(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action)
            {
                if (KeyPressType != type || ContinueTime != continueTime)
                {
                    return false;
                }
                for (int i = 0; i < keyCodes.Count; i++)
                {
                    if (!KeyCodes.Contains(keyCodes[i]))
                    {
                        return false;
                    }
                }
                Action += action;

                return true;
            }

            public override ICommand RemoveListener(List<KeyCode> keyCodes, KeyPressType type, float continueTime, UnityAction<List<KeyCode>> action)
            {
                if (KeyPressType != type || ContinueTime != continueTime)
                {
                    return null;
                }
                for (int i = 0; i < keyCodes.Count; i++)
                {
                    if (!KeyCodes.Contains(keyCodes[i]))
                    {
                        return null;
                    }
                }
                Action -= action;
                if (Action == null || Action.GetInvocationList().Length == 0)
                {
                    return this;
                }
                return null;
            }
        }
    }
}