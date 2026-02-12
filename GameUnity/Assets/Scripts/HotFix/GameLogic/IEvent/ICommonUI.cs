using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ICommonUI
    {
        void ShowWaitingUI(uint waitFuncID, string tips, System.Action callback);
    }
}