using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Internal;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngineInternal;

namespace DGame
{
    public static partial class Utility
    {
        public static class UnityUtil
        {
            private static IMonoDriver m_monoDriver;

            #region 控制协程Coroutine

            public static GameCoroutine StartCoroutine(string name, IEnumerator routine, MonoBehaviour bindBehaviour)
            {
                if (bindBehaviour == null)
                {
                    Debugger.Error("StartCoroutine {0} failed, bindBehaviour is null", name);
                    return null;
                }

                var behaviour = bindBehaviour;
                return StartCoroutine(behaviour, name, routine);
            }

            public static GameCoroutine StartCoroutine(string name, IEnumerator routine, GameObject bindGo)
            {
                if (bindGo == null)
                {
                    Debugger.Error("StartCoroutine {0} failed, BindGo is null", name);
                    return null;
                }

                var behaviour = GetDefaultBehaviour(bindGo);
                return StartCoroutine(behaviour, name, routine);
            }

            public static GameCoroutine StartGlobalCoroutine(string name, IEnumerator routine)
            {
                var coroutine = StartCoroutine(routine);
                var gameCoroutine = new GameCoroutine();
                gameCoroutine.Coroutine = coroutine;
                gameCoroutine.Name = name;
                gameCoroutine.BindBehaviour = null;
                return gameCoroutine;
            }

            public static void StopCoroutine(GameCoroutine coroutine)
            {
                if (coroutine.Coroutine != null)
                {
                    var behaviour = coroutine.BindBehaviour;
                    if (behaviour != null)
                    {
                        behaviour.StopCoroutine(coroutine.Coroutine);
                    }

                    coroutine.Coroutine = null;
                    coroutine.BindBehaviour = null;
                }
            }

            private static GameCoroutine StartCoroutine(MonoBehaviour behaviour, string name, IEnumerator routine)
            {
                var coroutine = behaviour.StartCoroutine(routine);
                var gameCoroutine = new GameCoroutine
                {
                    Coroutine = coroutine,
                    Name = name,
                    BindBehaviour = behaviour
                };
                return gameCoroutine;
            }

            private static GameCoroutineAgent GetDefaultBehaviour(GameObject bindGameObject)
            {
                if (bindGameObject != null)
                {
                    if (bindGameObject.TryGetComponent(out GameCoroutineAgent coroutineBehaviour))
                    {
                        return coroutineBehaviour;
                    }

                    return bindGameObject.AddComponent<GameCoroutineAgent>();
                }

                return null;
            }


            public static Coroutine StartCoroutine(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(methodName);
            }

            public static Coroutine StartCoroutine(IEnumerator routine)
            {
                if (routine == null)
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(routine);
            }

            public static Coroutine StartCoroutine(string methodName, [DefaultValue("null")] object value)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return null;
                }

                _MakeEntity();
                return m_monoDriver.StartCoroutine(methodName, value);
            }

