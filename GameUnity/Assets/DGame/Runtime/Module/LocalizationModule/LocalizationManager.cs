using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using I2.Loc;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DGame
{
    [DisallowMultipleComponent]
    public sealed class LocalizationManager : MonoBehaviour, IResourceManager_Bundles
    {
        private string m_defaultLanguage = "Chinese";

        [SerializeField] private TextAsset innerLocalizationCsv;

        [SerializeField]
        private List<string> allLanguages = new List<string>();

        private LanguageSource m_languageSource;

        private LanguageSourceData m_languageSourceData
            => m_languageSource == null
                ? (m_languageSource = gameObject.AddComponent<LanguageSource>()).SourceData
                : m_languageSource.SourceData;

        private IResourceModule m_resourceModule;

        /// <summary>
        /// 模拟平台运行时 编辑器资源不加载。
        /// </summary>
        [SerializeField] private bool m_useRuntimeModule = true;

        private string m_currentLanguage;

        public Language CurrentLanguage
        {
            get => LocalizationUtil.GetLanguage(m_currentLanguage);
            set => SetLanguage(LocalizationUtil.GetLanguage(value));
        }

        public Language SystemLanguage => LocalizationUtil.SystemLanguage;

        private void Awake()
        {
            m_resourceModule = ModuleSystem.GetModule<IResourceModule>();

            if (m_resourceModule == null)
            {
                DLogger.Fatal("ResourceModule无效的");
                return;
            }

            var localizationModule = ModuleSystem.GetModule<ILocalizationModule>();
            localizationModule?.Register(this);
        }

        private void Start()
        {
            RootModule rootModule = RootModule.Instance;

            if (rootModule == null)
            {
                DLogger.Fatal("RootModule 无效，请检查！");
                return;
            }

            m_defaultLanguage = LocalizationUtil.GetLanguage(
                rootModule.EditorLanguage != Language.Unspecified
                ? rootModule.EditorLanguage : SystemLanguage);
            AsyncInit().Forget();
        }

        private async UniTask<bool> AsyncInit()
        {
            if (string.IsNullOrEmpty(m_defaultLanguage))
            {
                DLogger.Fatal("必须设置默认语言");
                return false;
            }
#if UNITY_EDITOR
            if (!m_useRuntimeModule)
            {
                I2.Loc.LocalizationManager.RegisterSourceInEditor();
                UpdateAllLanguages();
                SetLanguage(m_defaultLanguage);
            }
            else
            {
                m_languageSourceData.Awake();
                await LoadLanguage(m_defaultLanguage, true, true);
            }
#else
            m_languageSourceData.Awake();
            await LoadLanguage(m_defaultLanguage, true, true);
#endif
            return true;
        }

        public async UniTask LoadLanguageTotalAsset(string assetName)
        {
#if UNITY_EDITOR
            if (!m_useRuntimeModule)
            {
                DLogger.Warning($"禁止在此模式下 动态加载语言");
                return;
            }
#endif
            TextAsset textAsset = await m_resourceModule.LoadAssetAsync<TextAsset>(assetName);

            if (textAsset == null)
            {
                DLogger.Warning("没有加载到语言总表");
                return;
            }
            DLogger.Info("加载语言总表成功");
            UseLocalizationCSV(textAsset.text, true);
        }

        public async UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false)
        {
#if UNITY_EDITOR
            if (!m_useRuntimeModule)
            {
                DLogger.Warning($"禁止在此模式下 动态加载语言");
                return;
            }
#endif
            TextAsset textAsset;

            if (!fromInit)
            {
                var assetName = GetLanguageAssetName(language);
                textAsset = await m_resourceModule.LoadAssetAsync<TextAsset>(assetName);
            }
            else
            {
                if (innerLocalizationCsv == null)
                {
                    DLogger.Warning("请使用I2Localization.asset导出CSV创建内置多语言");
                    return;
                }
                textAsset = innerLocalizationCsv;
            }

            if (textAsset == null)
            {
                DLogger.Warning($"没有加载到目标语言资源 {language}");
                return;
            }

            UseLocalizationCSV(textAsset.text, !setCurrent);

            if (setCurrent)
            {
                SetLanguage(language);
            }
        }

        private string GetLanguageAssetName(string language)
            => $"{LocalizationUtil.I2ResAssetNamePrefix}{language}";

        private void UpdateAllLanguages()
        {
            allLanguages.Clear();
            List<string> tempAllLanguages = I2.Loc.LocalizationManager.GetAllLanguages();

            for (int i = 0; i < tempAllLanguages.Count; i++)
            {
                var language = tempAllLanguages[i];
                var newLanguage = Regex.Replace(language, @"[\r\n]", "");
                allLanguages.Add(newLanguage);
            }
        }

        public bool CheckContainsLanguage(string language)
        {
            return allLanguages.Contains(language);
        }

        public bool SetLanguage(Language language, bool load = false)
        {
            return SetLanguage(LocalizationUtil.GetLanguage(language), load);
        }

        public bool SetLanguage(string language, bool load = false)
        {
            if (!CheckContainsLanguage(language))
            {
                if (load)
                {
                    LoadLanguage(language, true).Forget();
                    return true;
                }
                DLogger.Warning($"当前没有这个语言无法切换到此语言 {language}");
                return false;
            }

            if (m_currentLanguage == language)
            {
                return true;
            }

            DLogger.Info($"设置当前语言 = {language}");
            I2.Loc.LocalizationManager.CurrentLanguage = language;
            m_currentLanguage = language;
            return true;
        }

        public bool SetLanguage(int languageID)
        {
            if (languageID < 0 || languageID >= allLanguages.Count)
            {
                DLogger.Warning($"无效的语言ID, 无法找到{languageID} 语言数量: {allLanguages.Count}");
                return false;
            }

            var language = allLanguages[languageID];
            return SetLanguage(language);
        }

        public void UseLocalizationCSV(string text, bool isLocalizeAll = false)
        {
            m_languageSourceData.Import_CSV(string.Empty, text, eSpreadsheetUpdateMode.Merge, ',');

            if (isLocalizeAll)
            {
                I2.Loc.LocalizationManager.LocalizeAll();
            }
            UpdateAllLanguages();
        }

        /// <summary>
        /// 语言模块加载资源接口
        /// </summary>
        /// <param name="path">资源定位地址</param>
        /// <typeparam name="T">资源类型</typeparam>
        /// <returns>返回资源实例</returns>
        public T LoadFromBundle<T>(string path)where T : Object
        {
            var assetObj = m_resourceModule.LoadAssetSync<T>(path);

            if (assetObj != null)
            {
                return assetObj;
            }
            DLogger.Error($"本地化无法加载 {path} 资源类型: {typeof(T).Name}");
            return null;
        }
    }
}