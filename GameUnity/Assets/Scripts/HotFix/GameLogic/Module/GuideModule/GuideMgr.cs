using System.Collections.Generic;
using DGame;
using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 新手引导管理器，负责引导组筛选、步骤推进和引导进度存档。
    /// </summary>
    public class GuideMgr : Singleton<GuideMgr>
    {
        private GuideGroupConfig m_currentGroupCfg;
        private GuideStepConfig m_currentStepCfg;
        private GuideSaveData m_saveData;

        /// <summary>
        /// 当前是否正在运行新手引导。
        /// </summary>
        public bool IsRunning => m_currentGroupCfg != null && m_currentStepCfg != null;

        /// <summary>
        /// 当前运行中的引导组ID；未运行时为0。
        /// </summary>
        public int CurrentGroupId => m_currentGroupCfg != null ? m_currentGroupCfg.GroupId : 0;

        /// <summary>
        /// 当前运行中的步骤ID；未运行时为0。
        /// </summary>
        public int CurrentStepId => m_currentStepCfg != null ? m_currentStepCfg.StepId : 0;

        /// <summary>
        /// 当前运行中的引导组配置。
        /// </summary>
        public GuideGroupConfig CurrentGroupConfig => m_currentGroupCfg;

        /// <summary>
        /// 当前运行中的引导步骤配置。
        /// </summary>
        public GuideStepConfig CurrentStepConfig => m_currentStepCfg;

        private GuideSaveData SaveData => m_saveData ??= GuideSaveData.Get;

        /// <summary>
        /// 从可运行的引导组中选择优先级最高的一组并启动。
        /// </summary>
        /// <returns>是否成功启动引导。</returns>
        public bool TryStartNextGuide()
        {
            if (IsRunning)
            {
                return false;
            }

            var groupCfg = GetNextRunnableGroup();
            if (groupCfg == null)
            {
                return false;
            }

            return TryStartGuide(groupCfg.GroupId);
        }

        /// <summary>
        /// 尝试启动指定引导组。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <returns>是否成功启动引导。</returns>
        public bool TryStartGuide(int groupId)
        {
            if (IsRunning)
            {
                DLogger.Warning($"[GuideMgr] Guide is already running. CurrentGroupId:{CurrentGroupId}, TargetGroupId:{groupId}");
                return false;
            }

            var groupCfg = GuideConfigMgr.Instance.GetGroupOrDefault(groupId);
            if (!CanRunGroup(groupCfg))
            {
                return false;
            }

            var stepCfg = GetStartStep(groupCfg);
            if (stepCfg == null)
            {
                return false;
            }

            EnterStep(groupCfg, stepCfg);
            return true;
        }

        /// <summary>
        /// 完成当前步骤，并进入下一步骤；若没有下一步骤则完成当前引导组。
        /// </summary>
        public void CompleteCurrentStep()
        {
            if (!IsRunning)
            {
                return;
            }

            if (!TryGetNextStep(out var nextStepCfg, out var isEnd))
            {
                if (isEnd)
                {
                    CompleteCurrentGuide();
                    return;
                }

                DLogger.Warning(
                    $"[GuideMgr] Next step not found. GroupId:{m_currentGroupCfg.GroupId}, StepId:{m_currentStepCfg.StepId}, NextStepId:{m_currentStepCfg.NextStepId}");
                HandleCurrentStepFailure();
                return;
            }

            EnterStep(m_currentGroupCfg, nextStepCfg);
        }

        private bool TryGetNextStep(out GuideStepConfig nextStepCfg, out bool isEnd)
        {
            nextStepCfg = null;
            isEnd = false;

            if (!IsRunning)
            {
                return false;
            }

            if (m_currentStepCfg.NextStepId <= 0)
            {
                isEnd = true;
                return false;
            }

            nextStepCfg = GuideConfigMgr.Instance.GetStepByGroupStepId(
                m_currentGroupCfg.GroupId,
                m_currentStepCfg.NextStepId);
            return nextStepCfg != null;
        }

        /// <summary>
        /// 完成当前引导组，写入完成状态并关闭引导界面。
        /// </summary>
        public void CompleteCurrentGuide()
        {
            if (m_currentGroupCfg == null)
            {
                return;
            }

            SaveCompleted(m_currentGroupCfg);
            ClearCurrentGuide();
            GameModule.UIModule.CloseWindow<GuideUI>();
        }

        /// <summary>
        /// 中止当前引导组，清理进度并关闭引导界面。
        /// </summary>
        public void AbortCurrentGuide()
        {
            if (m_currentGroupCfg != null)
            {
                ClearProgress(m_currentGroupCfg);
            }

            ClearCurrentGuide();
            GameModule.UIModule.CloseWindow<GuideUI>();
        }

        /// <summary>
        /// 按当前步骤的跳过范围跳过引导。
        /// </summary>
        public void SkipCurrentGuide()
        {
            if (!IsRunning)
            {
                return;
            }

            switch (m_currentStepCfg.SkipScope)
            {
                case EGuideSkipScope.Group:
                    CompleteCurrentGuide();
                    break;
                case EGuideSkipScope.Guide:
                default:
                    SkipCurrentStep();
                    break;
            }
        }

        /// <summary>
        /// 按当前引导组的失败策略处理当前步骤失败。
        /// </summary>
        public void HandleCurrentStepFailure()
        {
            if (!IsRunning)
            {
                return;
            }

            switch (m_currentGroupCfg.FailurePolicy)
            {
                case EGuideFailurePolicy.Retry:
                    ShowCurrentStep();
                    break;
                case EGuideFailurePolicy.SkipStep:
                    SkipCurrentStep();
                    break;
                case EGuideFailurePolicy.AbortGuide:
                    AbortCurrentGuide();
                    break;
            }
        }

        private void SkipCurrentStep()
        {
            if (!IsRunning)
            {
                return;
            }

            if (!TryGetNextStep(out var nextStepCfg, out var isEnd))
            {
                if (isEnd)
                {
                    CompleteCurrentGuide();
                    return;
                }

                DLogger.Warning(
                    $"[GuideMgr] Skip step failed, next step not found. GroupId:{m_currentGroupCfg.GroupId}, StepId:{m_currentStepCfg.StepId}, NextStepId:{m_currentStepCfg.NextStepId}");
                AbortCurrentGuide();
                return;
            }

            EnterStep(m_currentGroupCfg, nextStepCfg);
        }

        /// <summary>
        /// 检查指定引导组是否已完成。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <returns>是否已完成。</returns>
        public bool IsGuideCompleted(int groupId)
        {
            var groupCfg = GuideConfigMgr.Instance.GetGroupOrDefault(groupId);
            return groupCfg != null && IsGuideCompleted(groupCfg);
        }

        private GuideGroupConfig GetNextRunnableGroup()
        {
            var groups = GuideConfigMgr.Instance.GuideGroups;
            if (groups == null || groups.Count <= 0)
            {
                return null;
            }

            GuideGroupConfig result = null;
            for (int i = 0; i < groups.Count; i++)
            {
                var groupCfg = groups[i];
                if (!CanRunGroup(groupCfg) || GetStartStep(groupCfg) == null)
                {
                    continue;
                }

                if (result == null ||
                    groupCfg.Priority > result.Priority ||
                    groupCfg.Priority == result.Priority && groupCfg.GroupId < result.GroupId)
                {
                    result = groupCfg;
                }
            }

            return result;
        }

        private bool CanRunGroup(GuideGroupConfig groupCfg)
        {
            if (groupCfg == null || !groupCfg.Enabled)
            {
                return false;
            }

            if (IsGuideCompleted(groupCfg))
            {
                return false;
            }

            if (!CheckPrerequisites(groupCfg))
            {
                return false;
            }

            return CheckCondition(groupCfg);
        }

        private bool CheckPrerequisites(GuideGroupConfig groupCfg)
        {
            var prerequisiteGuideIds = groupCfg.PrerequisiteGuideIds;
            if (prerequisiteGuideIds == null || prerequisiteGuideIds.Count <= 0)
            {
                return true;
            }

            for (int i = 0; i < prerequisiteGuideIds.Count; i++)
            {
                if (!IsGuideCompleted(prerequisiteGuideIds[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CheckCondition(GuideGroupConfig groupCfg)
        {
            if (groupCfg.ConditionKey <= 0)
            {
                return true;
            }

            // TODO: 接入新手引导开启条件系统，按 ConditionKey + ConditionParams 判断。
            return true;
        }

        private GuideStepConfig GetStartStep(GuideGroupConfig groupCfg)
        {
            if (groupCfg == null)
            {
                return null;
            }

            if (TryGetProgress(groupCfg, out var progress))
            {
                switch (groupCfg.ResumePolicy)
                {
                    case EGuideResumePolicy.ResumeStep:
                        var resumeStep = GuideConfigMgr.Instance.GetStepByGroupStepId(groupCfg.GroupId, progress.StepId);
                        return resumeStep ?? GuideConfigMgr.Instance.GetFirstStep(groupCfg);
                    case EGuideResumePolicy.DoNotResume:
                        ClearProgress(groupCfg);
                        return null;
                    case EGuideResumePolicy.RestartGuide:
                    default:
                        break;
                }
            }

            return GuideConfigMgr.Instance.GetFirstStep(groupCfg);
        }

        private void EnterStep(GuideGroupConfig groupCfg, GuideStepConfig stepCfg)
        {
            m_currentGroupCfg = groupCfg;
            m_currentStepCfg = stepCfg;
            SaveProgress(groupCfg, stepCfg);
            ShowCurrentStep();
        }

        private void ShowCurrentStep()
        {
            if (m_currentStepCfg == null)
            {
                return;
            }

            GameModule.UIModule.ShowWindowAsync<GuideUI>(m_currentStepCfg);
        }

        private void ClearCurrentGuide()
        {
            m_currentGroupCfg = null;
            m_currentStepCfg = null;
        }

        private bool IsGuideCompleted(GuideGroupConfig groupCfg)
        {
            if (groupCfg.LocalSave)
            {
                return SaveData.IsGroupCompleted(groupCfg.GroupId);
            }

            // TODO: 接入服务器新手引导完成状态。
            return false;
        }

        private bool TryGetProgress(GuideGroupConfig groupCfg, out GuideGroupProgressData progress)
        {
            progress = null;
            if (groupCfg.LocalSave)
            {
                return SaveData.TryGetProgress(groupCfg.GroupId, out progress);
            }

            // TODO: 接入服务器新手引导进度。
            return false;
        }

        private void SaveProgress(GuideGroupConfig groupCfg, GuideStepConfig stepCfg)
        {
            if (groupCfg.LocalSave)
            {
                SaveData.SetProgress(groupCfg.GroupId, stepCfg.StepId);
                SaveData.Save();
                return;
            }

            // TODO: 上报服务器新手引导进度。
        }

        private void SaveCompleted(GuideGroupConfig groupCfg)
        {
            if (groupCfg.LocalSave)
            {
                SaveData.MarkGroupCompleted(groupCfg.GroupId);
                SaveData.ClearProgress(groupCfg.GroupId);
                SaveData.Save();
                return;
            }

            // TODO: 上报服务器新手引导完成状态。
        }

        private void ClearProgress(GuideGroupConfig groupCfg)
        {
            if (groupCfg.LocalSave)
            {
                SaveData.ClearProgress(groupCfg.GroupId);
                SaveData.Save();
                return;
            }

            // TODO: 清理服务器新手引导进度。
        }

        protected override void OnDestroy()
        {
            ClearCurrentGuide();
            m_saveData = null;
        }
    }
}
