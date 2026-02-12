using System;
using Cysharp.Threading.Tasks;
using DGame;

namespace GameLogic
{
    public class CommonUIController : IUIController
    {
        public void RegUIMessage()
        {
            GameEvent.AddEventListener<uint, string, System.Action>(ICommonUI_Event.ShowWaitingUI, OnShowWaitingUI);
            GameEvent.AddEventListener<uint>(ICommonUI_Event.FinishWaiting, OnFinishWaiting);
        }

        #region FinishWaitingUI

        private void OnFinishWaiting(uint waitFuncID)
        {
            UIModule.Instance.GetWindowAsync<WaitingUI>(ui =>
            {
                if (ui != null && ui.FinishWaiting(waitFuncID))
                {
                    ui.Close();
                }
            });
        }

        #endregion

        #region ShowWaitingUI

        private void OnShowWaitingUI(uint waitFuncID, string tips, System.Action callback)
        {
            OnShowWaitingUIAsync(waitFuncID, tips, callback).Forget();
        }

        private async UniTaskVoid OnShowWaitingUIAsync(uint waitFuncID, string tips, System.Action callback)
        {
            var ui = await UIModule.Instance.ShowWindowAsyncAwait<WaitingUI>();
            ui?.Init(waitFuncID, tips, callback);
        }

        #endregion
    }
}