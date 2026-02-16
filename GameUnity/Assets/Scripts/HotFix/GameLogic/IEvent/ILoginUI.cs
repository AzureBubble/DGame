using DGame;
using Fantasy;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void OnRegister();

        void OnLogin();

        void ShowLoginUI();

        void GetAccountInfo(GameAccountInfo accountInfo);
    }
}