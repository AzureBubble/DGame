using DGame;

namespace GameLogic
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILocalization
    {
        void OnLanguageChanged(int language);
    }
}