using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    /// <summary>
    /// 默认本地化辅助器
    /// </summary>
    public class LocalizationUtil
    {
#if UNITY_EDITOR
        public const string I2GlobalSourcesEditorPath = "Assets/Editor/I2Localization/I2Languages.asset";
#endif

        public const string I2ResAssetNamePrefix = "I2_";

        public static Language SystemLanguage
            => Application.systemLanguage switch
            {
                UnityEngine.SystemLanguage.Afrikaans => Language.Afrikaans,
                UnityEngine.SystemLanguage.Arabic => Language.Arabic,
                UnityEngine.SystemLanguage.Basque => Language.Basque,
                UnityEngine.SystemLanguage.Belarusian => Language.Belarusian,
                UnityEngine.SystemLanguage.Bulgarian => Language.Bulgarian,
                UnityEngine.SystemLanguage.Catalan => Language.Catalan,
                UnityEngine.SystemLanguage.Chinese => Language.ChineseSimplified,
                UnityEngine.SystemLanguage.ChineseSimplified => Language.ChineseSimplified,
                UnityEngine.SystemLanguage.ChineseTraditional => Language.ChineseTraditional,
                UnityEngine.SystemLanguage.Czech => Language.Czech,
                UnityEngine.SystemLanguage.Danish => Language.Danish,
                UnityEngine.SystemLanguage.Dutch => Language.Dutch,
                UnityEngine.SystemLanguage.English => Language.English,
                UnityEngine.SystemLanguage.Estonian => Language.Estonian,
                UnityEngine.SystemLanguage.Faroese => Language.Faroese,
                UnityEngine.SystemLanguage.Finnish => Language.Finnish,
                UnityEngine.SystemLanguage.French => Language.French,
                UnityEngine.SystemLanguage.German => Language.German,
                UnityEngine.SystemLanguage.Greek => Language.Greek,
                UnityEngine.SystemLanguage.Hebrew => Language.Hebrew,
                UnityEngine.SystemLanguage.Hungarian => Language.Hungarian,
                UnityEngine.SystemLanguage.Icelandic => Language.Icelandic,
                UnityEngine.SystemLanguage.Indonesian => Language.Indonesian,
                UnityEngine.SystemLanguage.Italian => Language.Italian,
                UnityEngine.SystemLanguage.Japanese => Language.Japanese,
                UnityEngine.SystemLanguage.Korean => Language.Korean,
                UnityEngine.SystemLanguage.Latvian => Language.Latvian,
                UnityEngine.SystemLanguage.Lithuanian => Language.Lithuanian,
                UnityEngine.SystemLanguage.Norwegian => Language.Norwegian,
                UnityEngine.SystemLanguage.Polish => Language.Polish,
                UnityEngine.SystemLanguage.Portuguese => Language.PortuguesePortugal,
                UnityEngine.SystemLanguage.Romanian => Language.Romanian,
                UnityEngine.SystemLanguage.Russian => Language.Russian,
                UnityEngine.SystemLanguage.SerboCroatian => Language.SerboCroatian,
                UnityEngine.SystemLanguage.Slovak => Language.Slovak,
                UnityEngine.SystemLanguage.Slovenian => Language.Slovenian,
                UnityEngine.SystemLanguage.Spanish => Language.Spanish,
                UnityEngine.SystemLanguage.Swedish => Language.Swedish,
                UnityEngine.SystemLanguage.Thai => Language.Thai,
                UnityEngine.SystemLanguage.Turkish => Language.Turkish,
                UnityEngine.SystemLanguage.Ukrainian => Language.Ukrainian,
                UnityEngine.SystemLanguage.Unknown => Language.Unspecified,
                UnityEngine.SystemLanguage.Vietnamese => Language.Vietnamese,
                _ => Language.Unspecified
            };

        private static readonly Dictionary<Language, string> m_languageMap = new Dictionary<Language, string>();
        private static readonly Dictionary<string, Language> m_languageStrMap = new Dictionary<string, Language>();

        static LocalizationUtil()
        {
            RegisterLanguage(Language.English);
            RegisterLanguage(Language.ChineseSimplified, "Chinese (Simplified)");
            RegisterLanguage(Language.ChineseTraditional, "Chinese (Traditional)");
            RegisterLanguage(Language.Japanese);
            RegisterLanguage(Language.Korean);
        }

        private static void RegisterLanguage(Language language, string str = "")
        {
            if (string.IsNullOrEmpty(str))
            {
                str = language.ToString();
            }
            m_languageMap[language] = str;
            m_languageStrMap[str] = language;
        }

        public static Language GetLanguage(string language)
            => string.IsNullOrEmpty(language) ? Language.Unspecified : m_languageStrMap.GetValueOrDefault(language, Language.English);

        public static string GetLanguage(Language language)
            => m_languageMap.GetValueOrDefault(language, nameof(Language.English));
    }
}