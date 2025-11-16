using UnityEngine;

namespace DGame
{
    public interface IAnimPlayable
    {
        /// <summary>
        /// 动画图的名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 动画片段数量
        /// </summary>
        int AnimClipCount { get; }

        /// <summary>
        /// 动画混合器数量
        /// </summary>
        int AnimMixerCount { get; }

        bool IsDestroyed { get; }

        /// <summary>
        /// 播放动画图
        /// </summary>
        void PlayGraph();

        /// <summary>
        /// 停止动画图
        /// </summary>
        void StopGraph();

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="elapsedSeconds"></param>
        void Update(float elapsedSeconds);

        /// <summary>
        /// 获取动画片段信息
        /// </summary>
        /// <param name="animName"></param>
        /// <returns></returns>
        AnimInfo GetAnimInfo(string animName);

        /// <summary>
        /// 动画片段是否在播放中
        /// </summary>
        /// <param name="animName"></param>
        /// <returns></returns>
        bool IsPlaying(string animName);

        /// <summary>
        /// 播放一个动画
        /// </summary>
        /// <param name="animName">动画名</param>
        /// <param name="fadeDuration">融合时间</param>
        void Play(string animName, float fadeDuration);

        /// <summary>
        /// 停止播放动画
        /// </summary>
        /// <param name="animName"></param>
        void Stop(string animName);

        /// <summary>
        /// 添加一个动画片段
        /// </summary>
        /// <param name="animName">动画名</param>
        /// <param name="clip">动画片段</param>
        /// <param name="layer">动画层级</param>
        /// <returns></returns>
        bool AddAnimationClip(string animName, AnimationClip clip, int layer = 0);

        /// <summary>
        /// 删除一个动画片段
        /// </summary>
        /// <param name="animName">动画名</param>
        /// <returns></returns>
        bool RemoveAnimationClip(string animName);

        /// <summary>
        /// 是否包含一个动画状态
        /// </summary>
        /// <param name="animName">动画名称</param>
        /// <returns></returns>
        bool ContainsAnimationClip(string animName);

        /// <summary>
        /// 销毁动画图
        /// </summary>
        void DestroyGraph();
    }
}