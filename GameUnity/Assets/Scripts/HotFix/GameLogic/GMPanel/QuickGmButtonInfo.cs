using System;
using GameProto;

namespace GameLogic
{
    public enum ConfigType
    {

    }

    public class GmCacheConfig
    {
        /// <summary>
        /// GMID
        /// </summary>
        public int GmID;

        /// <summary>
        /// Gm类型（1:客户端，2:服务器）
        /// </summary>
        public int GmType;

        /// <summary>
        /// 描述（按钮名）
        /// </summary>
        public string GmDesc;

        /// <summary>
        /// GM命令
        /// </summary>
        public string GmOrder;

        /// <summary>
        /// 命令ID
        /// </summary>
        public int OrderID;

        /// <summary>
        /// 数量
        /// </summary>
        public int Num;
        /// <summary>
        /// 是否直接执行
        /// </summary>
        public int ExecuteDirectly;

        /// <summary>
        /// 是否执行后关闭
        /// </summary>
        public int ExecuteClose;

        /// <summary>
        /// 关联配置
        /// </summary>
        public int AssConfig;

        public GmCacheConfig() { }

        public GmCacheConfig(GmConfig gmConfig)
        {
            GmID = gmConfig.GmID;
            GmType = gmConfig.GmType;
            GmDesc = gmConfig.GmDesc;
            GmOrder = gmConfig.GmOrder;
            OrderID = gmConfig.OrderID;
            Num = gmConfig.Num;
            ExecuteDirectly = gmConfig.ExecuteDirectly;
            ExecuteClose = gmConfig.ExecuteClose;
            AssConfig = gmConfig.AssConfig;
        }
    }

    public class SaveGm
    {
        public GmCacheConfig saveCfg = new GmCacheConfig();
        public int BatchNum;
    }

    public class QuickGmButtonInfo
    {
        public string GmName;
        public string GmText;
        public Action<string, GmConfig> GmCfgAction;
        public Action<QuickGmButtonInfo> Action;
        public GmConfig GmCfg;

        public QuickGmButtonInfo(string gmName, string gmText, Action<QuickGmButtonInfo> action, GmConfig gmCfg = null)
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
            Action?.Invoke(this);
        }
    }
}