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
        public Button BtnUpdate;
        public Button BtnIgnore;
        public Button BtnPackage;
        public Text TextDesc;

        public Action OnOk;
        public Action OnCancel;
        public MessageShowType ButtonShowType = MessageShowType.None;

        private void Start()
        {
            BtnUpdate.onClick.AddListener(OnGameUpdate);
            BtnIgnore.onClick.AddListener(OnGameIgnore);
            BtnPackage.onClick.AddListener(OnGameInvoke);
        }

        public override void OnEnter(object data)
        {
            BtnUpdate.gameObject.SetActive(false);
            BtnIgnore.gameObject.SetActive(false);
            BtnPackage.gameObject.SetActive(false);

            switch (ButtonShowType)
            {
                case MessageShowType.OneButton:
                    BtnUpdate.gameObject.SetActive(true);
                    break;

                case MessageShowType.TwoButton:
                    BtnUpdate.gameObject.SetActive(true);
                    BtnIgnore.gameObject.SetActive(true);
                    break;

                case MessageShowType.ThreeButton:
                    BtnUpdate.gameObject.SetActive(true);
                    BtnIgnore.gameObject.SetActive(true);
                    BtnPackage.gameObject.SetActive(true);
                    break;
            }

            TextDesc.text = data?.ToString();
        }

        private void OnGameInvoke()
        {
            OnOk?.Invoke();
            OnClose();
        }

        private void OnGameIgnore()
        {
            OnCancel?.Invoke();
            OnClose();
        }

        private void OnGameUpdate()
        {
            OnOk?.Invoke();
            OnClose();
        }

        private void OnClose()
        {
            LauncherMgr.HideUI(UIDefine.LoadTipsUI);
        }
    }
}