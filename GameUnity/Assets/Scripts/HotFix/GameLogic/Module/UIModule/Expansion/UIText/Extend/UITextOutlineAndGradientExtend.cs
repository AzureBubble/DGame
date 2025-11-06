using System;
using UnityEngine;

namespace GameLogic
{
    public enum GradientType
    {
        NoColor = 0,
        TwoColor = 1,
        ThreeColor = 2,
        // FourColor = 3
    }

    [Serializable]
    public class UITextOutlineAndGradientExtend
    {
#pragma warning disable 0414

        [SerializeField] private bool m_isUseTextOutline;
        [SerializeField] private bool m_isUseTextGradient;
        [SerializeField] private bool m_isOpenShaderOutline = true;
        [SerializeField] private float m_lerpValue = 0f;
        [SerializeField] private UITextOutline m_textOutlineEx;
        [SerializeField, Range(1, 10)] private int m_outLineWidth = 1;
        [SerializeField] private GradientType m_gradientType = GradientType.TwoColor;
        [SerializeField] private Color32 m_gradientTopColor = Color.white;
        [SerializeField] private Color32 m_gradientMiddleColor = Color.white;
        [SerializeField] private Color32 m_gradientBottomColor = Color.white;
        [SerializeField] private Color32 m_outLineColor = Color.black;
        [SerializeField] private Camera m_camera;
        [SerializeField, Range(0f, 1f)] private float m_alpha = 1f;
        [SerializeField, Range(0.1f, 0.9f)] private float m_colorOffset = 0.5f;
        [SerializeField] private UITextOutlineAndGradientEffect m_textEffect;

        public UITextOutlineAndGradientEffect TextEffect => m_textEffect;

        public bool UseTextOutline { get => m_isUseTextOutline; set => m_isUseTextOutline = value; }
        public bool UseTextGradient { get => m_isUseTextGradient; set => m_isUseTextGradient = value; }
        public GradientType GradientType => m_gradientType;

#pragma warning restore 0414

        public void SaveSerializeData(UIText uiText)
        {
            m_textEffect = uiText.GetComponent<UITextOutlineAndGradientEffect>();

            if (m_textEffect == null)
            {
                int instanceID = uiText.GetInstanceID();
                UIText[] uiTextArray = Transform.FindObjectsOfType<UIText>();

                for (int i = 0; i < uiTextArray.Length; i++)
                {
                    if (uiTextArray[i].GetInstanceID() == instanceID)
                    {
                        m_textEffect = uiTextArray[i].gameObject.AddComponent<UITextOutlineAndGradientEffect>();
                        m_textEffect.hideFlags = HideFlags.HideInInspector;
                        break;
                    }
                }
            }

            if (m_camera == null)
            {
                m_camera = Camera.main;
                if (m_camera == null)
                {
                    m_camera = Transform.FindObjectOfType<Camera>();
                }
            }

            if (m_camera == null)
            {
                Debug.LogError("No Find The Main Camera!");
            }
        }

        public void SetAlpha(float alpha)
        {
            m_textEffect.SetAlpha(alpha);
        }
    }
}