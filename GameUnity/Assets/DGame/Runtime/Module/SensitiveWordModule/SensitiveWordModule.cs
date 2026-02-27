using System.Collections.Generic;
using ToolGood.Words;

#pragma warning disable CS0618  // 禁用过时警告

namespace DGame
{
    public class SensitiveWordModule : Module, ISensitiveWordModule
    {
        private IllegalWordsSearch m_illegalWordsSearch;
        private WordsSearch m_wordsSearch;

        private bool m_useAdvancedFilter = true;
        private bool m_useDBCcaseConverter = true;
        private bool m_useIgnoreCase = true;
        private bool m_useDuplicateWordFilter = true;
        private bool m_useSkipWordFilter = true;

        public bool UseAdvancedFilter
        {
            get => m_useAdvancedFilter;
            set
            {
                if (m_useAdvancedFilter != value)
                {
                    m_useAdvancedFilter = value;

                    if (m_useAdvancedFilter)
                    {
                        m_illegalWordsSearch = new IllegalWordsSearch();
                        m_wordsSearch = null;
                    }
                    else
                    {
                        m_illegalWordsSearch = null;
                        m_wordsSearch = new WordsSearch();
                    }
                }
            }
        }

        public bool UseDBCcaseConverter
        {
            get => m_useDBCcaseConverter;
            set
            {
                if (m_useDBCcaseConverter != value)
                {
                    m_useDBCcaseConverter = value;

                    if (m_illegalWordsSearch != null)
                    {
                        m_illegalWordsSearch.UseDBCcaseConverter = m_useDBCcaseConverter;
                    }
                }
            }
        }

        public bool UseIgnoreCase
        {
            get => m_useIgnoreCase;
            set
            {
                if (m_useIgnoreCase != value)
                {
                    m_useIgnoreCase = value;

                    if (m_illegalWordsSearch != null)
                    {
                        m_illegalWordsSearch.UseIgnoreCase = m_useIgnoreCase;
                    }
                }
            }
        }

        public bool UseDuplicateWordFilter
        {
            get => m_useDuplicateWordFilter;
            set
            {
                if (m_useDuplicateWordFilter != value)
                {
                    m_useDuplicateWordFilter = value;

                    if (m_illegalWordsSearch != null)
                    {
                        m_illegalWordsSearch.UseDuplicateWordFilter = m_useDuplicateWordFilter;
                    }
                }
            }
        }

        public bool UseSkipWordFilter
        {
            get => m_useSkipWordFilter;
            set
            {
                if (m_useSkipWordFilter != value)
                {
                    m_useSkipWordFilter = value;

                    if (m_illegalWordsSearch != null)
                    {
                        m_illegalWordsSearch.UseSkipWordFilter = m_useSkipWordFilter;
                    }
                }
            }
        }

        public char ReplaceChar { get; set; } = '*';

        public override void OnCreate()
        {
            if (m_useAdvancedFilter)
            {
                m_illegalWordsSearch = new IllegalWordsSearch();
                m_illegalWordsSearch.UseIgnoreCase = m_useIgnoreCase;
                m_illegalWordsSearch.UseDBCcaseConverter = m_useDBCcaseConverter;
                m_illegalWordsSearch.UseDuplicateWordFilter = m_useDuplicateWordFilter;
            }
            else
            {
                m_wordsSearch = new WordsSearch();
            }
        }

        public override void OnDestroy()
        {
            m_illegalWordsSearch = null;
            m_wordsSearch = null;
        }

        public void SetKeywords(List<string> keywords)
        {
            if (m_useAdvancedFilter)
            {
                if (m_illegalWordsSearch == null)
                {
                    m_illegalWordsSearch = new IllegalWordsSearch();
                }

                m_illegalWordsSearch.SetKeywords(keywords);
            }
            else
            {
                if (m_wordsSearch == null)
                {
                    m_wordsSearch = new WordsSearch();
                }

                m_wordsSearch.SetKeywords(keywords);
            }
        }

        public void SetKeywords(List<string> keywords, List<int> blacklistTypes)
        {
            if (m_useAdvancedFilter)
            {
                if (m_illegalWordsSearch == null)
                {
                    m_illegalWordsSearch = new IllegalWordsSearch();
                }

                m_illegalWordsSearch.SetKeywords(keywords);
                m_illegalWordsSearch.SetBlacklist(blacklistTypes.ToArray());
            }
        }

        public bool ContainsSensitiveWord(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            if (m_useAdvancedFilter)
            {
                return m_illegalWordsSearch != null && m_illegalWordsSearch.ContainsAny(content);
            }
            else
            {
                return m_wordsSearch != null && m_wordsSearch.ContainsAny(content);
            }
        }

        public string ReplaceSensitiveWords(string content, char replaceChar = '\0')
        {
            if (string.IsNullOrEmpty(content))
            {
                return content;
            }

            if (replaceChar == '\0')
            {
                replaceChar = ReplaceChar;
            }

            if (m_useAdvancedFilter)
            {
                return m_illegalWordsSearch == null ? content : m_illegalWordsSearch.Replace(content, replaceChar);
            }
            else
            {
                return m_wordsSearch == null ? content : m_wordsSearch.Replace(content, replaceChar);
            }
        }

        public void SetSkipWords(string skipWords)
        {
            if (string.IsNullOrEmpty(skipWords))
            {
                return;
            }
            if (m_useAdvancedFilter)
            {
                if (m_illegalWordsSearch == null)
                {
                    m_illegalWordsSearch = new IllegalWordsSearch();
                }

                m_illegalWordsSearch.SetSkipWords(skipWords);
            }
        }

        public IllegalWordsSearchResult FindFirst(string content)
            => m_useAdvancedFilter && m_illegalWordsSearch != null ? m_illegalWordsSearch.FindFirst(content) : null;

        public List<IllegalWordsSearchResult> FindAll(string content)
            => m_useAdvancedFilter && m_illegalWordsSearch != null ? m_illegalWordsSearch.FindAll(content) : null;

        public List<string> FindAllSensitiveWords(string content)
        {
            List<string> result = new List<string>();

            if (m_useAdvancedFilter)
            {
                if (m_illegalWordsSearch == null)
                {
                    m_illegalWordsSearch = new IllegalWordsSearch();
                }

                var matches = m_illegalWordsSearch.FindAll(content);

                foreach (var match in matches)
                {
                    result.Add(match.Keyword);
                }
            }
            else
            {
                if (m_wordsSearch == null)
                {
                    m_wordsSearch = new WordsSearch();
                }

                var matches = m_wordsSearch.FindAll(content);

                foreach (var match in matches)
                {
                    result.Add(match.Keyword);
                }
            }

            return result;
        }

        public void FindAllSensitiveWords(string content, ref List<string> result)
        {
            if (result == null)
            {
                result = new List<string>();
            }

            if (m_useAdvancedFilter)
            {
                if (m_illegalWordsSearch == null)
                {
                    m_illegalWordsSearch = new IllegalWordsSearch();
                }

                var matches = m_illegalWordsSearch.FindAll(content);

                foreach (var match in matches)
                {
                    result.Add(match.Keyword);
                }
            }
            else
            {
                if (m_wordsSearch == null)
                {
                    m_wordsSearch = new WordsSearch();
                }

                var matches = m_wordsSearch.FindAll(content);

                foreach (var match in matches)
                {
                    result.Add(match.Keyword);
                }
            }
        }
    }
}