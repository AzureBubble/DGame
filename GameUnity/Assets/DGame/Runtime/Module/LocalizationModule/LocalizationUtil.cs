using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public static class LocalizationUtil
    {
        private static readonly Dictionary<Language, string> m_languageMap = new Dictionary<Language, string>();
        private static readonly Dictionary<string, Language> m_languageStrMap = new Dictionary<string, Language>();

        private static void RegisterLanguageMap(Language language, string str = "")
        {
            if (string.IsNullOrEmpty(str))
            {
                str = language.ToString();
            }

            m_languageMap[language] = str;
            m_languageStrMap[str] = language;
        }

        public static string GetLanguage(Language language)
        {
            if (m_languageMap.TryGetValue(language, out var languageStr))
            {
                return languageStr;
            }

            language = Language.EN;
            return m_languageMap.TryGetValue(language, out languageStr) ? languageStr : language.ToString();
        }

        public static Language SystemLanguage
            => Application.systemLanguage switch
            {
                UnityEngine.SystemLanguage.ChineseSimplified => Language.CN,
                UnityEngine.SystemLanguage.ChineseTraditional => Language.GAT,
                UnityEngine.SystemLanguage.English => Language.EN,
                UnityEngine.SystemLanguage.Japanese => Language.JP,
                UnityEngine.SystemLanguage.Korean => Language.KR,
                UnityEngine.SystemLanguage.Indonesian => Language.INDO,
                UnityEngine.SystemLanguage.Vietnamese => Language.VN,
                _ => Language.EN
            };
    }
}