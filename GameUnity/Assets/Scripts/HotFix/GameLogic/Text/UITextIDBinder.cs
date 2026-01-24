using System;
using GameProto;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public enum UITextIDBinderResultType
    {
        /// <summary>
        /// 更新成功
        /// </summary>
        Success,

        /// <summary>
        /// 没有找到 Text 组件
        /// </summary>
        NoTextCom,

        /// <summary>
        /// 没有找到 TextID 的配置文件
        /// </summary>
        NoTextID,

        /// <summary>
        /// TextID = 0
        /// </summary>
        TextIDZero
    }

    [DisallowMultipleComponent]
    public class UITextIDBinder : MonoBehaviour
    {
        private static readonly Color32 SuccessColor = new Color32(0, 200, 0, 255);
        private static readonly Color32 ErrorColor = new Color32(200, 0, 0, 255);

        [SerializeField]
        [Header("文本配置ID")]
        private int m_textID;

        [SerializeField]
        [Header("预览语言(仅编辑器)")]
        private LocalizationType m_previewLanguage = LocalizationType.CN;

        [SerializeField]
        [Header("预览文本(仅编辑器,只读)")]
        private string m_previewText;

        /// <summary>
        /// 文本配置ID
        /// </summary>
        public int TextID
        {
            get => m_textID;
            set
            {
                if (m_textID != value)
                {
                    m_textID = value;
                    UpdateTextContent();
                }
            }
        }

        /// <summary>
        /// 预览语言
        /// </summary>
        public LocalizationType PreviewLanguage
        {
            get => m_previewLanguage;
            set
            {
                if (m_previewLanguage != value)
                {
                    m_previewLanguage = value;
                    UpdateTextContent();
                }
            }
        }

        private Text m_textBinder;

        public Text TextBinder => m_textBinder == null ? m_textBinder = GetComponent<Text>() : m_textBinder;

        private void Awake()
        {
            UpdateTextContent();
        }

        public UITextIDBinderResultType UpdateTextContent()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return m_textID == 0 ? UITextIDBinderResultType.TextIDZero : UITextIDBinderResultType.Success;
            }
#endif

            if (TextBinder == null)
            {
                return UITextIDBinderResultType.NoTextCom;
            }

            if (m_textID == 0)
            {
                m_previewText = string.Empty;
                return UITextIDBinderResultType.TextIDZero;
            }

            var textConfig = TextConfigMgr.Instance.GetTextConfig(m_textID);
            if (textConfig == null)
            {
                m_previewText = $"TextID:{m_textID} Not Found";
                return UITextIDBinderResultType.NoTextID;
            }

            int langIndex = (int)m_previewLanguage;
            if (langIndex < 0 || langIndex >= textConfig.Content.Length)
            {
                langIndex = 0;
            }

            m_previewText = textConfig.Content[langIndex];
            TextBinder.text = m_previewText;

            return UITextIDBinderResultType.Success;
        }

        /// <summary>
        /// 运行时更新文本（支持参数）
        /// </summary>
        public void SetText(params object[] args)
        {
            if (TextBinder != null && m_textID != 0)
            {
                TextBinder.text = TextConfigMgr.Instance.GetText(m_textID, args);
            }
        }

        /// <summary>
        /// 运行时更新文本（无参数）
        /// </summary>
        public void SetText()
        {
            SetText((object[])null);
        }
    }
}