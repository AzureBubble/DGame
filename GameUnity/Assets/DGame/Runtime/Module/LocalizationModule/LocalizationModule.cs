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

        }

        public Language CurrentLanguage { get; set; }
        public Language SystemLanguage { get; }
        public void Register()
        {
        }

        public UniTask LoadLanguageTotalAsset(string assetName)
        {
            return UniTask.CompletedTask;
        }

        public UniTask LoadLanguageTotalAsset()
        {
            return UniTask.CompletedTask;
        }

        public UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)
        {
            return UniTask.CompletedTask;
        }

        public bool CheckContainsLanguage(string language)
        {
            return false;
        }

        public bool SetLanguage(Language language, bool load = false)
        {
            return false;
        }

        public bool SetLanguage(string language, bool load = false)
        {
            return false;
        }

        public bool SetLanguage(int languageID)
        {
            return false;
        }
    }
}