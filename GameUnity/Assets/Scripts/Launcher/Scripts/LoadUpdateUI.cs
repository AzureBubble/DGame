using System;
using UnityEngine.UI;

namespace Launcher
{
    public class LoadUpdateUI : UIBase
    {
        #region 脚本工具生成的代码

        private Image m_imgBg;
        private Scrollbar m_scrollBarProgress;
        private Text m_textLabelDesc;
        private Text m_textVersion;
        private Text m_textLabelAppid;

        protected override void ScriptGenerator()
        {
            m_imgBg = FindChildComponent<Image>("m_imgBg");
            m_scrollBarProgress = FindChildComponent<Scrollbar>("m_scrollBarProgress");
            m_textLabelDesc = FindChildComponent<Text>("m_textLabelDesc/m_textLabelDesc");
            m_textVersion = FindChildComponent<Text>("m_textVersion");
            m_textLabelAppid = FindChildComponent<Text>("m_textLabelAppid");
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
            RefreshProgress(0f);
        }

        internal void RefreshProgress(float progress)
        {
            m_scrollBarProgress.gameObject.SetActive(true);
            m_scrollBarProgress.size = progress;
        }

        internal void RefreshVersion(string version)
        {
            m_textVersion.text = version;
        }

        internal void RefreshAppid(string appid)
        {
            m_textLabelAppid.text = appid;
        }
    }
}