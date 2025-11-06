using UnityEngine;
#if UNITY_5_5_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace DGame
{
    public partial class DebuggerDriver
    {
        private class EnvironmentInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Environment Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Product Name", Application.productName, "游戏/应用名称");
                    DrawItem("Company Name", Application.companyName, "公司名称");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Game Identifier", Application.identifier, "应用标识符");
#else
                    DrawItem("Game Identifier", Application.bundleIdentifier, "应用标识符");
#endif
                    DrawItem("Application Version", Application.version, "应用版本号");
                    DrawItem("Unity Version", Application.unityVersion, "Unity引擎版本");
                    DrawItem("Platform", Application.platform.ToString(), "运行平台");
                    DrawItem("System Language", Application.systemLanguage.ToString(), "Unity云项目ID");
                    DrawItem("Cloud Project Id", Application.cloudProjectId, "运行平台");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Build Guid", Application.buildGUID, "构建唯一标识符");
#endif
                    DrawItem("Target Frame Rate", Application.targetFrameRate.ToString(), "目标帧率设置");
                    DrawItem("Internet Reachability", Application.internetReachability.ToString(), "网络可达性状态");
                    DrawItem("Background Loading Priority", Application.backgroundLoadingPriority.ToString(), " 后台加载优先级");
                    DrawItem("Is Playing", Application.isPlaying.ToString(), "应用是否正在运行");
#if UNITY_5_5_OR_NEWER
                    DrawItem("Splash Screen Is Finished", SplashScreen.isFinished.ToString(), "启动画面是否已结束");
#else
                    DrawItem("Is Showing Splash Screen", Application.isShowingSplashScreen.ToString(), "启动画面是否已结束");
#endif
                    DrawItem("Run In Background", Application.runInBackground.ToString(), "是否在后台运行");
#if UNITY_5_5_OR_NEWER
                    DrawItem("Install Name", Application.installerName, "安装程序名称");
#endif
                    DrawItem("Install Mode", Application.installMode.ToString(), "安装模式（应用商店、开发构建等）");
                    DrawItem("Sandbox Type", Application.sandboxType.ToString(), "沙盒类型（安全限制环境）");
                    DrawItem("Is Mobile Platform", Application.isMobilePlatform.ToString(), "是否为移动平台");
                    DrawItem("Is Console Platform", Application.isConsolePlatform.ToString(), "是否为游戏主机平台");
                    DrawItem("Is Editor", Application.isEditor.ToString(), "是否在编辑器中运行");
                    DrawItem("Is Debug Build", Debug.isDebugBuild.ToString(), "是否为调试版本");
#if UNITY_5_6_OR_NEWER
                    DrawItem("Is Focused", Application.isFocused.ToString(), "应用是否拥有焦点");
#endif
#if UNITY_2018_2_OR_NEWER
                    DrawItem("Is Batch Mode", Application.isBatchMode.ToString(), "是否为批处理模式（无图形界面）");
#endif
#if UNITY_5_3
                    DrawItem("Stack Trace Log Type", Application.stackTraceLogType.ToString(), "堆栈跟踪日志类型");
#endif
                }
                GUILayout.EndVertical();
            }
        }
    }
}