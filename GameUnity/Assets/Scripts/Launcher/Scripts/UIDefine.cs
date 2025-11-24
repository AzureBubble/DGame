using System;
using UnityEngine;

namespace Launcher
{
    [Serializable]
    public class TextDefine
    {
        // InitPackageProcedure
        public string Init_Package_Error_Tips =
            "请检查StreamingAssets/package/DefaultPackage/PackageManifest_DefaultPackage.version是否存在";
        public string Init_Package_Failed_Tips = "资源初始化失败！";
        public string Init_Package_Failed_Try_Again_Tips = "资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {0}</color>";
        public string Init_Package_Retry_Tips = "重新初始化资源中...";

        // InitResourceProcedure
        public string Init_Resource_Tips = "初始化资源中...";
        public string Init_Resource_Update_Manifest_Tips = "更新清单文件...";
        public string Init_Resource_Error_Try_Again_Tips = "获取远程版本失败！点击确认重试\n <color=#FF0000>原因: {0}</color>";
        public string Init_Resource_Error_Try_Again_WebGL_Tips = "初始化资源失败！点击确认重试\n <color=#FF0000>原因: {0}</color>";
        public string Init_Resource_Network_Error_Try_Again_WebGL_Tips = "当前网络不可用，请检查本地网络设置后点击确认进行重试";
        public string Init_Resource_No_Find_Local_Record_Tips = "没有找到本地版本记录，需要更新资源！";
        public string Init_Resource_Have_Notice_No_Force_Update_Tips = "检测到可选资源更新，推荐完成更新提升游戏体验";
        public string Init_Resource_Have_Notice_No_Force_Update_Suggestion_Tips = "更新失败，检测到可选资源更新，推荐完成更新提升游戏体验！ \n \n 确定再试一次，取消进入游戏";

        // PreloadProcedure
        public string Preload_Loading_Tips = "正在载入...{0}%";
        public string Preload_Loading_Success_Tips = "载入完成";
    }

    public class UIDefine : TextDefine
    {
        private static UIDefine m_instance;
        public static UIDefine Instance => m_instance != null ? m_instance : m_instance = new UIDefine();

        public void InitConfigData(TextAsset config)
        {
            if (config == null)
            {
                return;
            }
            TextDefine textDefine = JsonUtility.FromJson<TextDefine>(config.text);
            if (textDefine == null)
            {
                return;
            }
        }
    }
}