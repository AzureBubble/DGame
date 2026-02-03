using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace DGame
{
    internal sealed class AnimClip : AnimNode
    {
        public readonly string Name;
        public int AnimHashCode => Animator.StringToHash(Name);
        private readonly AnimationClip m_clip;
        private readonly AnimationClipPlayable m_clipPlayable;
        private readonly float m_fadeDuration;

        public float FadeDuration => m_fadeDuration;

        /// <summary>
        /// 动画层级
        /// </summary>
        public int Layer { get; private set; } = 0;

        /// <summary>
        /// 动画长度
        /// </summary>
        public float ClipLength
        {
            get
            {
                if (m_clip == null)
                {
                    return 0f;
                }

                if (Speed == 0f)
                {
                    return Mathf.Infinity;
                }
                return m_clip.length / Speed;
            }
        }

        /// <summary>
        /// 归一化时间轴
        /// </summary>
        public float NormalizedTime
        {
            get => m_clip == null ? 1f : Time / m_clip.length;
            set
            {
                if (m_clip == null)
                {
                    return;
                }
                Time = m_clip.length * value;
            }
        }

        /// <summary>
        /// 动画模式
        /// </summary>
        public WrapMode WrapMode { get; }

        /// <summary>
        /// 动画信息
        /// </summary>
        public AnimInfo Info { get; private set; }

        public AnimClip(PlayableGraph graph, AnimationClip clip, string name, WrapMode wrapMode, int layer, float fadeDuration = 0f) : base(graph)
        {
            m_clip = clip;
            Name = name;
            Layer = layer;
            m_fadeDuration = fadeDuration;
            m_clipPlayable = AnimationClipPlayable.Create(graph, clip);
            m_clipPlayable.SetApplyFootIK(false);
            m_clipPlayable.SetApplyPlayableIK(false);
            SetSourcePlayable(m_clipPlayable);
            WrapMode = wrapMode;
            if (WrapMode == WrapMode.Once)
            {
                m_clipPlayable.SetDuration(clip.length);
            }

            Info = new AnimInfo(this);
        }

        public override void Play()
        {
            if (WrapMode == WrapMode.Once || WrapMode == WrapMode.ClampForever)
            {
                Time = 0;
            }

            base.Play();
        }
    }
}