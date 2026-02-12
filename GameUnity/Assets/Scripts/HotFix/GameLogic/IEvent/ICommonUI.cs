using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ICommonUI
    {
        void ShowWaitingUI(uint waitFuncID, string tips = "", System.Action callback = null);

        void FinishWaiting(uint waitFuncID = 0);
    }
}