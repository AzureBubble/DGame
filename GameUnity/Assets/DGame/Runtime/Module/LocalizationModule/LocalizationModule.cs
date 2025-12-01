using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    public class LocalizationModule : Module, ILocalizationModule
    {
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            Object.Destroy(m_localizationManager.gameObject);
        }

        public Language CurrentLanguage { get => m_localizationManager.CurrentLanguage; set => m_localizationManager.CurrentLanguage = value; }
        public Language SystemLanguage => m_localizationManager.SystemLanguage;
        private LocalizationManager m_localizationManager;

        public void Register(LocalizationManager localizationManager)
        {
            m_localizationManager = localizationManager;
        }

        public async UniTask LoadLanguageTotalAsset(string assetName)
        {
            await m_localizationManager.LoadLanguageTotalAsset(assetName);
        }

        public async UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)
        {
            await m_localizationManager.LoadLanguage(language, setCurrent, fromInit);
        }

        public bool CheckContainsLanguage(string language)
        {
            return m_localizationManager.CheckContainsLanguage(language);
        }

        public bool SetLanguage(Language language, bool load = false)
        {
            return m_localizationManager.SetLanguage(language, load);
        }

        public bool SetLanguage(string language, bool load = false)
        {
            return m_localizationManager.SetLanguage(language, load);
        }

        public bool SetLanguage(int languageID)
        {
            return m_localizationManager.SetLanguage(languageID);
        }
    }
}