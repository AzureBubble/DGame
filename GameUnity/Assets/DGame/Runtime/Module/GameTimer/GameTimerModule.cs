using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DGame
{
    public class GameTimerModule : Module, IUpdateModule, IGameTimerModule
    {
        private int m_timerID = 0;
        public int GlobalTimerID => ++m_timerID;

        private readonly DGameLinkedList<GameTimer> m_gameTimers = new DGameLinkedList<GameTimer>();
        private readonly DGameLinkedList<GameTimer> m_unscaleGameTimers = new DGameLinkedList<GameTimer>();

        private bool m_hasBadFrame = false;
        private bool m_hasUnscaleBadFrame = false;
        private readonly int m_maxBadFrameCheckCnt = 10;

        public override void OnCreate()
        {

        }

        #region Update

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            UpdateTimer(elapseSeconds);
            HandleLoopBadFrame();
            UpdateUnscaleTimer(realElapseSeconds);
            HandleUnscaleLoopBadFrame();
        }

        private void UpdateTimer(float elapseSeconds)
        {
            bool hasBadFrame = false;
            var curNode = m_gameTimers.First;

            while (curNode != null)
            {
                var nextNode = curNode.Next;

                if (curNode.Value.IsNeedRemove)
                {
                    curNode.Value.Destroy();
                    m_gameTimers.Remove(curNode);
                    curNode = nextNode;
                    continue;
                }

                if (!curNode.Value.IsRunning)
                {
                    curNode = nextNode;
                    continue;
                }

                curNode.Value.TriggerTime -= elapseSeconds;

                if (curNode.Value.TriggerTime <= 0)
                {
                    curNode.Value.Handler?.Invoke(curNode.Value.Args);

                    if (curNode.Value.HasLoopCount)
                    {
                        curNode.Value.LoopCount -= 1;

                        if (curNode.Value.LoopCount > 0)
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;
                            if (curNode.Value.TriggerTime <= 0)
                            {
                                hasBadFrame = true;
                            }
                        }
                        else
                        {
                            curNode.Value.Destroy();
                            m_gameTimers.Remove(curNode);
                        }
                    }
                    else
                    {
                        if (curNode.Value.IsLoop)
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                            if (curNode.Value.TriggerTime <= 0)
                            {
                                hasBadFrame = true;
                            }
                        }
                        else
                        {
                            curNode.Value.Destroy();
                            m_gameTimers.Remove(curNode);
                        }
                    }
                }

                curNode = nextNode;

                if (curNode == m_gameTimers.First)
                {
                    return;
                }
            }

            m_hasBadFrame = hasBadFrame;
        }

        private void HandleLoopBadFrame()
        {
            int maxBadFrameCheckCnt = m_maxBadFrameCheckCnt;
            while (m_hasBadFrame && maxBadFrameCheckCnt > 0)
            {
                m_hasBadFrame = false;
                var curNode = m_gameTimers.First;

                while (curNode != null && maxBadFrameCheckCnt-- > 0)
                {
                    var nextNode = curNode.Next;

                    if (curNode.Value.IsLoop && curNode.Value.TriggerTime <= 0)
                    {
                        curNode.Value.Handler?.Invoke(curNode.Value.Args);

                        if (curNode.Value.HasLoopCount)
                        {
                            curNode.Value.LoopCount -= 1;

                            if (curNode.Value.LoopCount > 0)
                            {
                                curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                                if (curNode.Value.TriggerTime <= 0)
                                {
                                    m_hasBadFrame = true;
                                }
                            }
                            else
                            {
                                curNode.Value.Destroy();
                                m_gameTimers.Remove(curNode);
                            }
                        }
                        else
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                            if (curNode.Value.TriggerTime <= 0)
                            {
                                m_hasBadFrame = true;
                            }
                        }
                    }
                    curNode = nextNode;
                }
            }
        }

        private void UpdateUnscaleTimer(float realElapseSeconds)
        {
            bool hasUnscaledBadFrame = false;
            var curNode = m_unscaleGameTimers.First;

            while (curNode != null)
            {
                var nextNode = curNode.Next;

                if (curNode.Value.IsNeedRemove)
                {
                    curNode.Value.Destroy();
                    m_unscaleGameTimers.Remove(curNode);
                    curNode = nextNode;
                    continue;
                }

                if (!curNode.Value.IsRunning)
                {
                    curNode = nextNode;
                    continue;
                }

                curNode.Value.TriggerTime -= realElapseSeconds;

                if (curNode.Value.TriggerTime <= 0)
                {
                    curNode.Value.Handler?.Invoke(curNode.Value.Args);

                    if (curNode.Value.HasLoopCount)
                    {
                        curNode.Value.LoopCount -= 1;

                        if (curNode.Value.LoopCount > 0)
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;
                            if (curNode.Value.TriggerTime <= 0)
                            {
                                hasUnscaledBadFrame = true;
                            }
                        }
                        else
                        {
                            curNode.Value.Destroy();
                            m_unscaleGameTimers.Remove(curNode);
                        }
                    }
                    else
                    {
                        if (curNode.Value.IsLoop)
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                            if (curNode.Value.TriggerTime <= 0)
                            {
                                hasUnscaledBadFrame = true;
                            }
                        }
                        else
                        {
                            curNode.Value.Destroy();
                            m_unscaleGameTimers.Remove(curNode);
                        }
                    }

                    // if (curNode.Value.IsLoop)
                    // {
                    //     curNode.Value.TriggerTime += curNode.Value.IntervalTime;
                    //
                    //     if (curNode.Value.TriggerTime <= 0)
                    //     {
                    //         hasUnscaledBadFrame = true;
                    //     }
                    // }
                    // else
                    // {
                    //     curNode.Value.Destroy();
                    //     m_unscaleGameTimers.Remove(curNode);
                    // }
                }

                curNode = nextNode;

                if (curNode == m_unscaleGameTimers.First)
                {
                    return;
                }
            }

            m_hasUnscaleBadFrame = hasUnscaledBadFrame;
        }

        private void HandleUnscaleLoopBadFrame()
        {
            int maxBadFrameCheckCnt = m_maxBadFrameCheckCnt;
            while (m_hasUnscaleBadFrame && maxBadFrameCheckCnt > 0)
            {
                m_hasUnscaleBadFrame = false;
                var curNode = m_unscaleGameTimers.First;

                while (curNode != null && maxBadFrameCheckCnt-- > 0)
                {
                    var nextNode = curNode.Next;

                    if (curNode.Value.IsLoop && curNode.Value.TriggerTime <= 0)
                    {
                        curNode.Value.Handler?.Invoke(curNode.Value.Args);
                        if (curNode.Value.HasLoopCount)
                        {
                            curNode.Value.LoopCount -= 1;

                            if (curNode.Value.LoopCount > 0)
                            {
                                curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                                if (curNode.Value.TriggerTime <= 0)
                                {
                                    m_hasUnscaleBadFrame = true;
                                }
                            }
                            else
                            {
                                curNode.Value.Destroy();
                                m_unscaleGameTimers.Remove(curNode);
                            }
                        }
                        else
                        {
                            curNode.Value.TriggerTime += curNode.Value.IntervalTime;

                            if (curNode.Value.TriggerTime <= 0)
                            {
                                m_hasUnscaleBadFrame = true;
                            }
                        }

                        // curNode.Value.TriggerTime += curNode.Value.IntervalTime;
                        //
                        // if (curNode.Value.TriggerTime <= 0)
                        // {
                        //     m_hasUnscaleBadFrame = true;
                        // }
                    }
                    curNode = nextNode;
                }
            }
        }

        #endregion

        public override void OnDestroy()
        {
            DestroyAllGameTimer();
        }

        #region CreateTimer

        private void InsertGameTimer(GameTimer timer)
        {
            if (timer.IsUnscaled)
            {
                if (m_unscaleGameTimers.Count <= 0)
                {
                    m_unscaleGameTimers.AddLast(timer);
                    return;
                }

                var curNode = m_unscaleGameTimers.First;

                while (curNode != null && curNode.Value.TriggerTime <= timer.TriggerTime)
                {
                    curNode = curNode.Next;
                }

                if (curNode == null)
                {
                    m_unscaleGameTimers.AddLast(timer);
                }
                else
                {
                    m_unscaleGameTimers.AddBefore(curNode, timer);
                }
            }
            else
            {
                if (m_gameTimers.Count <= 0)
                {
                    m_gameTimers.AddLast(timer);
                    return;
                }

                var curNode = m_gameTimers.First;

                while (curNode != null && curNode.Value.TriggerTime <= timer.TriggerTime)
                {
                    curNode = curNode.Next;
                }

                if (curNode == null)
                {
                    m_gameTimers.AddLast(timer);
                }
                else
                {
                    m_gameTimers.AddBefore(curNode, timer);
                }
            }
        }

        public GameTimer CreateLoopCountGameTimer(float interval, int loopCount, TimerHandler handler,
            object[] args = null)
        {
            return CreateGameTimer(interval,false, loopCount, handler, args);
        }

        public GameTimer CreateLoopGameTimer(float interval, TimerHandler handler, object[] args = null)
        {
            return CreateGameTimer(interval, true, false, handler, args);
        }

        public GameTimer CreateOnceGameTimer(float interval, TimerHandler handler, object[] args = null)
        {
            return CreateGameTimer(interval, false, false, handler, args);
        }

        public GameTimer CreateUnscaledLoopCountGameTimer(float interval, int loopCount, TimerHandler handler,
            object[] args = null)
        {
            return CreateGameTimer(interval,true, loopCount, handler, args);
        }

        public GameTimer CreateUnscaledLoopGameTimer(float interval, TimerHandler handler, object[] args = null)
        {
            return CreateGameTimer(interval, true, true, handler, args);
        }

        public GameTimer CreateUnscaledOnceGameTimer(float interval, TimerHandler handler, object[] args = null)
        {
            return CreateGameTimer(interval, false, true, handler, args);
        }

        public GameTimer CreateGameTimer(float interval, bool isLoop, bool isUnscaled, TimerHandler handler, object[] args = null)
        {
            GameTimer timer = new GameTimer(GlobalTimerID, interval, isUnscaled, isLoop, handler, args);
            InsertGameTimer(timer);
            return timer;
        }

        public GameTimer CreateGameTimer(float interval, bool isUnscaled, int loopCount, TimerHandler handler, object[] args = null)
        {
            GameTimer timer = new GameTimer(GlobalTimerID, interval, isUnscaled, loopCount, handler, args);
            InsertGameTimer(timer);
            return timer;
        }

        /// <summary>
        /// 立即移除。
        /// </summary>
        /// <param name="timer"></param>
        private void DestroyGameTimerImmediate(GameTimer timer)
        {
            if (!GameTimer.IsNull(timer))
            {
                if (timer.IsUnscaled && m_unscaleGameTimers.Contains(timer))
                {
                    m_unscaleGameTimers.Remove(timer);
                }

                if(!timer.IsUnscaled && m_gameTimers.Contains(timer))
                {
                    m_gameTimers.Remove(timer);
                }
            }
        }


        public void DestroyGameTimer(GameTimer timer)
        {
            if (!GameTimer.IsNull(timer))
            {
                timer.IsNeedRemove = true;
            }
        }

        public void DestroyAllGameTimer()
        {
            m_unscaleGameTimers.Clear();
            m_unscaleGameTimers.ClearNodePool();
            m_gameTimers.Clear();
            m_gameTimers.ClearNodePool();
        }

        #endregion
    }
}