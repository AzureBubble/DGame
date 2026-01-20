using DGame;

namespace GameLogic
{
    public partial class UIModule
    {
        public void ShowTipsUI(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
            {
                DLogger.Info(msg);
                ShowWindowAsync<TipsUI>(msg);
            }
        }
    }
}