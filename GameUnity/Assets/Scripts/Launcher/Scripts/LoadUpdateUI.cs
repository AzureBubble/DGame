using System;
using UnityEngine.UI;

namespace Launcher
{
    public class LoadUpdateUI : UIBase
    {
        #region 脚本工具生成的代码

        private Button m_btnClear;
        private Scrollbar m_scrollBarProgress;
        private Text m_textLabelDesc;
        private Text m_textLabelAppid;
        private Text m_textLabelResid;

        protected override void ScriptGenerator()
        {
            m_btnClear = FindChildComponent<Button>("m_btnClear");
            m_scrollBarProgress = FindChildComponent<Scrollbar>("m_scrollBarProgress");
            m_textLabelDesc = FindChildComponent<Text>("m_textLabelDesc");
            m_textLabelAppid = FindChildComponent<Text>("m_textLabelAppid");
            m_textLabelResid = FindChildComponent<Text>("m_textLabelResid");
            m_btnClear.onClick.AddListener(OnClickClearBtn);
        }

        #endregion

        public override bool FullScreen => true;
        public override bool NeedTween => false;

        public override void OnInit(object param)
        {
            if (param == null)
            {
                return;
            }
            base.OnInit(param);
            m_textLabelDesc.text = param.ToString();
            OnUpdateUIProgress(0f);
        }

        internal void OnUpdateUIProgress(float progress)
        {
            m_scrollBarProgress.gameObject.SetActive(true);
            m_scrollBarProgress.size = progress;
        }

        public void OnClickClearBtn()
        {

        }
    }
}