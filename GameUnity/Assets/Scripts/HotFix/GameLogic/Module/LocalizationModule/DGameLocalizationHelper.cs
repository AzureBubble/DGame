using DGame;
using GameProto;

namespace GameLogic
{
    public class DGameLocalizationHelper : ILocalizationHelper
    {
        public Language CurrentLanguage { get; set; }

        public Language SystemLanguage => LocalizationUtil.SystemLanguage;

        public bool ContainsLanguage(Language language) => (int)language < (int)LocalAreaType.MAX;

        public bool ContainsLanguage(int language) => language < (int)LocalAreaType.MAX;

        public bool SetLanguage(Language language)
        {
            if (ContainsLanguage(language) && language != CurrentLanguage)
            {
                CurrentLanguage = language;
                GameEvent.Get<ILocalization>().OnLanguageChanged((int)language);
                return true;
            }
            return false;
        }

        public bool SetLanguage(int language)
        {
            if (ContainsLanguage(language) && language != (int)CurrentLanguage)
            {
                CurrentLanguage = (Language)language;
                GameEvent.Get<ILocalization>().OnLanguageChanged(language);
                return true;
            }
            return false;
        }
    }
}