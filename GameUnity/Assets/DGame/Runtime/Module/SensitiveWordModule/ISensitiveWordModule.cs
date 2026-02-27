using System.Collections.Generic;
using ToolGood.Words;

namespace DGame
{
    public interface ISensitiveWordModule
    {
        /// <summary>
        /// 是否使用高级过滤
        /// </summary>
        bool UseAdvancedFilter { get; set; }

        /// <summary>
        /// 设置是否全角/半角自动转换
        /// </summary>
        bool UseDBCcaseConverter { get; set; }

        /// <summary>
        /// 设置是否忽略大小写
        /// </summary>
        bool UseIgnoreCase { get; set; }

        /// <summary>
        /// 设置是否自动识别重复字符
        /// </summary>
        bool UseDuplicateWordFilter { get; set; }

        /// <summary>
        /// 设置是否开启跳词检测
        /// </summary>
        bool UseSkipWordFilter { get; set; }

        /// <summary>
        /// 敏感词替换字符
        /// </summary>
        char ReplaceChar { get; set; }

        /// <summary>
        /// 设置敏感词库
        /// </summary>
        /// <param name="keywords">敏感词库</param>
        void SetKeywords(List<string> keywords);

        /// <summary>
        /// 设置敏感词库和黑名单分级
        /// </summary>
        /// <param name="keywords">敏感词库</param>
        /// <param name="blacklistTypes">黑名单分级</param>
        void SetKeywords(List<string> keywords, List<int> blacklistTypes);

        /// <summary>
        /// 检查文本是否包含敏感词
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        bool ContainsSensitiveWord(string content);

        /// <summary>
        /// 替换敏感词为指定字符
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="replaceChar">替换的字符</param>
        /// <returns></returns>
        string ReplaceSensitiveWords(string content, char replaceChar = '\0');

        /// <summary>
        /// 设置跳词字符
        /// </summary>
        /// <param name="skipWords">跳词字符串</param>
        /// <returns></returns>
        void SetSkipWords(string skipWords);

        /// <summary>
        /// 黑名单分级查询第一个
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        IllegalWordsSearchResult FindFirst(string content);

        /// <summary>
        /// 黑名单分级查询所有
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        List<IllegalWordsSearchResult> FindAll(string content);

        /// <summary>
        /// 获取文本中所有的敏感词
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <returns></returns>
        List<string> FindAllSensitiveWords(string content);

        /// <summary>
        /// 获取文本中所有的敏感词
        /// </summary>
        /// <param name="content">文本内容</param>
        /// <param name="result">敏感词结果</param>
        /// <returns></returns>
        void FindAllSensitiveWords(string content, ref List<string> result);
    }
}