using System;
using GameProto;
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
            m_animatorAgent = UIFrameAnimatorAgent.Create();
        }

        protected override void OnDestroy()
        {
            m_animatorAgent?.Release();
        }

        #endregion

        #region 字段

        private UIFrameAnimatorAgent m_animatorAgent;
        private Action<UIWidget> m_clickAction;
        private int m_modelID;
        private ModelConfig m_modelCfg;

        #endregion

        #region 函数

        public async void Init(int modelID, Action<UIWidget> clickAction = null)
        {
            if (modelID <= 0)
            {
                return;
            }
            m_modelID = modelID;

            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
            m_modelCfg = TbModelConfig.GetOrDefault(modelID);
            await m_animatorAgent.Init(m_modelCfg);
            m_animatorAgent?.BindDisplayRender(m_btnSprite.image);
            m_animatorAgent?.StartAnim();
        }

        public void BindClickEvent(Action<UIWidget> clickAction)
        {
            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
        }

        public void SwitchAnim(UIFrameAnimState state)
        {
            m_animatorAgent?.SwitchAnim(state);
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