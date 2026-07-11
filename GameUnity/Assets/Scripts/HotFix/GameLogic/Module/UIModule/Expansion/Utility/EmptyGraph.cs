using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class EmptyGraph : Graphic, ICanvasRaycastFilter
    {
        public bool Debug = false;
        
        private RectTransform m_target;

        public void SetTarget(RectTransform target)
        {
            m_target = target;
        }
        
        public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            if (m_target == null)
            {
                return true;
            }

            return !RectTransformUtility.RectangleContainsScreenPoint(m_target, sp, eventCamera);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

#if UNITY_EDITOR

            if (Debug)
            {
                base.OnPopulateMesh(vh);
            }

#endif
        }
    }
}