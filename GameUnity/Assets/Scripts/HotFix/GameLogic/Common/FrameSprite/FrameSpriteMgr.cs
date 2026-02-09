using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public sealed class FrameSpriteMgr : Singleton<FrameSpriteMgr>
    {
        private readonly Dictionary<string, FrameSpritePool> m_frameSpritePools = new Dictionary<string, FrameSpritePool>();

        /// <summary>
        /// 获取FrameSpritePool资源
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <returns></returns>
        public async UniTask<FrameSpritePool> GetFrameSpritePool(string location)
        {
            if (!m_frameSpritePools.TryGetValue(location, out var pool))
            {
                var goCfg = await GameModule.ResourceModule.LoadAssetAsync<GameObject>(location);
                if (goCfg != null)
                {
                    pool = goCfg.GetComponent<FrameSpritePool>();
                    m_frameSpritePools[location] = pool;
                }
            }
            return pool;
        }

        /// <summary>
        /// 清空所有FrameSpritePool
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in m_frameSpritePools.Values)
            {
                GameModule.ResourceModule.UnloadAsset(pool.gameObject);
            }
            m_frameSpritePools.Clear();
        }
    }
}