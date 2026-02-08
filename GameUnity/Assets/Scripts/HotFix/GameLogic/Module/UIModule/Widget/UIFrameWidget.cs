using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIFrameWidget : UIWidget
    {
        #region 脚本工具生成的代码

        private Button m_btnSprite;
        private Transform m_tfEffRoot;

        protected override void ScriptGenerator()
        {
            m_btnSprite = FindChildComponent<Button>("m_btnSprite");
            m_tfEffRoot = FindChild("m_tfEffRoot");
            m_btnSprite.onClick.AddListener(OnClickSpriteBtn);
        }

        #endregion

        #region Override

        protected override void BindMemberProperty()
        {
            m_btnSprite.interactable = false;
            m_animatorAgent = FrameAnimatorAgent.Create();
        }

        protected override void OnDestroy()
        {
            m_animatorAgent?.Release();
        }

        #endregion

        #region 字段

        private FrameAnimatorAgent m_animatorAgent;
        private Action<UIWidget> m_clickAction;
        private string m_location;

        #endregion

        #region 函数

        public void Init(string location, Action<UIWidget> clickAction = null)
        {
            if (string.IsNullOrEmpty(m_location))
            {
                return;
            }
            m_location = location;

            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
        }

        public void BindClickEvent(Action<UIWidget> clickAction)
        {
            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
        }

        #endregion

        #region 事件

        private void OnClickSpriteBtn()
        {
            m_clickAction?.Invoke(this);
        }

        #endregion
    }
}