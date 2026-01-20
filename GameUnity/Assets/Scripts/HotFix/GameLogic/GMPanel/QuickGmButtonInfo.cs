using System;
using GameProto;

namespace GameLogic
{
    public enum ConfigType
    {

    }

    public class QuickGmButtonInfo
    {
        public string GmName;
        public string GmText;
        public Action<string, GmConfig> GmCfgAction;
        public Action<string> Action;
        public GmConfig GmCfg;

        public QuickGmButtonInfo(string gmName, string gmText, Action<string> action, GmConfig gmCfg = null)
        {
            GmName = gmName;
            GmText = gmText;
            Action = action;
            GmCfg = gmCfg;
        }

        public QuickGmButtonInfo(string gmName, string gmText, Action<string, GmConfig> gmCfgAction, GmConfig gmCfg = null)
        {
            GmName = gmName;
            GmText = gmText;
            GmCfgAction = gmCfgAction;
            GmCfg = gmCfg;
        }

        public void Execute()
        {
            GmCfgAction?.Invoke(GmName, GmCfg);
            Action?.Invoke(GmName);
        }
    }
}