using DGame;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class SwitchTabItem : UIEventItem<SwitchTabItem>
    {
        #region Properties

        protected SwitchTabItemDataComponent m_dataComponent;
        protected Transform m_noSelectNode;
        protected Image m_noSelectBg;
        protected Image m_noSelectIcon;
        protected Text m_noSelectText;

        protected Transform m_selectedNode;
        protected Image m_selectedBg;
        protected Image m_selectedIcon;
        protected Text m_selectedText;

        protected Transform m_tfRedNode;

        protected bool m_selected;
        public bool Selected { get => m_selected; set => SetSelectedState(value); }

        #endregion

        #region override

        protected override void BindMemberProperty()
        {
            base.BindMemberProperty();
            m_dataComponent = gameObject.GetComponent<SwitchTabItemDataComponent>();

            if (m_dataComponent != null)
            {
                m_noSelectNode = m_dataComponent.m_noSelectNode;
                m_noSelectBg = m_dataComponent.m_noSelectBg;
                m_noSelectIcon = m_dataComponent.m_noSelectIcon;
                m_noSelectText = m_dataComponent.m_noSelectText;
                m_selectedNode = m_dataComponent.m_selectedNode;
                m_selectedBg = m_dataComponent.m_selectedBg;
                m_selectedIcon = m_dataComponent.m_selectedIcon;
                m_selectedText = m_dataComponent.m_selectedText;
                m_tfRedNode = m_dataComponent.m_tfRedNode;
            }
            else
            {
                m_noSelectNode = FindChild("NoSelectNode");
                m_tfRedNode =  FindChild("m_tfRedNode");
                m_tfRedNode.SetActive(false);
                if (m_noSelectNode != null)
                {
                    m_noSelectBg = FindChildComponent<Image>(m_noSelectNode, "noSelectBg");
                    m_noSelectIcon = FindChildComponent<Image>(m_noSelectNode, "noSelectIcon");
                    m_noSelectText = FindChildComponent<Text>(m_noSelectNode, "noSelectText");
                }

                m_selectedNode =  FindChild("SelectedNode");
                if (m_selectedNode != null)
                {
                    m_selectedBg = FindChildComponent<Image>(m_selectedNode, "selectedBg");
                    m_selectedIcon = FindChildComponent<Image>(m_selectedNode, "selectedIcon");
                    m_selectedText = FindChildComponent<Text>(m_selectedNode, "selectedText");
                }
            }

        }

        #endregion

        #region 函数

        public void SetTabIcon(string selectedIconPath, string noSelectIconPath)
        {
            m_selectedIcon?.SetSprite(selectedIconPath, true);
            m_noSelectIcon?.SetSprite(noSelectIconPath, true);
        }

        public void SetTabIconPos(Vector2 selectedIconPos, Vector2 noSelectIconPos)
        {
            ((RectTransform)m_selectedIcon.transform).localPosition = selectedIconPos;
            ((RectTransform)m_noSelectIcon.transform).localPosition = noSelectIconPos;
        }

        public void UpdateTabName(string tabName)
        {
            if (m_selectedText != null)
            {
                m_selectedText.text = tabName;
            }
            if (m_noSelectText != null)
            {
                m_noSelectText.text = tabName;
            }
        }

        public void UpdateTabNameChangeSize(string tabName, bool isChangeSize = true)
        {
            if (m_selectedText != null)
            {
                m_selectedText.text = tabName;

                if (isChangeSize)
                {
                    m_selectedText.rectTransform.sizeDelta = new Vector2(m_selectedText.preferredWidth,
                        m_selectedText.rectTransform.sizeDelta.y);
                }
            }
            if (m_noSelectText != null)
            {
                m_noSelectText.text = tabName;
                if (isChangeSize)
                {
                    m_noSelectText.rectTransform.sizeDelta = new Vector2(m_noSelectText.preferredWidth,
                        m_noSelectText.rectTransform.sizeDelta.y);
                }
            }
        }

        public void SetTabTextFontSize(int fontSize)
        {
            if (m_selectedText != null)
            {
                m_selectedText.fontSize = fontSize;
            }

            if (m_noSelectText != null)
            {
                m_noSelectText.fontSize = fontSize;
            }
        }

        public void SetTabTextColor(string selectedTextColor, string noSelectTextColor)
        {
            if (m_selectedText != null)
            {
                m_selectedText.color = DGame.Utility.Converter.HexToColor(selectedTextColor);
            }

            if (m_noSelectText != null)
            {
                m_noSelectText.color = DGame.Utility.Converter.HexToColor(noSelectTextColor);
            }
        }

        public void SetTabBg(string selectedBgPath, string noSelectBgPath)
        {
            if (!string.IsNullOrEmpty(selectedBgPath))
            {
                m_selectedBg?.SetSprite(selectedBgPath);
            }
            if (!string.IsNullOrEmpty(noSelectBgPath))
            {
                m_noSelectBg?.SetSprite(noSelectBgPath);
            }
        }

        public virtual void SetSelectedState(bool isSelected)
        {
            m_selected = isSelected;
            m_selectedNode?.SetActive(isSelected);
            m_noSelectNode?.SetActive(!isSelected);
        }

        public virtual void SetRedNodeActive(bool isActive)
        {
            m_tfRedNode.SetActive(isActive);
        }

        #endregion
    }
}