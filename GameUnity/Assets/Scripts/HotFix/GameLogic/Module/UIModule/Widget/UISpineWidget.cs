#if SPINE_UNITY && SPINE_CSHARP

using System;
using System.Collections.Generic;
using DGame;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

namespace GameLogic
{
    public class UISpineWidget : UIWidget
    {
        #region 脚本工具生成的代码

        private Transform m_tfUISpineRoot;
        private GameObject m_goSpineModel;
        private Transform m_tfEffRoot;

        protected override void ScriptGenerator()
        {
            m_tfUISpineRoot = FindChild("m_tfUISpineRoot");
            m_goSpineModel = FindChild("m_tfUISpineRoot/m_goSpineModel").gameObject;
            m_tfEffRoot = FindChild("m_tfEffRoot");
        }

        #endregion

        #region Override

        protected override void BindMemberProperty()
        {
            m_skeletonGraphic = m_goSpineModel.GetComponent<SkeletonGraphic>();
            m_curAnimState = m_skeletonGraphic?.AnimationState;

            if (m_curAnimState == null)
            {
                DLogger.Error(
                    $"skeletonGraphic.AnimationState为空，请检查是否调用时SkeletonGraphic是否Awake初始化.[{gameObject.name}]");
            }
        }

        protected override void OnDestroy()
        {
            CancelTimer();
            m_clickAction = null;
            m_skeletonGraphic = null;
            m_curAnimState = null;
        }

        #endregion

        #region 字段

        private float m_curScale = 1;
        private GameTimer m_gameTimer;
        private Action m_clickAction;
        private SkeletonGraphic m_skeletonGraphic;
        private Spine.AnimationState m_curAnimState;
        private Skin m_view;
        private List<string> m_currentSkinNames = new List<string>();

        #endregion

        #region 函数

        public void ChangeScale(float scale)
        {
            if(scale != m_curScale && m_goSpineModel != null)
            {
                m_curScale = scale;
                m_goSpineModel.transform.localScale = Vector3.one * m_curScale;
            }
        }

        public void ChangeMirror(bool isMirror)
        {
            if (m_goSpineModel != null)
            {
                m_goSpineModel.transform.localScale = new Vector3((isMirror ? -1 : 1) * m_curScale, m_curScale, 1);
            }
        }

        public void ChangeLocalPos(Vector3 pos)
        {
            if (m_goSpineModel != null)
            {
                m_goSpineModel.transform.localPosition = pos;
            }
        }

        public void SetAnimation(string animName, bool loop, bool forceReplay)
        {
            CancelTimer();

            if (m_curAnimState == null || m_skeletonGraphic == null || string.IsNullOrEmpty(animName))
            {
                return;
            }
            if (forceReplay)
            {
                m_curAnimState.ClearTracks();
                m_skeletonGraphic.Skeleton?.SetToSetupPose();
            }
            bool canFindAnim = m_curAnimState.Data?.SkeletonData?.FindAnimation(animName) != null;

            if (canFindAnim)
            {
                m_curAnimState.SetAnimation(0, animName, loop);
            }
        }

        public void SetAnimationDelay(string animName, bool loop, bool forceReplay, float dealy = 0)
        {
            if (dealy > 0 && !string.IsNullOrEmpty(animName))
            {
                CancelTimer();

                if (GameTimer.IsNull(m_gameTimer))
                {
                    m_gameTimer = GameModule.GameTimerModule.CreateUnscaledOnceGameTimer(dealy,
                        _ => { SetAnimation(animName, loop, forceReplay); });
                }
            }
        }

        public float GetAnimationDuration(string animName)
        {
            if (m_curAnimState == null || m_skeletonGraphic == null || string.IsNullOrEmpty(animName))
            {
                return 0;
            }

            if (m_curAnimState.TimeScale <= 0)
            {
                return 0;
            }
            var anim = m_curAnimState.Data?.SkeletonData?.FindAnimation(animName);

            if (anim != null)
            {
                return anim.Duration * (1 / m_curAnimState.TimeScale);
            }

            return 0;
        }

        /// <summary>
        /// 直接换皮
        /// </summary>
        /// <param name="skinName"></param>
        public void SwitchSkin(string skinName)
        {
            if(m_skeletonGraphic == null || string.IsNullOrEmpty(skinName))
            {
                return;
            }
            var skin = m_skeletonGraphic.Skeleton.Data.FindSkin(skinName);
            if (skin != null)
            {
                m_skeletonGraphic.Skeleton.SetSkin(skin);
                m_skeletonGraphic.Skeleton.SetSlotsToSetupPose();
            }
            else
            {
                DLogger.Error($"未找到皮肤: {skinName}");
            }
        }

        /// <summary>
        /// 组合皮
        /// </summary>
        /// <param name="skinNames"></param>
        public void SwitchSkin(List<string> skinNames)
        {
            if(m_skeletonGraphic == null || skinNames == null || skinNames.Count <= 0)
            {
                return;
            }
            if (IsSameSkinList(skinNames))
            {
                return;
            }
            if(m_view == null)
            {
                m_view = new Skin("View");
            }
            m_view.Clear();
            m_currentSkinNames.Clear();

            for (int i = 0; i < skinNames.Count; i++)
            {
                var skinName = skinNames[i];
                if (string.IsNullOrEmpty(skinName))
                {
                    continue;
                }
                var skin = m_skeletonGraphic.Skeleton.Data.FindSkin(skinName);
                if (skin != null)
                {
                    m_view.AddSkin(skin);
                    m_currentSkinNames.Add(skinName);
                }
                else
                {
                    DLogger.Error($"未找到皮肤: {skinName}");
                }
            }
            m_skeletonGraphic.Skeleton.SetSkin(m_view);
            m_skeletonGraphic.Skeleton.SetSlotsToSetupPose();
        }

        private bool IsSameSkinList(List<string> skinNames)
        {
            if (m_currentSkinNames.Count != skinNames.Count)
            {
                return false;
            }
            var currentSet = new HashSet<string>(m_currentSkinNames);
            var targetSet = new HashSet<string>(skinNames);
            return currentSet.SetEquals(targetSet);
        }

        public void SetSpineColor(Color color)
        {
            if (m_skeletonGraphic != null)
            {
                m_skeletonGraphic.color = color;
            }
        }

        private void CancelTimer()
        {
            GameModule.GameTimerModule.DestroyGameTimer(m_gameTimer);
            m_gameTimer = null;
        }

        public void BindClickEvent(Action clickAction, Vector2 clickRange)
        {
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = clickRange;
            }

            var image = transform.GetComponent<EmptyGraph>();

            if (image == null)
            {
                image = DGame.Utility.UnityUtil.AddMonoBehaviour<EmptyGraph>(gameObject);
                image.raycastTarget = true;
            }

            if (m_clickAction == null)
            {
                var button = DGame.Utility.UnityUtil.AddMonoBehaviour<UIButton>(gameObject);
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(OnSpineItemClick);
            }

            m_clickAction = clickAction;
        }

        private void OnSpineItemClick()
        {
            m_clickAction?.Invoke();
        }

        #endregion
    }
}

#endif