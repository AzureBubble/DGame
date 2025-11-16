using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace GameLogic
{
    [System.Serializable]
    public class UIButtonClickScaleExtend
    {
        [SerializeField] private bool m_isUseClickScale = true;
        [SerializeField] private float m_clickScaleTime = 0.1f;
        [SerializeField] private float m_clickRecoverTime = 0.18f;

        [SerializeField] private Vector3 m_normalScale = Vector3.one;
        [SerializeField] private Vector3 m_clickScale = new Vector3(0.95f, 0.95f, 0.95f);

        [SerializeField] private List<Transform> m_childList;

        [SerializeField] private bool m_isUseDoTween = true; // 开启缩放动画效果
        [SerializeField] private bool m_reboundEffect = true; // 回弹效果开启

        public void OnPointerDown(Transform transf, bool interactable)
        {
            if (m_isUseClickScale && interactable)
            {
                if (m_isUseDoTween)
                {
                    transf?.DOKill();
                    transf.DOScale(m_clickScale, m_clickScaleTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                    {
                        transf?.DOKill();
                    };

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        foreach (var child in m_childList)
                        {
                            child?.DOKill();
                            child.DOScale(m_clickScale, m_clickScaleTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                            {
                                child?.DOKill();
                            };
                        }
                    }
                }
                else
                {
                    transf.localScale = m_clickScale;

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        for (var index = 0; index < m_childList.Count; index++)
                        {
                            var child = m_childList[index];
                            child.localScale = m_clickScale;
                        }
                    }
                }
            }
        }

        public void OnPointerUp(Transform transf, bool interactable)
        {
            if (transf == null)
            {
                return;
            }
            if (m_isUseClickScale && interactable)
            {
                if (m_isUseDoTween)
                {
                    transf.DOKill();
                    transf.DOScale(m_normalScale, m_clickRecoverTime).SetUpdate(true).SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete += () =>
                    {
                        transf.DOKill();
                    };

                    if (m_childList != null && m_childList.Count > 0)
                    {
                        for (var index = 0; index < m_childList.Count; index++)
                        {
                            var child = m_childList[index];
                            child?.DOKill();
                            child.DOScale(m_normalScale, m_clickRecoverTime).SetUpdate(true)
                                    .SetEase(m_reboundEffect ? Ease.OutBack : Ease.Unset).onComplete +=
                                () => { child?.DOKill(); };
                        }
                    }
                }
                else
                {
                    transf.localScale = m_normalScale;
                    foreach (var child in m_childList)
                    {
                        child.localScale = m_normalScale;
                    }
                }
            }
        }

        private void KillTween(Transform transf)
        {
            transf?.DOKill();

            if (m_childList != null && m_childList.Count > 0)
            {
                for (int i = 0; i < m_childList.Count; i++)
                {
                    m_childList[i]?.DOKill();
                }
            }
        }

        public void OnDisable(Transform transf)
        {
            KillTween(transf);
        }

        public void OnDestroy(Transform transf)
        {
            KillTween(transf);
        }
    }
}