using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    [DisallowMultipleComponent]
    [System.Serializable]
    public abstract class BaseUIButton : Button, IUpdateSelectedHandler
    {
        #region Properties

        [SerializeField] private UnityEvent m_buttonClickEvent = new UnityEvent(); // 按钮可点击时候触发
        [SerializeField] private UIButtonClickProtectExtend m_uiButtonClickProtect = new UIButtonClickProtectExtend();
        [SerializeField] private UIButtonClickScaleExtend m_uiButtonClickScale = new UIButtonClickScaleExtend();
        [SerializeField] private UIButtonLongPressExtend m_uiButtonLongPress = new UIButtonLongPressExtend();
        [SerializeField] private UIButtonDoubleClickExtend m_uiButtonDoubleClick = new UIButtonDoubleClickExtend();
        [SerializeField] private UIButtonClickSoundExtend m_uiButtonClickSound = new UIButtonClickSoundExtend();

        private Vector2 m_pressPos; // 按下的坐标
        private bool m_isPress; // 是否按下
        private bool m_isClickDown;
        private PointerEventData m_pointerEventData;
        private bool m_deferClickScaleUntilClick;
        private bool m_deferClickScaleForCurrentPress;
        private bool m_longPressConsumedClick;
        public Action OnPointerUpEvent; // 按钮不可点击也触发
        public PointerEventData CurrentPointerEventData => m_pointerEventData;

        public UIButtonClickScaleExtend ClickScaleExtend => m_uiButtonClickScale;

        /// <summary>
        /// 将缩放反馈延迟到确认点击后播放，供需要区分滚动与点击的控件启用。
        /// </summary>
        public bool DeferClickScaleUntilClick
        {
            get => m_deferClickScaleUntilClick;
            set
            {
                m_deferClickScaleUntilClick = value;
                if (value && m_isPress)
                {
                    m_deferClickScaleForCurrentPress = true;
                    ResetDeferredClickScale();
                }
            }
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();
            m_uiButtonClickProtect?.Awake();
        }

        protected override void OnEnable()
        {
            m_uiButtonClickProtect?.OnEnable();
            m_uiButtonClickScale?.OnEnable(transform);
        }

        public void OnUpdateSelected(BaseEventData eventData)
        {
            m_uiButtonLongPress?.OnUpdateSelected();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (m_uiButtonClickProtect.IsUseClickProtect && !m_uiButtonClickProtect.CanClick)
            {
                return;
            }

            if (m_longPressConsumedClick ||
                (m_deferClickScaleForCurrentPress && !eventData.eligibleForClick))
            {
                return;
            }

            if (interactable)
            {
                if (m_deferClickScaleForCurrentPress && eventData.button == PointerEventData.InputButton.Left)
                {
                    m_uiButtonClickScale?.PlayClickFeedback(transform, true);
                    m_buttonClickEvent?.Invoke();
                }
                m_uiButtonClickSound?.OnPointerClick();
                base.OnPointerClick(eventData);
                // onClick?.Invoke();
            }
            // 连点保护在这里触发抬起事件 才算完成一次点击 开始倒计时
            m_uiButtonClickProtect?.OnPointerClick();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (!m_uiButtonClickProtect.CanClick)
            {
                return;
            }
            base.OnPointerDown(eventData);
            m_pressPos = eventData.position;
            m_isPress = true;
            m_pointerEventData = eventData;
            m_isClickDown = true;
            m_deferClickScaleForCurrentPress = m_deferClickScaleUntilClick;
            m_longPressConsumedClick = false;
            m_uiButtonClickProtect?.OnPointerDown();
            m_uiButtonLongPress?.OnPointerDown();
            m_uiButtonDoubleClick?.OnPointerDown();
            if (m_deferClickScaleForCurrentPress)
            {
                ResetDeferredClickScale();
            }
            else
            {
                m_uiButtonClickScale?.OnPointerDown(transform, interactable);
            }

            if (interactable)
            {
                m_uiButtonClickSound?.OnPointerDown();
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (!m_isClickDown || !m_uiButtonClickProtect.CanClick)
            {
                return;
            }

            if (m_isClickDown)
            {
                m_isClickDown = false;
            }
            base.OnPointerUp(eventData);
            m_isPress = false;
            m_pointerEventData = null;

            if (!m_deferClickScaleForCurrentPress && interactable &&
                Mathf.Abs(Vector2.Distance(m_pressPos, eventData.position)) < 10f)
            {
                m_buttonClickEvent?.Invoke();
            }
            OnPointerUpEvent?.Invoke();
            m_uiButtonClickProtect?.OnPointerUp();
            m_uiButtonLongPress?.OnPointerUp();
            if (!m_deferClickScaleForCurrentPress)
            {
                m_uiButtonClickScale?.OnPointerUp(transform, interactable);
            }
            if (interactable)
            {
                m_uiButtonClickSound?.OnPointerUp();
            }
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Update()
        {
            m_uiButtonClickProtect?.OnUpdate();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                if (m_isPress && m_pointerEventData != null)
                {
                    OnPointerUp(m_pointerEventData);
                }
            }
        }

        protected override void OnDisable()
        {
            m_uiButtonClickScale?.OnDestroy(transform);
        }

        protected override void OnDestroy()
        {
            m_uiButtonClickScale?.OnDestroy(transform);
        }

        /// <summary>
        /// 添加按钮长按时间
        /// </summary>
        /// <param name="callback">长按后回调</param>
        /// <param name="duration">长按持续时间</param>
        public void AddButtonLongPressListener(UnityAction callback, float duration)
        {
            m_uiButtonLongPress?.AddLongPressListener(() => OnLongPressTriggered(callback), duration);
        }

        /// <summary>
        /// 添加按钮长按持续触发时间
        /// </summary>
        /// <param name="callback">长按后回调</param>
        /// <param name="interval">长按持续触发间隔</param>
        public void AddButtonLoopLongPressListener(UnityAction callback, float interval)
        {
            m_uiButtonLongPress?.AddLoopLongPressListener(() => OnLongPressTriggered(callback), interval);
        }

        /// <summary>
        /// 长按已处理本次操作时取消短按，避免松手后再次播放点击反馈或执行点击。
        /// </summary>
        private void OnLongPressTriggered(UnityAction callback)
        {
            if (m_deferClickScaleForCurrentPress)
            {
                m_longPressConsumedClick = true;
                if (m_pointerEventData != null)
                {
                    m_pointerEventData.eligibleForClick = false;
                }
            }
            callback?.Invoke();
        }

        /// <summary>
        /// 清理待判定手势的缩放反馈，并保留原本关闭缩放的控件外观。
        /// </summary>
        private void ResetDeferredClickScale()
        {
            if (m_uiButtonClickScale?.IsUseClickScale == true)
            {
                m_uiButtonClickScale.OnEnable(transform);
            }
        }

        /// <summary>
        /// 添加按钮双击触发事件
        /// </summary>
        /// <param name="callback">双击触发回调</param>
        /// <param name="interval">双击时间间隔</param>
        public void AddButtonDoubleClickListener(UnityAction callback, float interval)
        {
            m_uiButtonDoubleClick?.AddDoubleClickListener(callback, interval);
        }

        /// <summary>
        /// 设置点击音效ID
        /// </summary>
        /// <param name="soundID">音效ID</param>
        public void SetClickSoundID(int soundID)
        {
            m_uiButtonClickSound?.SetClickSoundID(soundID);
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            // if (m_buttonClickScaleExtend.UseClickScale)
            // {
            //     transition = Transition.None;
            // }
            //Navigation tempNavigation = navigation;
            //tempNavigation.mode = Navigation.Mode.None;
            //navigation = tempNavigation;
        }

#endif
    }
}
