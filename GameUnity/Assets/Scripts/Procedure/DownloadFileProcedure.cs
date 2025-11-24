using System;
using Cysharp.Threading.Tasks;
using DGame;
using Launcher;
using UnityEngine;
using YooAsset;

namespace Procedure
{
    public class DownloadFileProcedure : ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        private float m_lastUpdateDownloadSize;
        private float m_totalSpeed;
        private int m_speedSampleCnt;

        private float CurrentSpeed
        {
            get
            {
                float interval = Mathf.Max(Time.deltaTime, 0.01f);
                var sizeDiff = m_resourceModule.Downloader.CurrentDownloadBytes - m_lastUpdateDownloadSize;
                m_lastUpdateDownloadSize = m_resourceModule.Downloader.CurrentDownloadBytes;
                var speed = sizeDiff / interval;

                m_totalSpeed += speed;
                m_speedSampleCnt++;
                return m_totalSpeed / m_speedSampleCnt;
            }
        }

        public override void OnEnter()
        {
            DLogger.Info("======== 6-开始下载更新资源文件 ========");
            LauncherMgr.ShowUI<LoadUpdateUI>("开始下载更新文件...");
            BeginDownload().Forget();
        }

        private async UniTaskVoid BeginDownload()
        {
            var downloader = m_resourceModule.Downloader;
            downloader.DownloadErrorCallback = OnDownloadErrorCallback;
            downloader.DownloadUpdateCallback = OnDownloadUpdateCallback;
            downloader.BeginDownload();
            await downloader;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                return;
            }
            SwitchState<DownloadOverProcedure>();
        }

        private void OnDownloadUpdateCallback(DownloadUpdateData data)
        {
            string currentSizeMb = (data.CurrentDownloadBytes / 1048576f).ToString("f1");
            string totalSizeMb = (data.TotalDownloadBytes / 1048576f).ToString("f1");
            float progressPercentage = m_resourceModule.Downloader.Progress * 100;
            string speed = string.Empty;
            string line1 = Utility.StringUtil.Format("正在更新，已更新 {0}/{1} ({2:F2}%)", data.CurrentDownloadCount,
                data.TotalDownloadCount, progressPercentage);
            string line2 = Utility.StringUtil.Format("已更新大小 {0}MB/{1}MB", currentSizeMb, totalSizeMb);
            string line3 = Utility.StringUtil.Format("当前网速 {0}/s，剩余时间 {1}", speed,
                GetRemainingTime(data.TotalDownloadBytes, data.CurrentDownloadBytes,
                    CurrentSpeed));

            LauncherMgr.RefreshProgress(m_resourceModule.Downloader.Progress);
            LauncherMgr.ShowUI<LoadUpdateUI>($"{line1}\n{line2}\n{line3}");

            DLogger.Info($"{line1} {line2} {line3}");
        }

        private string GetRemainingTime(long totalDownloadBytes, long currentDownloadBytes, float speed)
        {
            int needTime = 0;

            if (speed > 0)
            {
                needTime = (int)((totalDownloadBytes - currentDownloadBytes) / speed);
            }
            TimeSpan ts = new TimeSpan(0, 0, needTime);
            return ts.ToString(@"hh\:mm\:ss");
        }

        private void OnDownloadErrorCallback(DownloadErrorData data)
        {
            LauncherMgr.ShowMessageBox($"下载补丁文件失败: {data.FileName}",
                SwitchState<CreateDownloaderProcedure>, Application.Quit);
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