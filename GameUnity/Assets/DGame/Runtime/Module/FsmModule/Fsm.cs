using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    internal sealed class Fsm<T> : BaseFsm, IMemory, IFsm<T> where T : class
    {
        private T m_owner;
        private readonly Dictionary<Type, IFsmState<T>> m_states;
        private Dictionary<string, object> m_shareData;
        private IFsmState<T> m_currentState;
        private float m_currentStateTime;
        private bool m_isDestroyed;

        private IAnimModule m_animModule;
        private IAnimPlayable m_animPlayable;
        private Animator m_animator;
        public bool NeedSwitchAnim => m_animator != null && m_animPlayable != null;

        public T Owner => m_owner;
        public override Type OwnerType => typeof(T);
        public override int FsmStateCount => m_states.Count;
        public override bool IsRunning => m_currentState != null;
        public override bool IsDestroyed => m_isDestroyed;
        public IFsmState<T> CurrentState => m_currentState;
        public override string CurrentStateName => m_currentState?.GetType().FullName;
        public override float CurrentStateTime => m_currentStateTime;

        public Fsm()
        {
            m_owner = null;
            m_states = new Dictionary<Type, IFsmState<T>>();
            m_shareData = null;
            m_currentStateTime = 0;
            m_currentState = null;
            m_isDestroyed = true;
            m_animPlayable = null;
            m_animator = null;
        }

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="states"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, params IFsmState<T>[] states)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (states == null || states.Length <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;

            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        /// <summary>
        /// 创建有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="states"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, List<IFsmState<T>> states)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (states == null || states.Count <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        /// <summary>
        /// 创建带动画的有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="states"></param>
        /// <param name="animator"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, Animator animator, params IFsmState<T>[] states)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (animator == null)
            {
                throw new DGameException("animator对象无效");
            }

            if (states == null || states.Length <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_animModule = ModuleSystem.GetModule<IAnimModule>();
            fsm.m_animator = animator;
            fsm.m_animPlayable = fsm.m_animModule.CreateAnimPlayable(animator);
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;

            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        /// <summary>
        /// 创建带动画的有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="animator"></param>
        /// <param name="states"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, Animator animator, List<IFsmState<T>> states)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (animator == null)
            {
                throw new DGameException("animator对象无效");
            }

            if (states == null || states.Count <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_animModule = ModuleSystem.GetModule<IAnimModule>();
            fsm.m_animator = animator;
            fsm.m_animPlayable = fsm.m_animModule.CreateAnimPlayable(animator);
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        /// <summary>
        /// 创建带动画的有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="states"></param>
        /// <param name="animator"></param>
        /// <param name="animations"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, Animator animator, IFsmState<T>[] states, AnimationWrapper[] animations)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (animator == null)
            {
                throw new DGameException("animator对象无效");
            }

            if (states == null || states.Length <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            if (animations == null || animations.Length <= 0)
            {
                throw new DGameException("动画片段数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_animModule = ModuleSystem.GetModule<IAnimModule>();
            fsm.m_animator = animator;
            fsm.m_animPlayable = fsm.m_animModule.CreateAnimPlayable(animator);
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;

            fsm.AddAnimationClip(animations);

            for (int i = 0; i < states.Length; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        /// <summary>
        /// 创建带动画的有限状态机
        /// </summary>
        /// <param name="name"></param>
        /// <param name="owner"></param>
        /// <param name="animator"></param>
        /// <param name="states"></param>
        /// <param name="animations"></param>
        /// <returns></returns>
        /// <exception cref="DGameException"></exception>
        public static Fsm<T> Create(string name, T owner, Animator animator, List<IFsmState<T>> states, List<AnimationWrapper> animations)
        {
            if (owner == null)
            {
                throw new DGameException("有限状态机持有对象无效");
            }

            if (animator == null)
            {
                throw new DGameException("animator对象无效");
            }

            if (states == null || states.Count <= 0)
            {
                throw new DGameException("有限状态机状态数组无效");
            }

            if (animations == null || animations.Count <= 0)
            {
                throw new DGameException("动画片段数组无效");
            }

            Fsm<T> fsm = MemoryPool.Spawn<Fsm<T>>();
            fsm.m_animModule = ModuleSystem.GetModule<IAnimModule>();
            fsm.m_animator = animator;
            fsm.m_animPlayable = fsm.m_animModule.CreateAnimPlayable(animator);
            fsm.m_owner = owner;
            fsm.Name = name;
            fsm.m_isDestroyed = false;
            fsm.AddAnimationClip(animations);

            for (int i = 0; i < states.Count; i++)
            {
                var state = states[i];
                if (state == null)
                {
                    throw new DGameException("状态无效");
                }
                Type stateType = state.GetType();

                if (fsm.m_states.ContainsKey(stateType))
                {
                    throw new DGameException(Utility.StringUtil.Format("状态机 '{0}' 的状态 '{1}' 已存在",
                        new TypeNamePair(typeof(T), name), stateType.FullName));
                }

                fsm.m_states.Add(stateType, state);
                state.OnCreate(fsm);
            }
            return fsm;
        }

        public void Start<TState>() where TState : IFsmState<T>
        {
            Start(typeof(TState));
        }

        public void Start(Type stateType)
        {
            if (IsRunning)
            {
                throw new DGameException("状态机正在运行中，无法再次启动");
            }

            if (stateType == null)
            {
                throw new DGameException("启动的状态无效");
            }

            if (!typeof(IFsmState<T>).IsAssignableFrom(stateType))
            {
                throw new DGameException(Utility.StringUtil.Format("状态 '{0}' 无效", stateType.FullName));
            }

            IFsmState<T> state = GetState(stateType);

            if (state == null)
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 无法启动，状态 '{1}' 不存在",
                    new TypeNamePair(typeof(T), Name), stateType.FullName));
            }
            m_currentState = state;
            m_currentStateTime = 0f;
            m_currentState.OnEnter();

            if (NeedSwitchAnim)
            {
                m_animPlayable.PlayGraph();
            }
        }

        public void SwitchState<TState>() where TState : IFsmState<T>
        {
            SwitchState(typeof(TState));
        }

        public void SwitchState(Type type)
        {
            if (m_currentState == null)
            {
                throw new DGameException("当前状态无效");
            }
            IFsmState<T> state = GetState(type);

            if (state == null)
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 无法启动，状态 '{1}' 不存在",
                    new TypeNamePair(typeof(T), Name), type.FullName));
            }
            m_currentState?.OnExit();
            m_currentStateTime = 0f;
            m_currentState = state;
            m_currentState.OnEnter();
        }

        public void PlayAnimation(string animationName, float fadeDuration = 0.25f)
        {
            if (NeedSwitchAnim)
            {
                m_animPlayable.Play(animationName, fadeDuration);
            }
        }

        public bool ContainsFsmState<TState>() where TState : IFsmState<T>
        {
            return m_states.ContainsKey(typeof(TState));
        }

        public bool ContainsFsmState(Type type)
        {
            if (type == null)
            {
                throw new DGameException("状态无效");
            }

            if (!typeof(IFsmState<T>).IsAssignableFrom(type))
            {
                throw new DGameException(Utility.StringUtil.Format("状态 '{0}' 无效", type.FullName));
            }

            return m_states.ContainsKey(type);
        }

        public IFsmState<T> GetState<TState>() where TState : IFsmState<T>
        {
            return m_states.TryGetValue(typeof(TState), out var state) ? state : null;
        }

        public IFsmState<T> GetState(Type type)
        {
            if (type == null)
            {
                throw new DGameException("状态无效");
            }

            if (!typeof(IFsmState<T>).IsAssignableFrom(type))
            {
                throw new DGameException(Utility.StringUtil.Format("状态 '{0}' 无效", type.FullName));
            }
            return m_states.TryGetValue(type, out var state) ? state : null;
        }

        public IFsmState<T>[] GetAllStates()
        {
            if (m_states == null || m_states.Count <= 0)
            {
                return null;
            }

            IFsmState<T>[] results = new IFsmState<T>[m_states.Count];
            int index = 0;
            foreach (IFsmState<T> state in m_states.Values)
            {
                results[index++] = state;
            }
            return results;
        }

        public void GetAllStates(List<IFsmState<T>> states)
        {
            if (states == null)
            {
                states = new List<IFsmState<T>>();
            }

            states.Clear();
            foreach (KeyValuePair<Type, IFsmState<T>> state in m_states)
            {
                states.Add(state.Value);
            }
        }

        public void AddAnimationClip(params AnimationWrapper[] animations)
        {
            if (m_animPlayable == null)
            {
                throw new DGameException("动画控制器无效");
            }

            if (animations == null || animations.Length <= 0)
            {
                return;
            }

            for (int i = 0; i < animations.Length; i++)
            {
                var animation = animations[i];
                m_animPlayable.AddAnimationClip(animation.Clip.name, animation.Clip, animation.Layer);
            }
        }

        public void AddAnimationClip(List<AnimationWrapper> animations)
        {
            if (m_animPlayable == null)
            {
                throw new DGameException("动画控制器无效");
            }

            if (animations == null || animations.Count <= 0)
            {
                return;
            }

            for (int i = 0; i < animations.Count; i++)
            {
                var animation = animations[i];
                m_animPlayable.AddAnimationClip(animation.Clip.name, animation.Clip, animation.Layer);
            }
        }

        public void AddAnimationClip(AnimationWrapper animation)
        {
            if (m_animPlayable == null)
            {
                throw new DGameException("动画控制器无效");
            }

            if (animation == null || animation.Clip == null)
            {
                DLogger.Warning("动画片段无效");
                return;
            }

            m_animPlayable.AddAnimationClip(animation.Clip.name, animation.Clip, animation.Layer);
        }

        #region 黑板共享数据

        public bool ContainsShareData(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }

            if (m_shareData == null)
            {
                return false;
            }
            return m_shareData.ContainsKey(name);
        }

        public TData GetShareData<TData>(string name)
        {
            return (TData)GetShareData(name);
        }

        public object GetShareData(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }
            if (m_shareData == null)
            {
                return null;
            }
            return m_shareData.TryGetValue(name, out var data) ? data : null;
        }

        public bool TryGetShareData<TData>(string name, out TData data)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }
            data = default(TData);
            if (m_shareData == null)
            {
                return false;
            }

            if (m_shareData.TryGetValue(name, out var data2))
            {
                data = (TData)data2;
                return true;
            }

            return false;
        }

        public void AddShareData<TData>(string name, TData data)
        {
            AddShareData(name, (object)data);
        }

        public void AddShareData(string name, object data)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }

            if (m_shareData == null)
            {
                m_shareData = new Dictionary<string, object>(StringComparer.Ordinal);
            }
            m_shareData[name] = data;
        }

        public void UpdateShareData<TData>(string name, TData data)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }

            if (m_shareData == null)
            {
                return;
            }
            m_shareData[name] = data;
        }

        public bool RemoveShareData(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new DGameException("数据名称无效");
            }

            if (m_shareData == null)
            {
                return false;
            }

            return m_shareData.Remove(name);
        }

        public void ClearShareData()
        {
            m_shareData.Clear();
            m_shareData = null;
        }

        #endregion

        internal override void Update(float elapsedSeconds, float realElapsedSeconds)
        {
            if (m_currentState == null)
            {
                return;
            }
            m_currentStateTime += elapsedSeconds;
            m_currentState?.OnUpdate(elapsedSeconds, realElapsedSeconds);
        }

        internal override void FixedUpdate()
        {
            if (m_currentState == null)
            {
                return;
            }
            m_currentStateTime += GameTime.FixedDeltaTime;
            m_currentState?.OnFixedUpdate();
        }

        internal override void Destroy()
        {
            MemoryPool.Release(this);
        }

        public void OnRelease()
        {
            if (m_currentState != null)
            {
                m_currentState?.OnExit();
            }

            if (m_animPlayable != null)
            {
                m_animModule?.DestroyAnimPlayable(m_animPlayable);
            }

            foreach (IFsmState<T> state in m_states.Values)
            {
                state?.OnDestroy();
            }

            Name = null;
            m_owner = null;
            m_states?.Clear();
            m_shareData?.Clear();
            m_currentState = null;
            m_currentStateTime = 0;
            m_isDestroyed = true;
            m_animPlayable = null;
            m_animator = null;
        }
    }
}