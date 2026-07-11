using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    internal sealed class GuideClickListener : MonoBehaviour, IPointerClickHandler
    {
        private Action m_onClick;

        internal void Bind(Action onClick)
        {
            m_onClick = onClick;
        }

        internal void Clear()
        {
            m_onClick = null;
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_onClick?.Invoke();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}