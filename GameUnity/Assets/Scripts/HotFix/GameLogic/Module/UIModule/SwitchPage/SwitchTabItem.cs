using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class SwitchTabItem : UIEventItem<SwitchTabItem>
    {
        #region 脚本工具生成的代码

        private Transform m_tfNoSelectNode;
        private Image m_imgNoSelectBg;
        private Image m_imgNoSelectIcon;
        private Text m_textNoSelectText;
        private Transform m_tfSelectedNode;
        private Image m_imgSelectedBg;
        private Image m_imgSelectedIcon;
        private Text m_textSelectedText;
        private Transform m_tfRedNode;

        protected override void ScriptGenerator()
        {
            m_tfNoSelectNode = FindChild("m_tfNoSelectNode");
            m_imgNoSelectBg = FindChildComponent<Image>("m_tfNoSelectNode/m_imgNoSelectBg");
            m_imgNoSelectIcon = FindChildComponent<Image>("m_tfNoSelectNode/m_imgNoSelectIcon");
            m_textNoSelectText = FindChildComponent<Text>("m_tfNoSelectNode/m_textNoSelectText");
            m_tfSelectedNode = FindChild("m_tfSelectedNode");
            m_imgSelectedBg = FindChildComponent<Image>("m_tfSelectedNode/m_imgSelectedBg");
            m_imgSelectedIcon = FindChildComponent<Image>("m_tfSelectedNode/m_imgSelectedIcon");
            m_textSelectedText = FindChildComponent<Text>("m_tfSelectedNode/m_textSelectedText");
            m_tfRedNode = FindChild("m_tfRedNode");
        }

        #endregion

        #region Properties

        protected bool m_selected;
        public bool Selected { get => m_selected; set => SetSelectedState(value); }

        #endregion

        #region 函数

        public void SetTabIcon(string selectedIconPath, string noSelectIconPath)
        {
            m_imgSelectedIcon?.SetSprite(selectedIconPath, true);
            m_imgNoSelectIcon?.SetSprite(noSelectIconPath, true);
        }

        public void SetTabIconPos(Vector2 selectedIconPos, Vector2 noSelectIconPos)
        {
            if (m_imgSelectedIcon != null && m_imgSelectedIcon.rectTransform != null)
            {
                m_imgSelectedIcon.rectTransform.localPosition = selectedIconPos;
            }
            if (m_imgNoSelectIcon != null && m_imgNoSelectIcon.rectTransform != null)
            {
                m_imgNoSelectIcon.rectTransform.localPosition = noSelectIconPos;
            }
        }

        public void UpdateTabName(string tabName)
        {
            if (m_textSelectedText != null)
            {
                m_textSelectedText.text = tabName;
            }
            if (m_textNoSelectText != null)
            {
                m_textNoSelectText.text = tabName;
            }
        }

        public void UpdateTabNameChangeSize(string tabName, bool isChangeSize = true)
        {
            if (m_textSelectedText != null)
            {
                m_textSelectedText.text = tabName;

                if (isChangeSize)
                {
                    m_textSelectedText.rectTransform.sizeDelta = new Vector2(m_textSelectedText.preferredWidth,
                        m_textSelectedText.rectTransform.sizeDelta.y);
                }
            }
            if (m_textNoSelectText != null)
            {
                m_textNoSelectText.text = tabName;
                if (isChangeSize)
                {
                    m_textNoSelectText.rectTransform.sizeDelta = new Vector2(m_textNoSelectText.preferredWidth,
                        m_textNoSelectText.rectTransform.sizeDelta.y);
                }
            }
        }

        public void SetTabTextFontSize(int fontSize)
        {
            if (m_textSelectedText != null)
            {
                m_textSelectedText.fontSize = fontSize;
            }

            if (m_textNoSelectText != null)
            {
                m_textNoSelectText.fontSize = fontSize;
            }
        }

        public void SetTabTextColor(string selectedTextColor, string noSelectTextColor)
        {
            if (m_textSelectedText != null)
            {
                m_textSelectedText.color = DGame.Utility.Converter.HexToColor(selectedTextColor);
            }

            if (m_textNoSelectText != null)
            {
                m_textNoSelectText.color = DGame.Utility.Converter.HexToColor(noSelectTextColor);
            }
        }

        public void SetTabBg(string selectedBgPath, string noSelectBgPath)
        {
            if (!string.IsNullOrEmpty(selectedBgPath))
            {
                m_imgSelectedBg?.SetSprite(selectedBgPath);
            }
            if (!string.IsNullOrEmpty(noSelectBgPath))
            {
                m_imgNoSelectBg?.SetSprite(noSelectBgPath);
            }
        }

        public virtual void SetSelectedState(bool isSelected)
        {
            m_selected = isSelected;
            m_tfSelectedNode?.SetActive(isSelected);
            m_tfNoSelectNode?.SetActive(!isSelected);
        }

        public virtual void SetRedNodeActive(bool isActive)
        {
            m_tfRedNode?.SetActive(isActive);
        }

        #endregion
    }
}