            public static void StopCoroutine(string methodName)
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(methodName);
            }

            public static void StopCoroutine(IEnumerator routine)
            {
                if (routine == null)
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(routine);
            }

            public static void StopCoroutine(Coroutine routine)
            {
                if (routine == null)
                {
                    return;
                }

                _MakeEntity();
                m_monoDriver.StopCoroutine(routine);
                routine = null;
            }

            public static void StopAllCoroutines()
            {
                _MakeEntity();
                m_monoDriver.StopAllCoroutines();
            }

            #endregion

            #region 注入UnityUpdate/FixedUpdate/LateUpdate

            /// <summary>
            /// 为给外部提供的 添加帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddUpdateListener(Action fun)
            {
                _MakeEntity();
                AddUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddUpdateListenerImp(Action fun)
            {
                await UniTask.Yield( /*PlayerLoopTiming.LastPreUpdate*/);
                m_monoDriver.AddUpdateListener(fun);
            }

            /// <summary>
            /// 为给外部提供的 添加物理帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddFixedUpdateListener(Action fun)
            {
                _MakeEntity();
                AddFixedUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddFixedUpdateListenerImp(Action fun)
            {
                await UniTask.Yield(PlayerLoopTiming.LastEarlyUpdate);
                m_monoDriver.AddFixedUpdateListener(fun);
            }

            /// <summary>
            /// 为给外部提供的 添加Late帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddLateUpdateListener(Action fun)
            {
                _MakeEntity();
                AddLateUpdateListenerImp(fun).Forget();
            }

            private static async UniTaskVoid AddLateUpdateListenerImp(Action fun)
            {
                await UniTask.Yield( /*PlayerLoopTiming.LastPreLateUpdate*/);
                m_monoDriver.AddLateUpdateListener(fun);
            }

            /// <summary>
            /// 移除帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveUpdateListener(fun);
            }

            /// <summary>
            /// 移除物理帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveFixedUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveFixedUpdateListener(fun);
            }

            /// <summary>
            /// 移除Late帧更新事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveLateUpdateListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveLateUpdateListener(fun);
            }

            #endregion

            #region Unity Events 注入

            /// <summary>
            /// 为给外部提供的Destroy注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddDestroyListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.AddDestroyListener(fun);
            }

            /// <summary>
            /// 为给外部提供的Destroy反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveDestroyListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveDestroyListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnDrawGizmos注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddOnDrawGizmosListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.AddOnDrawGizmosListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnDrawGizmos反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveOnDrawGizmosListener(Action fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveOnDrawGizmosListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnApplicationPause注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void AddOnApplicationPauseListener(Action<bool> fun)
            {
                _MakeEntity();
                m_monoDriver.AddOnApplicationPauseListener(fun);
            }

            /// <summary>
            /// 为给外部提供的OnApplicationPause反注册事件。
            /// </summary>
            /// <param name="fun"></param>
            public static void RemoveOnApplicationPauseListener(Action<bool> fun)
            {
                _MakeEntity();
                m_monoDriver.RemoveOnApplicationPauseListener(fun);
            }

            #endregion

            private static void _MakeEntity()
            {
                if (m_monoDriver != null)
                {
                    return;
                }

                m_monoDriver = ModuleSystem.GetModule<IMonoDriver>();
            }

            #region FindObjectOfType

            public static T FindObjectOfType<T>() where T : UnityEngine.Object
            {
// #if UNITY_6000_0_OR_NEWER
//                 return UnityEngine.Object.FindFirstObjectByType<T>();
// #else
//                 return UnityEngine.Object.FindObjectOfType<T>();
// #endif

                return UnityEngine.Object.FindFirstObjectByType<T>();
            }

            #endregion

            #region 自定义组件事件管理

            /// <summary>
            /// 添加自定义事件监听器到指定控件的 EventTrigger 上。
            /// </summary>
            /// <param name="control">要添加监听器的控件</param>
            /// <param name="type">事件类型</param>
            /// <param name="action">回调函数</param>
            public static void AddCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
            {
                // 找到控件身上的EventTrigger组件
                EventTrigger trigger = AddMonoBehaviour<EventTrigger>(control);
                // 创建一个 EventTrigger.Entry 条目并设置事件类型和回调函数
                EventTrigger.Entry entry = new EventTrigger.Entry
                {
                    eventID = type
                };
                if (entry.callback == null)
                {
                    entry.callback = new EventTrigger.TriggerEvent();
                }
                entry.callback.AddListener(action);
                // 将条目添加到 EventTrigger 的触发器列表中
                trigger.triggers?.Add(entry);
            }

            /// <summary>
            /// 移除自定义事件监听器到指定控件的 EventTrigger 上。
            /// </summary>
            /// <param name="control">要添加监听器的控件</param>
            /// <param name="type">事件类型</param>
            /// <param name="action">回调函数</param>
            public static void RemoveCustomEventListener(UIBehaviour control, EventTriggerType type, UnityAction<BaseEventData> action)
            {
                // 找到控件身上的EventTrigger组件
                EventTrigger trigger = control.GetComponent<EventTrigger>();
                if (trigger?.triggers != null)
                {
                    EventTrigger.Entry entry;
                    for (int i = 0; i < trigger.triggers.Count; i++)
                    {
                        entry = trigger.triggers[i];
                        if (entry?.callback == null)
                        {
                            continue;
                        }
                        if (entry.eventID == type && entry.callback.GetPersistentMethodName(0) == action.Method.Name)
                        {
                            // 移除匹配的条目
                            trigger.triggers.RemoveAt(i);
                            break;
                        }
                    }
                    trigger.triggers.RemoveAll(e => e?.callback == null || e.callback?.GetPersistentEventCount() == 0);
                }
            }

            #endregion

            #region AddComponent

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static Component AddMonoBehaviour(Type type, GameObject go)
            {
                var comp = go.GetComponent(type);
                if (comp == null)
                {
                    comp = go.AddComponent(type);
                }

                return comp;
            }

            public static T AddMonoBehaviour<T>(Component comp) where T : Component
            {

                var ret = comp.GetComponent<T>();
                if (ret == null)
                {
                    ret = comp.gameObject.AddComponent<T>();
                }

                return ret;
            }

            public static T AddMonoBehaviour<T>(GameObject go) where T : Component
            {
                var comp = go.GetComponent<T>();
                if (comp == null)
                {
                    comp = go.AddComponent<T>();
                }

                return comp;
            }

            [TypeInferenceRule(TypeInferenceRules.TypeReferencedByFirstArgument)]
            public static void RmvMonoBehaviour(Type type, GameObject go)
            {
                var comp = go.GetComponent(type);
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }

            public static void RmvMonoBehaviour<T>(GameObject go) where T : Component
            {
                var comp = go.GetComponent<T>();
                if (comp != null)
                {
                    UnityEngine.Object.Destroy(comp);
                }
            }

            #endregion
        }

        public class GameCoroutine
        {
            public string Name;
            public Coroutine Coroutine;
            public MonoBehaviour BindBehaviour;
        }

        class GameCoroutineAgent : MonoBehaviour
        {
        }
    }
}