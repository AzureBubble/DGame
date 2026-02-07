using DGame;

namespace GameLogic
{
    public sealed class FrameAnimatorAgent : IMemory
    {

        public static FrameAnimatorAgent Create()
        {
            return null;
        }

        #region 释放资源

        /// <summary>
        /// 主动释放
        /// </summary>
        public void Release()
        {
            MemoryPool.Release(this);
        }

        /// <summary>
        /// 释放资源回调
        /// </summary>
        public void OnRelease()
        {
        }

        #endregion
    }
}