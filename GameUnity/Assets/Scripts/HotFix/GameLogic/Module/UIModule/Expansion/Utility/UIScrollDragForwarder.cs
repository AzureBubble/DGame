using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 将子节点的拖拽转交给指定或父级滚动区域。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UIScrollDragForwarder : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private ScrollRect m_targetScrollRect;
        private ScrollRect m_parentScrollRect;
        private UIButton m_feedbackButton;
        private bool m_originalDeferClickScale;

        /// <summary>
        /// 判断当前拖拽是否允许转交，未设置时默认允许。
        /// </summary>
        public Func<bool> CanForwardDrag { get; set; }

        /// <summary>
        /// 本次按压是否已转交滚动，仅在下次有效左键按下时重置。
        /// </summary>
        public bool IsScrolling { get; private set; }

        /// <summary>
        /// 接收拖拽的滚动区域，未指定时从父节点开始自动查找并缓存。
        /// </summary>
        public ScrollRect TargetScrollRect
        {
            get
            {
                if (m_targetScrollRect != null)
                {
                    return m_targetScrollRect;
                }

                if (m_parentScrollRect == null && transform.parent != null)
                {
                    m_parentScrollRect = transform.parent.GetComponentInParent<ScrollRect>(true);
                }

                return m_parentScrollRect;
            }
            set => m_targetScrollRect = value;
        }

        /// <summary>
        /// 开始新的左键按压，清理上一次按压的滚动标记。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (isActiveAndEnabled && eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                IsScrolling = false;
                ConfigureButtonFeedback();
            }
        }

        /// <summary>
        /// 首次开始拖拽时转交目标，并补发目标所需的拖拽初始化事件。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isActiveAndEnabled || eventData == null ||
                eventData.button != PointerEventData.InputButton.Left || IsScrolling)
            {
                return;
            }

            var scroll = TargetScrollRect;
            if (scroll == null || scroll.gameObject == gameObject || !scroll.isActiveAndEnabled ||
                CanForwardDrag?.Invoke() == false)
            {
                return;
            }

            IsScrolling = true;
            eventData.eligibleForClick = false;
            eventData.pointerDrag = scroll.gameObject;
            ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
            ExecuteEvents.Execute(scroll.gameObject, eventData, ExecuteEvents.beginDragHandler);
        }

        /// <summary>
        /// 提供子节点的拖拽入口。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnDrag(PointerEventData eventData)
        {
            // 实现 IDragHandler 以参与 Unity 拖拽目标选择；转交后的 Drag/EndDrag 由 Unity 路由，不重复发送。
        }

        /// <summary>
        /// 重新挂接父节点时仅清理自动查找缓存，保留显式指定的目标。
        /// </summary>
        private void OnTransformParentChanged()
        {
            m_parentScrollRect = null;
        }

        /// <summary>
        /// 启用时推迟按钮缩放；没有 UIButton 的普通节点仍可转交滚动。
        /// </summary>
        private void OnEnable()
        {
            ConfigureButtonFeedback();
        }

        /// <summary>
        /// 回收或禁用时归还按钮设置，已判定的滚动标记保留到下一次按下。
        /// </summary>
        private void OnDisable()
        {
            RestoreButtonFeedback();
        }

        private void ConfigureButtonFeedback()
        {
            var button = GetComponent<UIButton>();
            if (button == m_feedbackButton)
            {
                return;
            }

            RestoreButtonFeedback();
            m_feedbackButton = button;
            if (m_feedbackButton != null)
            {
                m_originalDeferClickScale = m_feedbackButton.DeferClickScaleUntilClick;
                m_feedbackButton.DeferClickScaleUntilClick = true;
            }
        }

        private void RestoreButtonFeedback()
        {
            if (m_feedbackButton != null)
            {
                m_feedbackButton.DeferClickScaleUntilClick = m_originalDeferClickScale;
                m_feedbackButton = null;
            }
        }

        /// <summary>
        /// 销毁时释放外部拖拽条件委托。
        /// </summary>
        private void OnDestroy()
        {
            RestoreButtonFeedback();
            CanForwardDrag = null;
        }
    }
}
