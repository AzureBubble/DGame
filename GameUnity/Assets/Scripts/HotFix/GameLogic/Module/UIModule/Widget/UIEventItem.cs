using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIEventItem<T> : UIWidget where T : UIEventItem<T>
    {
        private object m_eventParam1;
        private object m_eventParam2;
        private object m_eventParam3;
        public object EventParam1 => m_eventParam1;
        public object EventParam2 => m_eventParam2;
        public object EventParam3 => m_eventParam3;
        private Action<T> m_clickAction;
        private Action<T, bool> m_pressAction;
        private Action<T, PointerEventData> m_beginDragAction;
        private Action<T, PointerEventData> m_dragAction;
        private Action<T, PointerEventData> m_endDragAction;

        public void BindClickEventEx(Action<T> clickAction, object eParam1 = null, object eParam2 = null,
            object eParam3 = null, Selectable.Transition transition = Selectable.Transition.None)
        {
            if (m_clickAction == null)
            {
                var button = DGame.Utility.UnityUtil.AddMonoBehaviour<UIButton>(gameObject);
                button.transition = transition;
                button.onClick.AddListener(() => { m_clickAction?.Invoke(this as T); });
            }

            m_clickAction = clickAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindClickEvent(Action<T> clickAction, object eParam1 = null, object eParam2 = null,
            object eParam3 = null)
        {
            if (m_clickAction == null)
            {
                var button = DGame.Utility.UnityUtil.AddMonoBehaviour<UIButton>(gameObject);
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => { m_clickAction?.Invoke(this as T); });
            }

            m_clickAction = clickAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindBeginDragEvent(Action<T, PointerEventData> dragAction, object eParam1 = null,
            object eParam2 = null, object eParam3 = null)
        {
            if (m_beginDragAction == null)
            {
                var trigger = DGame.Utility.UnityUtil.AddMonoBehaviour<EventTrigger>(gameObject);
                EventTrigger.Entry entry = new EventTrigger.Entry
                {
                    eventID = EventTriggerType.BeginDrag,
                    callback = new EventTrigger.TriggerEvent()
                };
                entry.callback.AddListener(data => { m_beginDragAction?.Invoke(this as T, (PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            m_beginDragAction = dragAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindDragEvent(Action<T, PointerEventData> dragAction, object eParam1 = null, object eParam2 = null,
            object eParam3 = null)
        {
            if (m_dragAction == null)
            {
                var trigger = DGame.Utility.UnityUtil.AddMonoBehaviour<EventTrigger>(gameObject);
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Drag;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener(data => { m_dragAction?.Invoke(this as T, (PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            m_dragAction = dragAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindEndDragEvent(Action<T, PointerEventData> dragendAction, object eParam1 = null,
            object eParam2 = null, object eParam3 = null)
        {
            if (m_endDragAction == null)
            {
                m_endDragAction = dragendAction;
                var trigger = DGame.Utility.UnityUtil.AddMonoBehaviour<EventTrigger>(gameObject);
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.EndDrag;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener((data) => { m_endDragAction?.Invoke(this as T, (PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            m_endDragAction = dragendAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindPressEvent(Action<T, bool> pressAction, object eParam1 = null, object eParam2 = null,
            object eParam3 = null)
        {
            if (m_pressAction == null)
            {
                var trigger = DGame.Utility.UnityUtil.AddMonoBehaviour<EventTrigger>(gameObject);
                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerDown;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener(data => { m_pressAction?.Invoke(this as T, true); });
                trigger.triggers.Add(entry);
                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerUp;
                entry.callback = new EventTrigger.TriggerEvent();
                entry.callback.AddListener((data) => { m_pressAction?.Invoke(this as T, false); });
                trigger.triggers.Add(entry);
            }

            m_pressAction = pressAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void BindPressEvent(Action<T, bool> pressAction, object eParam1 = null, object eParam2 = null,
            object eParam3 = null, float durationThreshold = 1)
        {
            if (m_pressAction == null)
            {
                var button = DGame.Utility.UnityUtil.AddMonoBehaviour<UIButton>(gameObject);
                button.AddButtonLongPressListener(() => { m_pressAction?.Invoke(this as T, true); }, durationThreshold);
            }

            m_pressAction = pressAction;
            SetEventParam(eParam1, eParam2, eParam3);
        }

        public void SetEventParam(object eParam1, object eParam2 = null, object eParam3 = null)
        {
            m_eventParam1 = eParam1;
            m_eventParam2 = eParam2;
            m_eventParam3 = eParam3;
        }

        public void OnTriggerBtnEvent()
        {
            if (m_clickAction != null)
            {
                m_clickAction(this as T);
            }
        }
    }
}