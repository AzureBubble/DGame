using DGame;
using GameProto;

namespace GameLogic
{
    public class TextConfigMgr : Singleton<TextConfigMgr>
    {
        private LocalizationType m_curLocalizationType;

        private int m_curLanguage => (int)m_curLocalizationType;

        public LocalizationType CurLocalizationType
        {
            get => m_curLocalizationType;
            set
            {
                if (m_curLocalizationType == value)
                {
                    return;
                }
                m_curLocalizationType = value;
            }
        }

        public TextConfig GetTextConfig(int id) => ConfigSystem.Instance.Tables.TbTextConfig.GetOrDefault(id);

        public TextConfig GetTextConfig(TextDefine id) => GetTextConfig((int)id);

        public string GetText(int id, params object[] args)
        {
            var textConfig = GetTextConfig(id);
            if (textConfig == null)
            {
                return $"TextID: {id}";
            }
            string content = textConfig.Content[m_curLanguage];

            if ((textConfig.ArgNum > 0 && args == null) || textConfig.ArgNum != args.Length)
            {
                DLogger.Error("invalid string arg num, strId[{0}] config num[{1}] input num[{2}]", id, textConfig.ArgNum,
                    args != null ? args.Length : -1);
                return content;
            }

            return string.Format(content, args);
        }

        public string GetText(uint id, params object[] args) => GetText((int)id, args);

        public string GetText(TextDefine id, params object[] args) => GetText((int)id, args);
    }
}