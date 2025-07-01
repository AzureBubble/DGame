using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DGame
{
    public interface IGameTimerModule
    {
        /// <summary>
        /// 创建受时间缩放影响的Loop次数计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="loopCount">循环次数。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateLoopCountGameTimer(float interval, int loopCount, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的Loop次数计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="loopCount">循环次数。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledLoopCountGameTimer(float interval, int loopCount, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建受时间缩放影响的Loop计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateLoopGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建受时间缩放影响的一次计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateOnceGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的Loop计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledLoopGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建不受时间缩放影响的一次计时器。
        /// </summary>
        /// <param name="handler">计时器回调。</param>
        /// <param name="interval">计时器间隔。</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns>计时器Id。</returns>
        GameTimer CreateUnscaledOnceGameTimer(float interval, TimerHandler handler, object[] args = null);

        /// <summary>
        /// 创建计时器
        /// </summary>
        /// <param name="interval">计时器间隔</param>
        /// <param name="isLoop">是否循环计时器</param>
        /// <param name="isUnscaled">是否受时间缩放影响</param>
        /// <param name="handler">计时器回调</param>
        /// <param name="args">传参。(避免闭包)</param>
        /// <returns></returns>
        GameTimer CreateGameTimer(float interval, bool isLoop, bool isUnscaled, TimerHandler handler,
            object[] args = null);

        /// <summary>
        /// 移除计时器。
        /// </summary>
        /// <param name="timer">计时器Id。</param>
        void DestroyGameTimer(GameTimer timer);

        /// <summary>
        /// 移除所有计时器。
        /// </summary>
        void DestroyAllGameTimer();
    }
}