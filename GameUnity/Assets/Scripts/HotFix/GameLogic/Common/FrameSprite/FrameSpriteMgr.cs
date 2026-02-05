using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    public sealed class FrameSpriteMgr : Singleton<FrameSpriteMgr>
    {
        private readonly Dictionary<string, FrameSpritePool> m_frameSpritePools = new Dictionary<string, FrameSpritePool>();

        public async UniTask<bool> InitFrameSpritePool(string location, FrameSpritePool pool)
        {
            if (!m_frameSpritePools.TryGetValue(location, out pool))
            {
                var goCfg = await GameModule.ResourceModule.LoadAssetAsync<GameObject>(location);
                if (goCfg != null)
                {
                    pool = goCfg.GetComponent<FrameSpritePool>();
                    m_frameSpritePools[location] = pool;
                }
            }
            return pool != null;
        }
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