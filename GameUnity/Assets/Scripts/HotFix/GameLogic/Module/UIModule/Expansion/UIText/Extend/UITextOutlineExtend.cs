using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameLogic
{
    [Serializable]
    public class UITextOutlineExtend
    {
#pragma warning disable 0414

        [SerializeField] private bool m_isUseTextOutline;
        [SerializeField] private bool m_isOpenShaderOutline = true;
        [SerializeField] private float m_lerpValue = 0f;
        [FormerlySerializedAs("m_textOutlineEx")] [SerializeField] private UITextShaderOutline textShaderOutlineEx;
        [SerializeField, Range(1, 10)] private int m_outLineWidth = 1;
        [SerializeField] private Color m_outLineColor = Color.white;
        [SerializeField] private Camera m_camera;
        [SerializeField, Range(0f, 1f)] private float m_alpha = 1f;
        [SerializeField] private UITextOutlineEffect m_textEffect;

        public UITextOutlineEffect TextEffect => m_textEffect;

        public bool UseTextOutline { get => m_isUseTextOutline; set => m_isUseTextOutline = value; }

#pragma warning restore 0414

        public void SaveSerializeData(UIText uiText)
        {
            if (!m_isUseTextOutline) return;

            if(!uiText.TryGetComponent(out m_textEffect))
            {
                int instanceID = uiText.GetInstanceID();
                UIText[] uiTextArray = Transform.FindObjectsOfType<UIText>();

                for (int i = 0; i < uiTextArray.Length; i++)
                {
                    if (uiTextArray[i].GetInstanceID() == instanceID)
                    {
                        m_textEffect = uiTextArray[i].gameObject.AddComponent<UITextOutlineEffect>();
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