using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Helper;

namespace Hotfix.Gate;

public static class GameAccountFactory
{
    /// <summary>
    /// 创建一个新的GameAccount
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="uid">token令牌传递的uid</param>
    /// <param name="isSaveDataBase">是否在创建过程中保存到数据库</param>
    /// <returns></returns>
    public static async FTask<GameAccount> Create(Scene scene, long uid, bool isSaveDataBase = true)
    {
        var gameAccount = Entity.Create<GameAccount>(scene, uid, false, false);
        gameAccount.LoginTime = gameAccount.CreateTime = TimeHelper.Now;

        if (isSaveDataBase)
        {
            await gameAccount.SaveDataBase();
        }

        return gameAccount;
    }
}