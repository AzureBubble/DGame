using DGame;

namespace GameLogic
{
    public partial class UIModule
    {
        public void ShowTipsUI(uint result)
        {
            string text = TextConfigMgr.Instance.GetText(result);
            ShowTipsUI(text);
        }

        public void ShowTipsUI(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                DLogger.Info(msg);
                ShowWindowAsync<TipsUI>(msg);
            }
        }

        public void ShowErrorTipsUI(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                DLogger.Error(msg);
                ShowWindowAsync<TipsUI>(msg);
            }
        }
    }
}