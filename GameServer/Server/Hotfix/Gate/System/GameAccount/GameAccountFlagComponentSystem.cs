using Fantasy;
using Fantasy.Entitas.Interface;

namespace Hotfix.Gate;

public sealed class GameAccountFlagComponentDestroySystem : DestroySystem<GameAccountFlagComponent>
{
    protected override void Destroy(GameAccountFlagComponent self)
    {
        if (self.AccountId != 0)
        {
            // 执行下线过程 并且要求在五分钟后完成缓存清理 也就是五分钟后会保存数据到数据库
            // 由于5分钟太长 为方便测试 把时间改短 比如10秒
            GameAccountHelper.Disconnect(self.Scene, self.AccountId, 1000 * 60 * 5).Coroutine();
            self.AccountId = 0;
        }
    }
}