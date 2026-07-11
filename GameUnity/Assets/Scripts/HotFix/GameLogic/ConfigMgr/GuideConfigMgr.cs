using System.Collections.Generic;
using GameProto;

namespace GameLogic
{
    /// <summary>
    /// 新手引导配置访问封装。
    /// </summary>
    public class GuideConfigMgr : Singleton<GuideConfigMgr>
    {
        /// <summary>
        /// 所有新手引导组配置。
        /// </summary>
        public List<GuideGroupConfig> GuideGroups => TbGuideGroupConfig.DataList;

        /// <summary>
        /// 所有新手引导步骤配置。
        /// </summary>
        public List<GuideStepConfig> GuideSteps => TbGuideStepConfig.DataList;

        /// <summary>
        /// 根据引导组ID获取引导组配置。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <returns>引导组配置；不存在时返回 null。</returns>
        public GuideGroupConfig GetGroupOrDefault(int groupId)
            => TbGuideGroupConfig.GetOrDefault(groupId);

        /// <summary>
        /// 根据引导ID获取步骤配置。
        /// </summary>
        /// <param name="guideId">引导ID。</param>
        /// <returns>步骤配置；不存在时返回 null。</returns>
        public GuideStepConfig GetStepOrDefault(int guideId)
            => TbGuideStepConfig.GetOrDefault(guideId);

        /// <summary>
        /// 获取指定引导组下的所有步骤配置。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <returns>步骤配置列表。</returns>
        public List<GuideStepConfig> GetStepsByGroupId(int groupId)
            => TbGuideStepConfig.GetListByGroupId(groupId);

        /// <summary>
        /// 根据引导组ID和组内步骤ID获取步骤配置。
        /// </summary>
        /// <param name="groupId">引导组ID。</param>
        /// <param name="stepId">组内步骤ID。</param>
        /// <returns>步骤配置；不存在时返回 null。</returns>
        public GuideStepConfig GetStepByGroupStepId(int groupId, int stepId)
        {
            var steps = GetStepsByGroupId(groupId);
            if (steps == null || steps.Count <= 0)
            {
                return null;
            }

            for (int i = 0; i < steps.Count; i++)
            {
                var stepCfg = steps[i];
                if (stepCfg != null && stepCfg.StepId == stepId)
                {
                    return stepCfg;
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定引导组的首步骤配置。
        /// </summary>
        /// <param name="groupCfg">引导组配置。</param>
        /// <returns>首步骤配置；不存在时返回 null。</returns>
        public GuideStepConfig GetFirstStep(GuideGroupConfig groupCfg)
        {
            if (groupCfg == null)
            {
                return null;
            }

            var steps = GetStepsByGroupId(groupCfg.GroupId);
            if (steps == null || steps.Count <= 0)
            {
                return null;
            }

            GuideStepConfig fallback = null;
            for (int i = 0; i < steps.Count; i++)
            {
                var stepCfg = steps[i];
                if (stepCfg == null)
                {
                    continue;
                }

                if (stepCfg.StepId == groupCfg.FirstStepId)
                {
                    return stepCfg;
                }

                if (fallback == null || stepCfg.StepId < fallback.StepId)
                {
                    fallback = stepCfg;
                }
            }

            return fallback;
        }

        /// <summary>
        /// 获取用于预览的步骤配置。
        /// </summary>
        /// <returns>预览步骤配置；不存在时返回 null。</returns>
        public GuideStepConfig GetPreviewStep()
        {
            var groupCfg = GetFirstGuideGroup(true) ?? GetFirstGuideGroup(false);
            if (groupCfg != null)
            {
                var stepCfg = GetFirstStep(groupCfg);
                if (stepCfg != null)
                {
                    return stepCfg;
                }
            }

            var steps = GuideSteps;
            return steps != null && steps.Count > 0 ? steps[0] : null;
        }

        private GuideGroupConfig GetFirstGuideGroup(bool onlyEnabled)
        {
            var groups = GuideGroups;
            if (groups == null || groups.Count <= 0)
            {
                return null;
            }

            for (int i = 0; i < groups.Count; i++)
            {
                var groupCfg = groups[i];
                if (groupCfg == null)
                {
                    continue;
                }

                if (!onlyEnabled || groupCfg.Enabled)
                {
                    return groupCfg;
                }
            }

            return null;
        }
    }
}
