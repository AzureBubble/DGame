using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ICommonUI
    {
        void ShowWaitingUI(uint waitFuncID, uint textID, System.Action callback);
    }
}