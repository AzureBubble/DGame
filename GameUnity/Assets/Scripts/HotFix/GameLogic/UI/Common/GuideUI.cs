using System;
using DGame;
using GameProto;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameLogic
{
    /// <summary>
    /// 新手引导界面，负责遮罩、提示文本、跳过按钮和引导手指表现。
    /// </summary>
    public partial class GuideUI
    {
        #region 字段

        private const float MASK_SHRINK_START_SIZE = 1000f;
        private const float MASK_SHRINK_TIME = 0.3f;
        private const float MIN_MASK_RADIUS = 8f;
        private const float MAX_MASK_RADIUS = 40f;
        private const float MASK_RADIUS_RATIO = 0.1f;
        private const float MASK_DONE_THRESHOLD = 0.1f;
        private const float ARROW_MOVE_TIME = 0.6f;

        private Material m_guideMaskMaterial;
        private bool m_tweenMask;
        private bool m_tweenArrow;
        private bool m_loopMoveArrow;
        private bool m_waitLoopArrow;
        private Vector2 m_targetMaskPos;
        private Vector2 m_currentMaskSize;
        private Vector2 m_targetMaskSize;
        private Vector2 m_arrowStartPos;
        private Vector2 m_arrowTargetPos;
        private float m_maskWidthVelocity;
        private float m_maskHeightVelocity;
        private float m_arrowMoveTimer;
        private float m_arrowLoopDelayTimer;
        private float m_arrowLoopDelayTime;
        private Vector2 m_defaultTipsSize;
        private UIImage m_uiImgGuideMask;
        private GuideClickListener m_guideClickListener;
        private EmptyGraph m_emptyGraphNextStep;

        protected override UILayer windowLayer => UILayer.Top;

        protected override ModelType GetModelType() => ModelType.NoneType;

        #endregion

        #region Override

        protected override void BindMemberProperty()
        {
            InitGuideMaskMaterial();
            InitGuideMaskImage();
            InitEmptyGraphNextStep();
            InitTipsDefaultSize();
            HideUnusedGuideNodes();
        }

        protected override void OnRefresh()
        {
            var stepCfg = ResolveGuideStep();
            if (stepCfg == null)
            {
                DLogger.Warning("[GuideUI] No GuideStepConfig found for mask preview.");
                ResetGuideMask(false);
                ResetClickableArea();
                HideTips();
                ApplySkippable(false);
                HideArrow();
                return;
            }

            PlayMask(stepCfg);
        }

        protected override void OnUpdate()
        {
            if (m_tweenMask && m_guideMaskMaterial != null)
            {
                m_currentMaskSize.x = Mathf.SmoothDamp(
                    m_currentMaskSize.x,
                    m_targetMaskSize.x,
                    ref m_maskWidthVelocity,
                    MASK_SHRINK_TIME);
                m_currentMaskSize.y = Mathf.SmoothDamp(
                    m_currentMaskSize.y,
                    m_targetMaskSize.y,
                    ref m_maskHeightVelocity,
                    MASK_SHRINK_TIME);

                if ((m_currentMaskSize - m_targetMaskSize).sqrMagnitude <= MASK_DONE_THRESHOLD * MASK_DONE_THRESHOLD)
                {
                    m_currentMaskSize = m_targetMaskSize;
                    m_tweenMask = false;
                }

                ApplyMaskOrigin(m_currentMaskSize);
            }

            UpdateArrowMove();
        }

        protected override void OnDestroy()
        {
            ResetGuideMask(false);
            ResetClickableArea();
            ResetStepClickListener();
            HideArrow();
        }

        #endregion

        #region 函数

        private void InitGuideMaskMaterial()
        {
            if (m_imgGuideMask == null || m_imgGuideMask.material == null)
            {
                return;
            }

            m_guideMaskMaterial = m_imgGuideMask.material;
        }

        private void InitGuideMaskImage()
        {
            if (m_imgGuideMask == null)
            {
                return;
            }

            m_uiImgGuideMask = m_imgGuideMask as UIImage;
            if (m_uiImgGuideMask == null)
            {
                m_imgGuideMask.TryGetComponent(out m_uiImgGuideMask);
            }
        }
        
        private void InitEmptyGraphNextStep()
        {
            if (m_btnNextStep == null)
            {
                return;
            }
            m_btnNextStep.TryGetComponent(out m_emptyGraphNextStep);
        }

        private void HideUnusedGuideNodes()
        {
            m_rectTips.SetActive(false);
            m_rectArrow.SetActive(false);
            m_btnSkip.SetActive(false);
            m_btnNextStep.SetActive(false);
        }

        private GuideStepConfig ResolveGuideStep()
        {
            if (UserData is GuideStepConfig stepCfg)
            {
                return stepCfg;
            }

            if (UserData is GuideGroupConfig groupCfg)
            {
                return GuideConfigMgr.Instance.GetFirstStep(groupCfg);
            }

            if (UserData is int configId)
            {
                var byGuideId = GuideConfigMgr.Instance.GetStepOrDefault(configId);
                if (byGuideId != null)
                {
                    return byGuideId;
                }

                var byGroupId = GuideConfigMgr.Instance.GetGroupOrDefault(configId);
                if (byGroupId != null)
                {
                    return GuideConfigMgr.Instance.GetFirstStep(byGroupId);
                }
            }

            return GuideConfigMgr.Instance.GetPreviewStep();
        }

        /// <summary>
        /// 使用指定步骤配置刷新引导表现。
        /// </summary>
        /// <param name="stepCfg">新手引导步骤配置。</param>
        public void Init(GuideStepConfig stepCfg)
        {
            PlayMask(stepCfg);
        }

        private void PlayMask(GuideStepConfig stepCfg)
        {
            if (stepCfg == null)
            {
                ResetGuideMask(false);
                ResetClickableArea();
                ResetStepClickListener();
                HideTips();
                ApplySkippable(false);
                HideArrow();
                return;
            }

            ApplySkippable(stepCfg.Skippable);
            ApplyTips(stepCfg);

            if (m_imgGuideMask == null)
            {
                return;
            }

            m_imgGuideMask.gameObject.SetActive(true);
            var clickTarget = ApplyClickableArea(stepCfg);
            ApplyStepClickListener(stepCfg, clickTarget);

            if (m_guideMaskMaterial == null)
            {
                return;
            }

            m_targetMaskPos = new Vector2(stepCfg.MaskPos.X, stepCfg.MaskPos.Y);
            m_targetMaskSize = GetTargetMaskSize(stepCfg);
            ApplyArrow(stepCfg);

            if (stepCfg.MaskShape == EGuideMaskShape.None)
            {
                ResetGuideMask(true);
                return;
            }

            m_currentMaskSize = stepCfg.NeedTween
                ? GetStartMaskSize(stepCfg.MaskShape)
                : m_targetMaskSize;
            m_maskWidthVelocity = 0f;
            m_maskHeightVelocity = 0f;
            m_tweenMask = stepCfg.NeedTween;

            m_guideMaskMaterial.SetFloat("_MaskType", GetShaderMaskType(stepCfg.MaskShape, m_targetMaskSize));
            m_guideMaskMaterial.SetFloat("_Raid", GetMaskRadius(stepCfg, m_targetMaskSize));
            ApplyMaskOrigin(m_currentMaskSize);
        }

        private void InitTipsDefaultSize()
        {
            if (m_rectTips != null)
            {
                m_defaultTipsSize = m_rectTips.sizeDelta;
            }
        }

        private Vector2 GetTargetMaskSize(GuideStepConfig stepCfg)
        {
            var padding = Mathf.Max(0f, stepCfg.MaskPadding);
            var width = Mathf.Max(0f, stepCfg.MaskSize.Width);
            var height = Mathf.Max(0f, stepCfg.MaskSize.Height);

            if (stepCfg.MaskShape == EGuideMaskShape.Circle)
            {
                var radius = Mathf.Max(width, height) * 0.5f + padding;
                return new Vector2(radius, 0f);
            }

            return new Vector2(width + padding * 2f, height + padding * 2f);
        }

        private static Vector2 GetStartMaskSize(EGuideMaskShape shape)
        {
            return shape == EGuideMaskShape.Circle
                ? new Vector2(MASK_SHRINK_START_SIZE, 0f)
                : Vector2.one * MASK_SHRINK_START_SIZE;
        }

        private static float GetShaderMaskType(EGuideMaskShape shape, Vector2 targetSize)
        {
            switch (shape)
            {
                case EGuideMaskShape.Circle:
                    return 0f;
                case EGuideMaskShape.Rectangle:
                    return 1f;
                case EGuideMaskShape.RoundedRectangle:
                    return targetSize.x >= targetSize.y ? 2f : 3f;
                default:
                    return -1f;
            }
        }

        private static float GetMaskRadius(GuideStepConfig stepCfg, Vector2 targetSize)
        {
            if (stepCfg.MaskShape != EGuideMaskShape.RoundedRectangle)
            {
                return 0f;
            }

            if (stepCfg.MaskRadius > 0f)
            {
                return stepCfg.MaskRadius;
            }

            var minSize = Mathf.Min(targetSize.x, targetSize.y);
            if (minSize <= 0f)
            {
                return MIN_MASK_RADIUS;
            }

            return Mathf.Clamp(minSize * MASK_RADIUS_RATIO, MIN_MASK_RADIUS, MAX_MASK_RADIUS);
        }

        private void ApplyMaskOrigin(Vector2 maskSize)
        {
            if (m_guideMaskMaterial == null)
            {
                return;
            }

            m_guideMaskMaterial.SetVector(
                "_Origin",
                new Vector4(m_targetMaskPos.x, m_targetMaskPos.y, maskSize.x, maskSize.y));
        }

        private void ApplyTips(GuideStepConfig stepCfg)
        {
            if (m_rectTips == null || m_textTips == null)
            {
                return;
            }

            if (stepCfg.TipTextId <= 0)
            {
                m_rectTips.SetActive(false);
                return;
            }

            m_textTips.text = TextConfigMgr.Instance.GetText(stepCfg.TipTextId);
            m_rectTips.sizeDelta = GetTipsSize(stepCfg);
            m_rectTips.anchoredPosition = GetTipsPosition(stepCfg);
            m_rectTips.gameObject.SetActive(true);
        }

        private Vector2 GetTipsSize(GuideStepConfig stepCfg)
        {
            var width = Mathf.Max(0f, stepCfg.TipsSize.Width);
            var height = Mathf.Max(0f, stepCfg.TipsSize.Height);

            if (width <= 0f && height <= 0f)
            {
                return m_defaultTipsSize;
            }

            if (width <= 0f)
            {
                width = m_defaultTipsSize.x;
            }

            if (height <= 0f)
            {
                height = m_defaultTipsSize.y;
            }

            return new Vector2(width, height);
        }

        private Vector2 GetTipsPosition(GuideStepConfig stepCfg)
        {
            var offset = new Vector2(stepCfg.TipsPos.X, stepCfg.TipsPos.Y);
            switch (stepCfg.TipPlacement)
            {
                case EGuideTipPlacement.ScreenCenter:
                    return offset;
                case EGuideTipPlacement.AboveTarget:
                    return GetTargetRelativeTipsPosition(stepCfg, true) + offset;
                case EGuideTipPlacement.BelowTarget:
                    return GetTargetRelativeTipsPosition(stepCfg, false) + offset;
                default:
                    return offset;
            }
        }

        private Vector2 GetTargetRelativeTipsPosition(GuideStepConfig stepCfg, bool above)
        {
            var targetPos = new Vector2(stepCfg.MaskPos.X, stepCfg.MaskPos.Y);
            var tipsSize = GetTipsSize(stepCfg);
            var targetSize = GetTargetMaskSize(stepCfg);
            var targetHalfHeight = stepCfg.MaskShape == EGuideMaskShape.Circle
                ? targetSize.x
                : targetSize.y * 0.5f;
            var direction = above ? 1f : -1f;

            return targetPos + Vector2.up * direction * (targetHalfHeight + tipsSize.y * 0.5f);
        }

        private void HideTips()
        {
            m_rectTips.SetActive(false);
        }

        private RectTransform ApplyClickableArea(GuideStepConfig stepCfg)
        {
            if (m_imgGuideMask == null)
            {
                return null;
            }

            var target = GetClickableTarget(stepCfg);
            if (m_uiImgGuideMask != null)
            {
                m_imgGuideMask.raycastTarget = true;
                m_uiImgGuideMask.SetTarget(target);
                return target;
            }

            m_imgGuideMask.raycastTarget = target == null;
            return target;
        }

        private void ApplyStepClickListener(GuideStepConfig stepCfg, RectTransform clickTarget)
        {
            ResetStepClickListener();

            switch (stepCfg.StepType)
            {
                case EGuideStepType.Explain:
                case EGuideStepType.ClickAnywhere:
                    m_btnNextStep.SetActive(true);
                    break;
                case EGuideStepType.ClickTarget:
                    BindTargetClickListener(clickTarget);
                    break;
            }
        }

        private void BindTargetClickListener(RectTransform clickTarget)
        {
            if (clickTarget == null)
            {
                return;
            }

            m_guideClickListener = clickTarget.GetComponent<GuideClickListener>();
            if (m_guideClickListener == null)
            {
                m_guideClickListener = clickTarget.gameObject.AddComponent<GuideClickListener>();
            }

            m_guideClickListener.Bind(OnGuideTargetClicked);
        }

        private void OnGuideTargetClicked()
        {
            ResetStepClickListener();
            GameModule.GuideModule.CompleteCurrentStep();
        }

        private void ResetStepClickListener()
        {
            if (m_btnNextStep != null)
            {
                m_btnNextStep.SetActive(false);
            }

            if (m_guideClickListener == null)
            {
                return;
            }

            m_guideClickListener.Clear();
            UnityEngine.Object.Destroy(m_guideClickListener);
            m_guideClickListener = null;
        }

        private RectTransform GetClickableTarget(GuideStepConfig stepCfg)
        {
            switch (stepCfg.StepType)
            {
                case EGuideStepType.Explain:
                case EGuideStepType.ClickAnywhere:
                    return m_imgGuideMask.rectTransform;
                case EGuideStepType.ClickTarget:
                    return ResolveClickTarget(stepCfg);
                default:
                    return null;
            }
        }

        private RectTransform ResolveClickTarget(GuideStepConfig stepCfg)
        {
            if (!TryGetClickTargetParams(stepCfg, out var windowName, out var componentName))
            {
                DLogger.Warning($"[GuideUI] ClickTarget StepParam invalid. GuideId:{stepCfg.GuideId}");
                return null;
            }

            if (!GameModule.UIModule.TryGetWindow(windowName, out var window))
            {
                DLogger.Warning($"[GuideUI] ClickTarget window not found. GuideId:{stepCfg.GuideId}, Window:{windowName}");
                return null;
            }

            var targetTrans = DGame.Utility.UnityUtil.FindChildByName(window.transform, componentName);
            if (targetTrans == null)
            {
                DLogger.Warning($"[GuideUI] ClickTarget component not found. GuideId:{stepCfg.GuideId}, Window:{windowName}, Component:{componentName}");
                return null;
            }

            return targetTrans as RectTransform;
        }

        private static bool TryGetClickTargetParams(GuideStepConfig stepCfg, out string windowName, out string componentName)
        {
            windowName = null;
            componentName = null;

            if (stepCfg.StepParam == null)
            {
                return false;
            }

            var stepParam = stepCfg.StepParam.Value;
            windowName = stepParam.StringParam1?.Trim();
            componentName = stepParam.StringParam2?.Trim();
            return !string.IsNullOrEmpty(windowName) && !string.IsNullOrEmpty(componentName);
        }

        private void ResetClickableArea()
        {
            if (m_imgGuideMask == null)
            {
                return;
            }

            m_imgGuideMask.raycastTarget = true;
            m_uiImgGuideMask?.SetTarget(null);
        }

        private void ApplyArrow(GuideStepConfig stepCfg)
        {
            if (m_rectArrow == null)
            {
                return;
            }

            if (stepCfg.ArrowMoveType == EGuideArrowMoveType.None)
            {
                HideArrow();
                return;
            }

            m_arrowStartPos = GetArrowStartPosition(stepCfg);
            m_arrowTargetPos = GetArrowTargetPosition(stepCfg);
            m_arrowMoveTimer = 0f;
            m_arrowLoopDelayTimer = 0f;
            m_arrowLoopDelayTime = Mathf.Max(0f, stepCfg.LoopDelayTime);
            m_loopMoveArrow = stepCfg.NeedTween && stepCfg.LoopMoveArrow;
            m_tweenArrow = stepCfg.NeedTween;
            m_waitLoopArrow = false;

            m_rectArrow.SetActive(true);
            m_rectArrow.anchoredPosition = stepCfg.NeedTween ? m_arrowStartPos : m_arrowTargetPos;
        }

        private Vector2 GetArrowStartPosition(GuideStepConfig stepCfg)
        {
            switch (stepCfg.ArrowMoveType)
            {
                case EGuideArrowMoveType.Target:
                    return Vector2.zero;
                case EGuideArrowMoveType.CustomMove:
                    return ToVector2(stepCfg.ArrowStartPos);
                default:
                    return Vector2.zero;
            }
        }

        private Vector2 GetArrowTargetPosition(GuideStepConfig stepCfg)
        {
            switch (stepCfg.ArrowMoveType)
            {
                case EGuideArrowMoveType.Target:
                    return m_targetMaskPos + ToVector2(stepCfg.ArrowTargetPos);
                case EGuideArrowMoveType.CustomMove:
                    return ToVector2(stepCfg.ArrowTargetPos);
                default:
                    return Vector2.zero;
            }
        }

        private void UpdateArrowMove()
        {
            if (!m_tweenArrow || m_rectArrow == null)
            {
                return;
            }

            if (m_waitLoopArrow)
            {
                m_arrowLoopDelayTimer += Time.deltaTime;
                if (m_arrowLoopDelayTimer < m_arrowLoopDelayTime)
                {
                    return;
                }

                m_waitLoopArrow = false;
                m_arrowMoveTimer = 0f;
                m_arrowLoopDelayTimer = 0f;
                m_rectArrow.anchoredPosition = m_arrowStartPos;
            }

            m_arrowMoveTimer += Time.deltaTime;
            var progress = Mathf.Clamp01(m_arrowMoveTimer / ARROW_MOVE_TIME);
            m_rectArrow.anchoredPosition = Vector2.Lerp(m_arrowStartPos, m_arrowTargetPos, progress);

            if (progress < 1f)
            {
                return;
            }

            if (m_loopMoveArrow)
            {
                m_waitLoopArrow = true;
                m_arrowLoopDelayTimer = 0f;
                m_rectArrow.anchoredPosition = m_arrowTargetPos;
                return;
            }

            m_tweenArrow = false;
            m_rectArrow.anchoredPosition = m_arrowTargetPos;
        }

        private void HideArrow()
        {
            m_tweenArrow = false;
            m_loopMoveArrow = false;
            m_waitLoopArrow = false;
            m_arrowLoopDelayTimer = 0f;

            if (m_rectArrow != null)
            {
                m_rectArrow.SetActive(false);
            }
        }

        private void ApplySkippable(bool skippable)
        {
            m_btnSkip.SetActive(skippable);
        }

        private void ResetGuideMask(bool keepSolidMask)
        {
            m_tweenMask = false;

            m_imgGuideMask.SetActive(keepSolidMask);

            if (m_guideMaskMaterial == null)
            {
                return;
            }

            m_guideMaskMaterial.SetFloat("_MaskType", -1f);
            m_guideMaskMaterial.SetVector("_Origin", Vector4.zero);
            m_guideMaskMaterial.SetVector("_TopOri", Vector4.zero);
            m_guideMaskMaterial.SetFloat("_Raid", 0f);
        }

        private static Vector2 ToVector2(Pos pos) => new Vector2(pos.X, pos.Y);

        #endregion

        #region 事件

        private partial void OnClickSkipBtn()
        {
            GameModule.GuideModule.SkipCurrentGuide();
        }

        private partial void OnClickNextStepBtn()
        {
            GameModule.GuideModule.CompleteCurrentStep();
        }

        #endregion
    }
}
