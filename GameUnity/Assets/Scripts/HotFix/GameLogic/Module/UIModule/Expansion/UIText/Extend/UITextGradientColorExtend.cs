using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace GameLogic
{
    [Serializable]
    public class UITextGradientColorExtend
    {
#pragma warning disable 0414
        [SerializeField]
        private bool m_isUseGradientColor = false;
        [SerializeField]
        private Color m_colorTop = Color.white;
        [SerializeField]
        private Color m_colorBottom = Color.white;
        [SerializeField]
        private Color m_colorLeft = Color.white;
        [SerializeField]
        private Color m_colorRight = Color.white;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetVertical = 0f;
        [SerializeField, Range(-1f, 1f)]
        private float m_gradientOffsetHorizontal = 0f;
        [SerializeField]
        private bool m_splitTextGradient = false;
        [SerializeField]private UITextGradientColor m_gradientEffect;

        public UITextGradientColor GradientEffect => m_gradientEffect;
#pragma warning disable 0414
        public void SaveSerializeData(UIText uiText)
        {
            if (!m_isUseGradientColor) return;

            if(!uiText.TryGetComponent(out m_gradientEffect))
            {
                m_gradientEffect = uiText.gameObject.AddComponent<UITextGradientColor>();
                m_gradientEffect.hideFlags = HideFlags.HideInInspector;
            }
        }

        public void SetGradientColor(Color32 colorTop, Color32 colorBottom, Color32 colorLeft = default, Color32 colorRight = default, float verticalOffset = 0f, float horizontalOffset = 0f, bool splitTextGradient = false)
        {
            m_isUseGradientColor = true;
            m_gradientEffect.UseGradientColor = m_isUseGradientColor;
            m_colorTop = colorTop;
            m_colorBottom = colorBottom;
            m_colorLeft = colorLeft;
            m_colorRight = colorRight;
            m_splitTextGradient = splitTextGradient;
            m_gradientOffsetVertical = verticalOffset;
            m_gradientOffsetHorizontal = horizontalOffset;
            m_gradientEffect.colorTop = colorTop;
            m_gradientEffect.colorBottom = colorBottom;
            m_gradientEffect.colorLeft = colorLeft;
            m_gradientEffect.colorRight = colorRight;
            m_gradientEffect.gradientOffsetVertical = verticalOffset;
            m_gradientEffect.gradientOffsetHorizontal = horizontalOffset;
            m_gradientEffect.splitTextGradient = splitTextGradient;
        }
    }
}