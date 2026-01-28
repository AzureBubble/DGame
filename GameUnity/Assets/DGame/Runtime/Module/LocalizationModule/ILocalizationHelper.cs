namespace DGame
{
    public interface ILocalizationHelper
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
        /// 检查是否存在语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool ContainsLanguage(Language language);

        /// <summary>
        /// 检查是否存在语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool ContainsLanguage(int language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool SetLanguage(Language language);

        /// <summary>
        /// 设置当前语言
        /// </summary>
        /// <param name="language">语言</param>
        /// <returns></returns>
        bool SetLanguage(int language);
    }
}