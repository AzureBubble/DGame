using System;
using UnityEngine.UI;

namespace Launcher
{
    public enum MessageShowType : byte
    {
        None = 0,
        OneButton = 1,
        TwoButton = 2,
        ThreeButton = 3,
    }

    public class LoadTipsUI : UIBase
    {
        #region 脚本工具生成的代码

        private Button m_btnPackage;
        private Text m_textPackage;
        private Text m_textDesc;
        private Button m_btnUpdate;
        private Text m_textUpdate;
        private Button m_btnIgnore;
        private Text m_textIgnore;

        protected override void ScriptGenerator()
        {
            m_btnPackage = FindChildComponent<Button>("m_btnPackage/m_btnPackage");
            m_textPackage = FindChildComponent<Text>("m_textPackage/m_textPackage/m_textPackage");
            m_textDesc = FindChildComponent<Text>("m_textDesc/m_textDesc");
            m_btnUpdate = FindChildComponent<Button>("m_btnUpdate/m_btnUpdate/m_btnUpdate");
            m_textUpdate = FindChildComponent<Text>("m_textUpdate/m_textUpdate/m_textUpdate/m_textUpdate");
            m_btnIgnore = FindChildComponent<Button>("m_btnIgnore/m_btnIgnore/m_btnIgnore");
            m_textIgnore = FindChildComponent<Text>("m_textIgnore/m_textIgnore/m_textIgnore/m_textIgnore");
            m_btnPackage.onClick.AddListener(OnClickPackageBtn);
            m_btnUpdate.onClick.AddListener(OnClickUpdateBtn);
            m_btnIgnore.onClick.AddListener(OnClickIgnoreBtn);
        }

        #endregion

        public Action OnOk;
        public Action OnCancel;
        public MessageShowType ButtonShowType = MessageShowType.None;


        public override void OnInit(object data)
        {
            m_btnUpdate.gameObject.SetActive(false);
            m_btnIgnore.gameObject.SetActive(false);
            m_btnPackage.gameObject.SetActive(false);

            switch (ButtonShowType)
            {
                case MessageShowType.OneButton:
                    m_btnUpdate.gameObject.SetActive(true);
                    break;

                case MessageShowType.TwoButton:
                    m_btnUpdate.gameObject.SetActive(true);
                    m_btnIgnore.gameObject.SetActive(true);
                    break;

                case MessageShowType.ThreeButton:
                    m_btnUpdate.gameObject.SetActive(true);
                    m_btnIgnore.gameObject.SetActive(true);
                    m_btnPackage.gameObject.SetActive(true);
                    break;
            }

            m_textDesc.text = data?.ToString();
        }

        private void OnClickPackageBtn()
        {
            OnOk?.Invoke();
            Close();
        }

        private void OnClickIgnoreBtn()
        {
            OnCancel?.Invoke();
            Close();
        }

        private void OnClickUpdateBtn()
        {
            OnOk?.Invoke();
            Close();
        }
    }
}