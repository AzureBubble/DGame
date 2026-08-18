using System;
using Cysharp.Threading.Tasks;
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
            m_initVersion++;
            var animatorAgent = m_animatorAgent;
            m_animatorAgent = null;
            animatorAgent?.Release();
            m_clickAction = null;
        }

        #endregion

        #region 字段

        private UIFrameAnimatorAgent m_animatorAgent;
        private Action<UIWidget> m_clickAction;
        private int m_modelID;
        private ModelConfig m_modelCfg;
        private int m_initVersion;

        #endregion

        #region 函数

        /// <summary>
        /// 初始化帧动画Widget，异步加载模型资源
        /// </summary>
        /// <param name="modelID">模型ID</param>
        /// <param name="clickAction">点击回调</param>
        public void Init(int modelID, Action<UIWidget> clickAction = null)
            => InitAsync(modelID, clickAction).Forget();

        private async UniTask InitAsync(int modelID, Action<UIWidget> clickAction)
        {
            if (modelID <= 0 || IsDestroyed || m_animatorAgent == null)
            {
                return;
            }
            int initVersion = ++m_initVersion;
            m_modelID = modelID;

            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
            m_modelCfg = ModelConfigMgr.Instance.GetOrDefault(modelID);
            var animatorAgent = m_animatorAgent;
            var spriteButton = m_btnSprite;
            await animatorAgent.Init(m_modelCfg);

            if (initVersion != m_initVersion || IsDestroyed || gameObject == null ||
                animatorAgent != m_animatorAgent || spriteButton == null)
            {
                return;
            }

            animatorAgent.BindDisplayRender(spriteButton.image);
            animatorAgent.StartAnim();
        }

        /// <summary>
        /// 绑定点击事件
        /// </summary>
        /// <param name="clickAction">点击回调</param>
        public void BindClickEvent(Action<UIWidget> clickAction)
        {
            m_clickAction = clickAction;
            m_btnSprite.interactable = clickAction != null;
        }

        /// <summary>
        /// 切换动画状态
        /// </summary>
        /// <param name="state">目标动画状态</param>
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
