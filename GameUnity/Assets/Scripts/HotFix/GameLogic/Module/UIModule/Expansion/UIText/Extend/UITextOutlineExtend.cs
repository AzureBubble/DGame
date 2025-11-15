using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        private Text m_text;

        public UITextOutlineEffect TextEffect => m_textEffect;

        public bool UseTextOutline
        {
            get => m_isUseTextOutline;
            set
            {
                if (m_isUseTextOutline == value)
                {
                    return;
                }
                m_isUseTextOutline = value;
#if UNITY_EDITOR

                if (m_text != null)
                {
                    if (value)
                    {
                        if (!m_text.TryGetComponent(out m_textEffect))
                        {
                            m_textEffect = m_text.gameObject.AddComponent<UITextOutlineEffect>();
                            m_textEffect.hideFlags = HideFlags.HideInInspector;
                        }
                    }
                    else
                    {
                        if (m_textEffect != null || m_text.TryGetComponent(out m_textEffect))
                        {
                            GameObject.DestroyImmediate(m_textEffect);
                        }
                        m_textEffect = null;
                    }
                }
#endif
                Refresh();
            }
        }

#pragma warning restore 0414

        public void SaveSerializeData(UIText uiText)
        {
            m_text = uiText;
            if (!m_isUseTextOutline) return;

            if (!uiText.TryGetComponent(out m_textEffect))
            {
                m_textEffect = uiText.gameObject.AddComponent<UITextOutlineEffect>();
                m_textEffect.hideFlags = HideFlags.HideInInspector;
            }

            // if(!uiText.TryGetComponent(out m_textEffect))
            // {
            //     int instanceID = uiText.GetInstanceID();
            //     UIText[] uiTextArray = Transform.FindObjectsOfType<UIText>();
            //
            //     for (int i = 0; i < uiTextArray.Length; i++)
            //     {
            //         if (uiTextArray[i].GetInstanceID() == instanceID)
            //         {
            //             m_textEffect = uiTextArray[i].gameObject.AddComponent<UITextOutlineEffect>();
            //             m_textEffect.hideFlags = HideFlags.HideInInspector;
            //             break;
            //         }
            //     }
            // }

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

        public void Refresh()
        {
            m_text?.SetVerticesDirty();
        }
    }
}