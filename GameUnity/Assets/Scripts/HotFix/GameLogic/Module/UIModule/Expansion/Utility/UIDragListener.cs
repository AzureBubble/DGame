using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    public class UIDragListener : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler,
        IPointerUpHandler
    {
        private event Action<PointerEventData> OnBeginDragAction;

        private event Action<PointerEventData> OnEndDragAction;

        private event Action<PointerEventData> OnDragAction;

        private event Action<PointerEventData> OnPointerDownAction;

        private event Action<PointerEventData> OnPointerUpAction;

        public static UIDragListener Get(GameObject go)
            => DGame.Utility.UnityUtil.AddMonoBehaviour<UIDragListener>(go);

        public static UIDragListener Get(GameObject go, Action<PointerEventData> onBeginDrag,
            Action<PointerEventData> onEndDrag, Action<PointerEventData> onDrag,
            Action<PointerEventData> onPointerDown, Action<PointerEventData> onPointerUp)
        {
            var com = DGame.Utility.UnityUtil.AddMonoBehaviour<UIDragListener>(go);
            com?.Init(onBeginDrag, onEndDrag, onDrag, onPointerDown, onPointerUp);
            return com;
        }

        public void Init(Action<PointerEventData> onBeginDrag, Action<PointerEventData> onEndDrag,
            Action<PointerEventData> onDrag, Action<PointerEventData> onPointerDown,
            Action<PointerEventData> onPointerUp)
        {
            AddOnBeginDragListener(onBeginDrag);
            AddOnEndDragListener(onEndDrag);
            AddOnDragListener(onDrag);
            AddOnPointerDownListener(onPointerDown);
            AddOnPointerUpListener(onPointerUp);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            OnBeginDragAction?.Invoke(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragAction?.Invoke(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            OnDragAction?.Invoke(eventData);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerDownAction?.Invoke(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OnPointerUpAction?.Invoke(eventData);
        }

        #region AddListener

        public void AddOnBeginDragListener(Action<PointerEventData> action)
        {
            OnBeginDragAction += action;
        }

        public void AddOnEndDragListener(Action<PointerEventData> action)
        {
            OnEndDragAction += action;
        }

        public void AddOnDragListener(Action<PointerEventData> action)
        {
            OnDragAction += action;
        }

        public void AddOnPointerDownListener(Action<PointerEventData> action)
        {
            OnPointerDownAction += action;
        }

        public void AddOnPointerUpListener(Action<PointerEventData> action)
        {
            OnPointerUpAction += action;
        }

        #endregion

        #region RemoveListener

        public void RemoveOnBeginDragListener(Action<PointerEventData> action)
        {
            OnBeginDragAction -= action;
        }

        public void RemoveOnEndDragListener(Action<PointerEventData> action)
        {
            OnEndDragAction -= action;
        }

        public void RemoveOnDragListener(Action<PointerEventData> action)
        {
            OnDragAction -= action;
        }

        public void RemoveOnPointerDownListener(Action<PointerEventData> action)
        {
            OnPointerDownAction -= action;
        }

        public void RemoveOnPointerUpListener(Action<PointerEventData> action)
        {
            OnPointerUpAction -= action;
        }

        #endregion

        private void OnDestroy()
        {
            Clear();
        }

        public void Clear()
        {
            OnBeginDragAction = null;
            OnEndDragAction = null;
            OnDragAction = null;
            OnPointerDownAction = null;
            OnPointerUpAction = null;
        }
    }
}