using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
        [InspectorName("强制更新(不更新无法进入游戏)")]
        Force = 0,

        /// <summary>
        /// 非强制(不更新可以进入游戏)
        /// </summary>
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
        [InspectorName("更新存在提示")]
        Notice = 0,

        /// <summary>
        /// 更新非提示
        /// </summary>
        [InspectorName("更新非提示")]
        NoNotice = 1,
    }

    [CreateAssetMenu(fileName = "UpdateSettings", menuName = "DGame/UpdateSettings")]
    public class UpdateSettings : ScriptableObject
    {
        [SerializeField]
        private string projectName = "Demo";

        public bool Enable
        {
             get
             {
#if ENABLE_HYBRIDCLR
                return true;
#else
                return false;
#endif
             }
        }

        [Header("自动同步 [HybridCLRGlobalSettings]")]
        public List<string> HotUpdateAssemblies = new List<string>() { "GameProto.dll", "GameLogic.dll" };
        public List<string> AOTMetaAssemblies = new List<string>() { "mscorlib.dll", "System.dll", "System.Core.dll", "DGame.Runtime.dll" ,"UniTask.dll", "YooAsset.dll" };

        public string LogicMainDllName = "GameLogic.dll";

        public string AssemblyTextAssetExtension = ".bytes";

        public string AssemblyTextAssetPath = "ABAssets/DLL";

        [Header("更新设置")]
        public UpdateStyle UpdateStyle = UpdateStyle.Force;

        public UpdateNotice UpdateNotice = UpdateNotice.Notice;

        [SerializeField]
        private string m_resDownloadPath = "https://127.0.0.1:8081";

        [SerializeField]
        private string m_fallbackResDownloadPath = "https://127.0.0.1:8082";

        [SerializeField]
        private LoadResWayWebGL m_loadResWayWebGL = LoadResWayWebGL.Remote;

        public LoadResWayWebGL GetLoadResWayWebGL() => m_loadResWayWebGL;

        [SerializeField]
        private bool m_isAutoAssetCopyToBuildAddress = false;

        public bool IsAutoAssetCopyToBuildAddress() => m_isAutoAssetCopyToBuildAddress;

        [SerializeField]
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