using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 3 - 初始化 Yooasset Package
    /// </summary>
    public class InitPackageProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private const string INIT_PACKAGE_ERROR_TIPS =
            "PackageManifest_DefaultPackage.version Error : HTTP/1.1 404 Not Found";
        private const string INIT_PACKAGE_ERROR_TRUE_TIPS =
            "请检查StreamingAssets/package/DefaultPackage/PackageManifest_DefaultPackage.version是否存在";

        public override void OnEnter()
        {
            Debugger.Info("======== 3-进入游戏初始化 Yooasset Package 流程 ========");
            InitPackage().Forget();
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public override void OnFixedUpdate()
        {
        }

        public override void OnExit()
        {
        }

        public override void OnDestroy()
        {
        }

        private async UniTaskVoid InitPackage()
        {
            Debugger.Info("======== InitPackage ========");
            try
            {
                var initOperation = await m_resourceModule.InitPackage(m_resourceModule.DefaultPackageName);

                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    var playMode = m_resourceModule.PlayMode;

                    switch (playMode)
                    {
                        case EPlayMode.EditorSimulateMode:
                            Debugger.Info("======== 当前处于编辑器资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;

                        case EPlayMode.OfflinePlayMode:
                            Debugger.Info("======== 当前处于单机资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;

                        case EPlayMode.HostPlayMode:
                        case EPlayMode.WebPlayMode:
                            LauncherMgr.ShowUI(UIDefine.LoadUpdateUI);
                            Debugger.Info("======== 当前处于联机/WebGL资源模式 ========");
                            SwitchState<InitResourceProcedure>();
                            break;
                        default:
                            Debugger.Info("======== 未知的资源模式 请检查 ResourcesModuleDriver 中的设置 ========");
                            break;
                    }
                }
                else
                {
                    LauncherMgr.ShowUI(UIDefine.LoadUpdateUI);
                    Debugger.Error($"======== InitPackage 失败 ========> {initOperation.Error}");
                    LauncherMgr.ShowUI(UIDefine.LoadUpdateUI, "初始化资源失败！");
                    LauncherMgr.ShowMessageBox($"资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {initOperation.Error}</color>",
                        MessageShowType.TwoButton, Retry, Application.Quit);
                }
            }
            catch (Exception e)
            {
                OnInitPackageFailed(e.Message);
                throw;
            }
        }

        private void OnInitPackageFailed(string message)
        {
            LauncherMgr.ShowUI(UIDefine.LoadUpdateUI);
            Debugger.Error($"======== OnInitPackageFailed ========> {message}");
            LauncherMgr.ShowUI(UIDefine.LoadUpdateUI, "资源初始化失败！");
            if (message.Contains(INIT_PACKAGE_ERROR_TIPS))
            {
                message = $"======== {INIT_PACKAGE_ERROR_TRUE_TIPS} ========";
            }

            LauncherMgr.ShowMessageBox($"资源初始化失败！点击确认重试 \n \n <color=#FF0000>原因: {message}</color>",
                MessageShowType.TwoButton, Retry, Application.Quit);
        }

        /// <summary>
        /// 重试初始化包
        /// </summary>
        private void Retry()
        {
            Debugger.Error($"======== 重新尝试 InitPackage ========");
            LauncherMgr.ShowUI(UIDefine.LoadUpdateUI, "重新初始化资源中...");
            InitPackage().Forget();
        }
    }
}