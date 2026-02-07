using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    public class UIFrameWidget : UIWidget
    {
        #region 脚本工具生成的代码

        private Image m_imgSprite;
        private Transform m_tfEffRoot;

        protected override void ScriptGenerator()
        {
            m_imgSprite = FindChildComponent<Image>("m_imgSprite");
            m_tfEffRoot = FindChild("m_tfEffRoot");
        }

        #endregion


        #region Override

        protected override void BindMemberProperty()
        {
            m_animatorAgent = FrameAnimatorAgent.Create();
        }

        protected override void OnDestroy()
        {
            m_animatorAgent?.Release();
        }

        #endregion

        #region 字段

        private FrameAnimatorAgent m_animatorAgent;

        #endregion
    }
}