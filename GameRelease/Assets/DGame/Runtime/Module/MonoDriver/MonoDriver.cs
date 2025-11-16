using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    internal sealed partial class MonoDriver : Module, IMonoDriver
    {
        private GameObject m_monoDriver;
        private MainMonoBehaviour m_monoBehaviour;

        private void _MakeDriver()
        {
            if (m_monoDriver != null)
            {
                return;
            }

            m_monoDriver = new GameObject("[MonoDriver]");
            m_monoDriver.SetActive(true);
            GameObject.DontDestroyOnLoad(m_monoDriver);
            m_monoBehaviour = m_monoDriver.AddComponent<MainMonoBehaviour>();
        }

        public override void OnCreate()
        {
            _MakeDriver();
        }

        public override void OnDestroy()
        {
            m_monoBehaviour?.Destroy();
            if (m_monoDriver != null)
            {
                GameObject.Destroy(m_monoDriver);
            }
            m_monoDriver = null;
        }

        #region 协程控制相关

        public Coroutine StartCoroutine(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return null;
            }
            _MakeDriver();
            return m_monoBehaviour.StartCoroutine(methodName);
        }

        public Coroutine StartCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                return null;
            }
            _MakeDriver();
            return m_monoBehaviour.StartCoroutine(routine);
        }

        public Coroutine StartCoroutine(string methodName, object value)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return null;
            }
            _MakeDriver();
            return m_monoBehaviour.StartCoroutine(methodName, value);
        }

        public void StopCoroutine(string methodName)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return;
            }
            if (m_monoDriver != null)
            {
                m_monoBehaviour?.StopCoroutine(methodName);
            }
        }

        public void StopCoroutine(IEnumerator routine)
        {
            if (routine == null)
            {
                return;
            }

            if (m_monoDriver != null)
            {
                m_monoBehaviour?.StopCoroutine(routine);
            }
        }

        public void StopCoroutine(Coroutine routine)
        {
            if (routine == null)
            {
                return;
            }

            if (m_monoDriver != null)
            {
                m_monoBehaviour?.StopCoroutine(routine);
                routine = null;
            }
        }

        public void StopAllCoroutines()
        {
            if (m_monoDriver != null)
            {
                m_monoBehaviour?.StopAllCoroutines();
            }
        }

        #endregion

        #region 注入 Unity Update/FixedUpdate/LateUpdate 事件

        public void AddUpdateListener(Action action)
        {
            _MakeDriver();
            AddUpdateListenerAsync(action).Forget();
        }

        private async UniTaskVoid AddUpdateListenerAsync(Action action)
        {
            await UniTask.Yield();
            m_monoBehaviour?.AddUpdateListener(action);
        }

        public void RemoveUpdateListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveUpdateListener(action);
        }

        public void AddFixedUpdateListener(Action action)
        {
            _MakeDriver();
            AddFixedUpdateListenerAsync(action).Forget();
        }

        private async UniTaskVoid AddFixedUpdateListenerAsync(Action action)
        {
            // 即将进入 FixedUpdate 之前
            await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
            m_monoBehaviour?.AddFixedUpdateListener(action);
        }

        public void RemoveFixedUpdateListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveFixedUpdateListener(action);
        }

        public void AddLateUpdateListener(Action action)
        {
            _MakeDriver();
            AddLateUpdateListenerAsync(action).Forget();
        }

        private async UniTaskVoid AddLateUpdateListenerAsync(Action action)
        {
            await UniTask.Yield();
            m_monoBehaviour?.AddLateUpdateListener(action);
        }

        public void RemoveLateUpdateListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveLateUpdateListener(action);
        }

        #endregion

        #region Unity 生命周期事件注入

        public void AddDestroyListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.AddDestroyListener(action);
        }

        public void RemoveDestroyListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveDestroyListener(action);
        }

        public void AddOnDrawGizmosListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.AddOnDrawGizmosListener(action);
        }

        public void RemoveOnDrawGizmosListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveOnDrawGizmosListener(action);
        }

        public void AddOnDrawGizmosSelectedListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.AddOnDrawGizmosSelectedListener(action);
        }

        public void RemoveOnDrawGizmosSelectedListener(Action action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveOnDrawGizmosSelectedListener(action);
        }

        public void AddOnApplicationPauseListener(Action<bool> action)
        {
            _MakeDriver();
            m_monoBehaviour?.AddOnApplicationPauseListener(action);
        }

        public void RemoveOnApplicationPauseListener(Action<bool> action)
        {
            _MakeDriver();
            m_monoBehaviour?.RemoveOnApplicationPauseListener(action);
        }

        #endregion
    }
}