using System.Collections.Generic;
using DGame;

namespace Launcher
{
    public class UIDefine
    {
        public static readonly string LoadUpdateUI = "LoadUpdateUI";
        public static readonly string LoadTipsUI = "LoadTipsUI";

        public static void RegisterUI(Dictionary<string, string> list)
        {
            if (list == null)
            {
                DLogger.Error("======== UI窗口List为空 ========");
                return;
            }

            if (!list.ContainsKey(UIDefine.LoadUpdateUI))
            {
                list.Add(LoadUpdateUI, $"UIWindow/{LoadUpdateUI}");
            }

            if (!list.ContainsKey(UIDefine.LoadTipsUI))
            {
                list.Add(LoadTipsUI, $"UIWindow/{LoadTipsUI}");
            }
        }

    }
}