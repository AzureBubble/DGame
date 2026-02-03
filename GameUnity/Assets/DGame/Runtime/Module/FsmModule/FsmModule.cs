using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DGame
{
    internal sealed class FsmModule : Module, IUpdateModule, IFsmModule
    {
        private readonly Dictionary<TypeNamePair, BaseFsm> m_fsmMap;
        private readonly List<BaseFsm> m_tempFsmList;

        public override int Priority => 1;

        public int Count => m_fsmMap.Count;

        public FsmModule()
        {
            m_fsmMap = new Dictionary<TypeNamePair, BaseFsm>();
            m_tempFsmList = new List<BaseFsm>();
        }

        public override void OnCreate()
        {

        }

        public override void OnDestroy()
        {
            foreach (var fsm in m_fsmMap.Values)
            {
                fsm?.Destroy();
            }
            m_fsmMap.Clear();
            m_tempFsmList.Clear();
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            m_tempFsmList.Clear();

            if (m_fsmMap.Count <= 0)
            {
                return;
            }

            foreach (var fsm in m_fsmMap.Values)
            {
                m_tempFsmList.Add(fsm);
            }

            for (int i = 0; i < m_tempFsmList.Count; i++)
            {
                if (m_tempFsmList[i] == null || m_tempFsmList[i].IsDestroyed)
                {
                    continue;
                }
                m_tempFsmList[i]?.Update(elapseSeconds, realElapseSeconds);
            }
        }

        #region Contains FSM

        private bool InternalContainsFsm(TypeNamePair pair)
        {
            return m_fsmMap.ContainsKey(pair);
        }

        public bool ContainsFsm<T>() where T : class
        {
            return InternalContainsFsm(new TypeNamePair(typeof(T)));
        }

        public bool ContainsFsm(Type type)
        {
            if (type == null)
            {
                throw new DGameException("持有者类型无效");
            }

            return InternalContainsFsm(new TypeNamePair(type));
        }

        public bool ContainsFsm<T>(string name) where T : class
        {
            return InternalContainsFsm(new TypeNamePair(typeof(T), name));
        }

        public bool ContainsFsm(Type type, string name)
        {
            if (type == null)
            {
                throw new DGameException("持有者类型无效");
            }

            return InternalContainsFsm(new TypeNamePair(type, name));
        }

        #endregion

        #region Get FSM

        private BaseFsm GetFsm(TypeNamePair pair)
        {
            return m_fsmMap.GetValueOrDefault(pair);
        }

        public IFsm<T> GetFsm<T>() where T : class
        {
            return (IFsm<T>)GetFsm(new TypeNamePair(typeof(T)));
        }

        public BaseFsm GetFsm(Type ownerType)
        {
            if (ownerType == null)
            {
                throw new DGameException("持有者类型无效");
            }
            return GetFsm(new TypeNamePair(ownerType));
        }

        public IFsm<T> GetFsm<T>(string name) where T : class
        {
            return (IFsm<T>)GetFsm(new TypeNamePair(typeof(T), name));
        }

        public BaseFsm GetFsm(Type ownerType, string name)
        {
            if (ownerType == null)
            {
                throw new DGameException("持有者类型无效");
            }
            return GetFsm(new TypeNamePair(ownerType, name));
        }

        public BaseFsm[] GetAllFsms()
        {
            if (m_tempFsmList == null || m_fsmMap.Count <= 0)
            {
                return null;
            }
            BaseFsm[] results = new BaseFsm[m_fsmMap.Count];
            int index = 0;
            foreach (var fsm in m_fsmMap.Values)
            {
                results[index++] = fsm;
            }
            return results;
        }

        public void GetAllFsms(List<BaseFsm> results)
        {
            if (results == null)
            {
                throw new DGameException("传入的参数无效");
            }
            results.Clear();
            foreach (var fsm in m_fsmMap.Values)
            {
                results.Add(fsm);
            }
        }

        #endregion

        #region Create FSM

        public IFsm<T> CreateFsm<T>(T owner, params IFsmState<T>[] states) where T : class
        {
            return CreateFsm<T>(string.Empty, owner, states);
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, params IFsmState<T>[] states) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, states);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
        }

        public IFsm<T> CreateFsm<T>(T owner, List<IFsmState<T>> states) where T : class
        {
            return CreateFsm<T>(string.Empty, owner, states);
        }

        public IFsm<T> CreateFsm<T>(string name, T owner, List<IFsmState<T>> states) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, states);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
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
        public IFsm<T> CreateFsm<T>(string name, T owner, Animator animator, params IFsmState<T>[] states) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, animator, states);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
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
        public IFsm<T> CreateFsm<T>(string name, T owner, Animator animator, List<IFsmState<T>> states) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, animator, states);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
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
        public IFsm<T> CreateFsm<T>(string name, T owner, Animator animator,
            IFsmState<T>[] states, AnimationWrapper[] animations) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, animator, states, animations);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
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
        public IFsm<T> CreateFsm<T>(string name, T owner, Animator animator, List<IFsmState<T>> states,
            List<AnimationWrapper> animations) where T : class
        {
            TypeNamePair pair = new TypeNamePair(typeof(T), name);

            if (ContainsFsm<T>(name))
            {
                throw new DGameException(Utility.StringUtil.Format("状态机 {0} 已经存在", pair));
            }
            var fam = Fsm<T>.Create(name, owner, animator, states, animations);
            m_fsmMap.TryAdd(pair, fam);
            return fam;
        }


        #endregion

        #region Destroy FSM

        private bool InternalDestroyFsm(TypeNamePair pair)
        {
            if (m_fsmMap.TryGetValue(pair, out BaseFsm fam))
            {
                fam?.Destroy();
                return m_fsmMap.Remove(pair);
            }
            return false;
        }

        public bool DestroyFsm<T>() where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T)));
        }

        public bool DestroyFsm(Type type)
        {
            if (type == null)
            {
                throw new DGameException("持有者类型无效");
            }
            return InternalDestroyFsm(new TypeNamePair(type));
        }

        public bool DestroyFsm<T>(string name) where T : class
        {
            return InternalDestroyFsm(new TypeNamePair(typeof(T), name));
        }

        public bool DestroyFsm(Type type, string name)
        {
            if (type == null)
            {
                throw new DGameException("持有者类型无效");
            }
            return InternalDestroyFsm(new TypeNamePair(type, name));
        }

        public bool DestroyFsm<T>(IFsm<T> fsm) where T : class
        {
            if (fsm == null)
            {
                throw new DGameException("状态机无效");
            }
            return InternalDestroyFsm(new TypeNamePair(typeof(T), fsm.Name));
        }

        public bool DestroyFsm(BaseFsm fsm)
        {
            if (fsm == null)
            {
                throw new DGameException("状态机无效");
            }
            return InternalDestroyFsm(new TypeNamePair(fsm.OwnerType, fsm.Name));
        }

        #endregion
    }
}