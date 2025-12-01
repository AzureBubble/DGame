using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    public interface ILocalizationModule
    {
        /// <summary>
        /// 获取或设置本地化语言
        /// </summary>
        Language CurrentLanguage { get; set; }

        /// <summary>
        /// 获取系统语言
        /// </summary>
        Language SystemLanguage { get; }

        /// <summary>
        /// 注册管理器
        /// </summary>
        /// <param name="localizationManager"></param>
        void Register(LocalizationManager localizationManager);

        /// <summary>
        /// 加载语言总表
        /// </summary>
        /// <param name="assetName"></param>
        /// <returns></returns>
        UniTask LoadLanguageTotalAsset(string assetName);

        /// <summary>
        /// 加载语言分表
        /// </summary>
        /// <param name="language">语言类型</param>
        /// <param name="setCurrent">是否立即设置成当前语言</param>
        /// <param name="fromInit">是否初始化Inner语言</param>
        /// <returns></returns>
        UniTask LoadLanguage(string language, bool setCurrent = false, bool fromInit = false);

        /// <summary>
        /// 检查是否存在改语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool CheckContainsLanguage(string language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="load">是否加载</param>
        /// <returns></returns>
        bool SetLanguage(Language language, bool load = false);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <param name="load">是否加载</param>
        /// <returns></returns>
        bool SetLanguage(string language, bool load = false);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="languageID">语言ID</param>
        /// <returns></returns>
        bool SetLanguage(int languageID);
    }
}