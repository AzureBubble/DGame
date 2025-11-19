using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    /// <summary>
    /// 5 - 创建热更资源下载器
    /// </summary>
    public class CreateDownloaderProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private int m_curTryCnt;
        private const int MAX_TRY_COUNT = 3;
        private ResourceDownloaderOperation m_downloader;
        private int m_totalDownloadCnt;
        private float m_totalSizeMb;

        public override void OnEnter()
        {
            DLogger.Info("======== 5-创建热更资源下载器 ========");
            LauncherMgr.ShowUI(UIDefine.LoadUpdateUI, "创建补丁下载器...");
            CreateDownloader().Forget();
        }

        private async UniTaskVoid CreateDownloader()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            m_downloader = m_resourceModule.CreateResourceDownloader();

            if (m_downloader.TotalDownloadCount == 0)
            {
                DLogger.Info("======== 没有资源需要下载 ========");
                SwitchState<DownloadOverProcedure>();
            }
            else
            {
                DLogger.Info($"======== 总共找到了 {m_downloader.TotalDownloadCount} 个资源文件需要下载 ========");

                // 需要下载新文件 则挂起流程系统
                // 开发者需要在下载前检查磁盘空间
                m_totalDownloadCnt = m_downloader.TotalDownloadCount;
                long totalDownloadBytes = m_downloader.TotalDownloadBytes;
                float sizeMB = totalDownloadBytes / 1048576f;
                m_totalSizeMb = Mathf.Clamp(sizeMB, 0.1f, float.MaxValue);
                LauncherMgr.ShowMessageBox($"总共找到了 {m_totalDownloadCnt} 个补丁文件, 总大小为 {m_totalSizeMb}MB",
                    MessageShowType.TwoButton, StartDownloadFiles, Application.Quit);
            }
        }

        private void StartDownloadFiles()
        {
            SwitchState<DownloadFileProcedure>();
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
    }
}