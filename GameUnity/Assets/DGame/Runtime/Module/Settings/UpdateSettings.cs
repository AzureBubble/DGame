using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR

using Sirenix.OdinInspector;

#endif

namespace DGame
{
    /// <summary>
    /// 强制更新类型
    /// </summary>
    public enum UpdateStyle
    {
        /// <summary>
        /// 强制更新(不更新无法进入游戏)
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("强制更新(不更新无法进入游戏)")]
#endif
        [InspectorName("强制更新(不更新无法进入游戏)")]
        Force = 0,

        /// <summary>
        /// 非强制(不更新可以进入游戏)
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("非强制(不更新可以进入游戏)")]
#endif
        [InspectorName("非强制(不更新可以进入游戏)")]
        Optional = 1,
    }

    /// <summary>
    /// 是否有提示更新
    /// </summary>
    public enum UpdateNotice
    {
        /// <summary>
        /// 更新存在提示
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("更新存在提示")]
#endif
        [InspectorName("更新存在提示")]
        Notice = 0,

        /// <summary>
        /// 更新非提示
        /// </summary>
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("更新非提示")]
#endif
        [InspectorName("更新非提示")]
        NoNotice = 1,
    }

    [CreateAssetMenu(fileName = "UpdateSettings", menuName = "DGame/UpdateSettings")]
    public class UpdateSettings : ScriptableObject
    {
        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("项目名称")]
#endif
        private string projectName = "Demo";

#if UNITY_EDITOR
#pragma warning disable 0162
#endif
        public bool Enable
        {
             get
             {
#if ENABLE_HYBRIDCLR
                return true;
#endif
                return false;
             }
        }
#if UNITY_EDITOR
#pragma warning disable 0162
#endif

        [Header("自动同步 [HybridCLRGlobalSettings]")]
        public List<string> HotUpdateAssemblies = new List<string>() { "GameProto.dll", "GameLogic.dll" };
        public List<string> AOTMetaAssemblies = new List<string>() { "mscorlib.dll", "System.dll", "System.Core.dll", "DGame.Runtime.dll" ,"UniTask.dll", "YooAsset.dll" };

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("主业务逻辑DLL")]
#endif
        public string LogicMainDllName = "GameLogic.dll";

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("DLL文本资产打包后缀名")]
#endif
        public string AssemblyTextAssetExtension = ".bytes";

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("DLL文本资产路径")]
#endif
        public string AssemblyTextAssetPath = "ABAssets/DLL";

        [Header("更新设置")]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("强制更新类型")]
#endif
        public UpdateStyle UpdateStyle = UpdateStyle.Force;

#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("是否有更新提示")]
#endif
        public UpdateNotice UpdateNotice = UpdateNotice.Notice;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("资源服务器地址")]
#endif
        private string m_resDownloadPath = "https://127.0.0.1:8081";

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("资源服务器备用地址")]
#endif
        private string m_fallbackResDownloadPath = "https://127.0.0.1:8082";

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("WebGL平台加载资源方式")]
#endif
        private LoadResWayWebGL m_loadResWayWebGL = LoadResWayWebGL.Remote;

        public LoadResWayWebGL GetLoadResWayWebGL() => m_loadResWayWebGL;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("自动Copy资源到StreamingAssets"), ToggleLeft]
#endif
        private bool m_isAutoAssetCopyToBuildAddress = false;

        public bool IsAutoAssetCopyToBuildAddress() => m_isAutoAssetCopyToBuildAddress;

        [SerializeField]
#if ODIN_INSPECTOR && UNITY_EDITOR && ENABLE_ODIN_INSPECTOR
        [LabelText("打包程序资源地址")]
#endif
        private string m_buildAddress = "../../Builds/Unity_Data/StreamingAssets";

        public string GetBuildAddress() => m_buildAddress;

        public string GetResDownloadPath() => Path.Combine(m_resDownloadPath, projectName, GetPlatformName()).Replace("\\", "/");

        public string GetFallbackResDownloadPath() => Path.Combine(m_fallbackResDownloadPath, projectName, GetPlatformName()).Replace("\\", "/");

        /// <summary>
        /// 获取当前平台名称
        /// </summary>
        /// <returns></returns>
        public static string GetPlatformName()
        {
#if UNITY_ANDROID
            return "Android";
#elif UNITY_IOS
            return "IOS";
#elif UNITY_WEBGL
            return "WebGL";
#else
            switch (Application.platform)
            {
                    case RuntimePlatform.WindowsEditor:
                            return "Windows64";
                    case RuntimePlatform.WindowsPlayer:
                            return "Windows64";
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                            return "MacOS";
                    case RuntimePlatform.IPhonePlayer:
                            return "IOS";
                    case RuntimePlatform.Android:
                            return "Android";
                    case RuntimePlatform.WebGLPlayer:
                            return "WebGL";
                    case RuntimePlatform.PS5:
                            return "PS5";
                    default:
                            throw new NotSupportedException($"没有支持平台: '{Application.platform.ToString()}'");
            }
#endif
        }
    }
}