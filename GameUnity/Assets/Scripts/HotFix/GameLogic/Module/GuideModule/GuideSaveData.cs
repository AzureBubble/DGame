using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>
    /// 引导组本地进度数据。
    /// </summary>
    public sealed class GuideGroupProgressData
    {
        /// <summary>
        /// 引导组ID。
        /// </summary>
        public int GroupId;

        /// <summary>
        /// 当前步骤ID。
        /// </summary>
        public int StepId;
    }
    
    /// <summary>
    /// 新手引导本地存档数据。
    /// </summary>
    [ClientSaveData("GuideSaveData", true)]
    public sealed class GuideSaveData : BaseClientSaveData
    {
        /// <summary>
        /// 已完成的引导组ID列表。
        /// </summary>
        public List<int> CompletedGroupIds { get; private set; } = new List<int>();

        /// <summary>
        /// 未完成引导组的当前进度列表。
        /// </summary>
        public List<GuideGroupProgressData> ProgressList { get; private set; } = new List<GuideGroupProgressData>();

        /// <summary>
        /// 获取新手引导本地存档实例。
        /// </summary>
        public static GuideSaveData Get => BaseClientSaveData.Get<GuideSaveData>();

        /// <summary>
        /// 检查指定引导组是否已完成。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <returns>是否已完成。</returns>
        public bool IsGroupCompleted(int groupId) => CompletedGroupIds.Contains(groupId);

        /// <summary>
        /// 标记指定引导组为已完成。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        public void MarkGroupCompleted(int groupId)
        {
            if (!CompletedGroupIds.Contains(groupId))
            {
                CompletedGroupIds.Add(groupId);
            }
        }

        /// <summary>
        /// 尝试获取指定引导组的当前进度。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <param name="progress">引导组进度数据。</param>
        /// <returns>是否存在进度。</returns>
        public bool TryGetProgress(int groupId, out GuideGroupProgressData progress)
        {
            progress = GetProgress(groupId);
            return progress != null;
        }

        /// <summary>
        /// 设置指定引导组的当前步骤进度。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <param name="stepId">步骤ID。</param>
        public void SetProgress(int groupId, int stepId)
        {
            var progress = GetProgress(groupId);
            if (progress == null)
            {
                progress = new GuideGroupProgressData
                {
                    GroupId = groupId,
                    StepId = stepId,
                };
                ProgressList.Add(progress);
                return;
            }

            progress.StepId = stepId;
        }

        /// <summary>
        /// 清理指定引导组的当前进度。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        public void ClearProgress(int groupId)
        {
            for (int i = ProgressList.Count - 1; i >= 0; i--)
            {
                if (ProgressList[i].GroupId == groupId)
                {
                    ProgressList.RemoveAt(i);
                }
            }
        }

        private GuideGroupProgressData GetProgress(int groupId)
        {
            for (int i = 0; i < ProgressList.Count; i++)
            {
                var progress = ProgressList[i];
                if (progress != null && progress.GroupId == groupId)
                {
                    return progress;
                }
            }

            return null;
        }
    }
}