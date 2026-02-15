using Fantasy;
using Fantasy.Async;

namespace Hotfix.Gate;

public static class GameAccountHelper
{
    /// <summary>
    /// 从数据库中读取GameAccount
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountId">uid</param>
    /// <returns></returns>
    public static async FTask<GameAccount> LoadDataBase(Scene scene, long uid)
    {
        var database = scene.World.Database;
        var gameAccount = await database.First<GameAccount>(d => d.Id == uid);

        if (gameAccount == null)
        {
            return null;
        }
        gameAccount.Deserialize(scene);
        return gameAccount;
    }

    /// <summary>
    /// 保存GameAccount到数据库中
    /// </summary>
    /// <returns></returns>
    public static async FTask SaveDataBase(this GameAccount self)
        => await self.Scene.World.Database.Save(self);

    /// <summary>
    /// 获得GameAccountInfo
    /// </summary>
    /// <param name="self"></param>
    /// <returns></returns>
    public static GameAccountInfo GetGameAccountInfo(this GameAccount self)
        // 可以不用每次都new一个新的GameAccountInfo
        // 可以在当前账号下创建一个GameAccountInfo 每次变动会提前通知这个GameAccountInfo
        // 或者每次调用该方法的时候 把值重新赋值一下
        => new GameAccountInfo()
        {
            CreateTime = self.CreateTime,
            LoginTime = self.LoginTime,
        };
}