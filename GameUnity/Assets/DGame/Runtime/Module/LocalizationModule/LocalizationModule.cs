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
            throw new System.NotImplementedException();
        }

        public UniTask LoadLanguageTotalAsset(string assetName)
        {
            throw new System.NotImplementedException();
        }

        public UniTask LoadLanguageTotalAsset()
        {
            throw new System.NotImplementedException();
        }

        public UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)
        {
            throw new System.NotImplementedException();
        }

        public bool CheckContainsLanguage(string language)
        {
            throw new System.NotImplementedException();
        }

        public bool SetLanguage(Language language, bool load = false)
        {
            throw new System.NotImplementedException();
        }

        public bool SetLanguage(string language, bool load = false)
        {
            throw new System.NotImplementedException();
        }

        public bool SetLanguage(int languageID)
        {
            throw new System.NotImplementedException();
        }
    }
}