using System;
using UnityEngine.UI;

namespace Launcher
{
    public class LoadUpdateUI : UIBase
    {
        public Button BtnClear;
        public Scrollbar SliderProgress;
        public Text TextDesc;
        public Text TextAppID;
        public Text TextResID;

        private void Start()
        {
            BtnClear.onClick.AddListener(OnClear);
            BtnClear.gameObject.SetActive(true);
            OnUpdateUIProgress(0f);
        }

        public override void OnEnter(object param)
        {
            if (param == null)
            {
                return;
            }
            base.OnEnter(param);
            TextDesc.text = param.ToString();
        }

        internal void OnUpdateUIProgress(float progress)
        {
            SliderProgress.gameObject.SetActive(true);
            SliderProgress.size = progress;
        }

        public void OnClear()
        {

        }
    }
}