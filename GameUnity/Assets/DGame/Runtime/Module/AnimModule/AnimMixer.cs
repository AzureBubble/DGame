using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DGame
{
    internal sealed class AnimMixer : AnimNode
    {
        private const float HIDE_DURATION = 0.25f;
        private readonly List<AnimClip> m_animClips = new List<AnimClip>(8);
        private AnimationMixerPlayable m_mixer;
        private bool m_isQuiting = false;

        /// <summary>
        /// 动画层级
        /// </summary>
        public int Layer { get; private set; }

        public AnimMixer(PlayableGraph graph, int layer) : base(graph)
        {
            Layer = layer;
            m_mixer = AnimationMixerPlayable.Create(graph);
            SetSourcePlayable(m_mixer);
        }

        public override void Update(float elapsedSeconds)
        {
            base.Update(elapsedSeconds);

            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];

                if (animClip != null)
                {
                    animClip.Update(elapsedSeconds);
                }
            }

            bool isAllDone = true;

            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];
                if (animClip != null)
                {
                    if (!animClip.IsDone)
                    {
                        isAllDone = false;
                        break;
                    }
                }
            }

            // 当所有子节点都已完成 断开连接
            if (isAllDone && !m_isQuiting)
            {
                m_isQuiting = true;
                StartWeightFade(0, HIDE_DURATION);
            }

            if (m_isQuiting && Mathf.Approximately(Weight, 0f))
            {
                DisconnectMixer();
            }
        }

        private void DisconnectMixer()
        {
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];
                if (animClip != null && animClip.IsConnected)
                {
                    animClip.Disconnect();
                    m_animClips[i] = null;
                }
            }

            Disconnect();
        }

        /// <summary>
        /// 播放指定动画
        /// </summary>
        /// <param name="animClip"></param>
        /// <param name="fadeDuration"></param>
        public void Play(AnimClip animClip, float fadeDuration)
        {
            m_isQuiting = false;
            StartWeightFade(1f, 0);
            if(!ContainsAnimClip(animClip))
            {
                int index = m_animClips.FindIndex(s => s == null);

                if (index == -1)
                {
                    int inputCount = m_mixer.GetInputCount();
                    m_mixer.SetInputCount(inputCount + 1);
                    animClip.Connect(m_mixer, inputCount);
                    m_animClips.Add(animClip);
                }
                else
                {
                    animClip.Connect(m_mixer, index);
                    m_animClips[index] = animClip;
                }
            }

            for (int i = 0; i < m_animClips.Count; i++)
            {
                var clip = m_animClips[i];

                if (clip == null)
                {
                    continue;
                }

                if (clip == animClip)
                {
                    clip.StartWeightFade(1f, fadeDuration);
                    clip.Play();
                }
                else
                {
                    clip.StartWeightFade(0f, fadeDuration);
                    clip.Pause();
                }
            }
        }

        public void Stop(int animHashCode)
        {
            AnimClip animClip = FindAnimClip(animHashCode);

            if (animClip == null)
            {
                return;
            }
            animClip.Pause();
            animClip.Reset();
        }


        public void PauseAll()
        {
            for (int i = 0; i < m_animClips.Count; i++)
            {
                var animClip = m_animClips[i];

                if (animClip != null)
                {
                    animClip.Pause();
                }
            }
        }

        public void RemoveAnimClip(int animHashCode)
        {
            AnimClip animClip = FindAnimClip(animHashCode);

            if (animClip == null)
            {
                return;
            }

            if (animClip.IsConnected)
            {
                animClip.Disconnect();
            }

            m_animClips[animClip.InputPort] = null;
            animClip.Destroy();
        }

        private AnimClip FindAnimClip(int animHashCode)
        {
            // var animHashCode = Animator.StringToHash(name);
            foreach (var item in m_animClips)
            {
                if (item != null && item.AnimHashCode == animHashCode)
                {
                    return item;
                }
            }

            DLogger.Warning($"{nameof(AnimClip)} 不存在：{animHashCode}");
            return null;
        }

        public bool ContainsAnimClip(AnimClip animClip)
        {
            foreach (var clip in m_animClips)
            {
                if (clip == animClip)
                {
                    return true;
                }
            }
            return false;
        }
    }
}