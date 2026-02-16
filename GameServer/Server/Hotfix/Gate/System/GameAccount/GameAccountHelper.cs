using Fantasy;
using Fantasy.Async;
using Fantasy.Network;

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
    /// 账号完整的断开逻辑 执行这个接口后 该账号会完全下线
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="uid"></param>
    /// <param name="timeOut"></param>
    public static async FTask Disconnect(Scene scene, long uid, int timeOut = 1000 * 60 * 3)
    {
        // 调用该方法有如下几种情况
        // 1、客户端主动断开 如：退出游戏、切换账号等 客户端会主动发送一个协议给服务器通知服务器断开
        // 2、客户端断线 客户端不会主动发送协议给服务器 是由服务器的心跳来检测 是否断开
        // 如果是心跳检测断开的Session 怎么拿到当前账号进行下线处理
        // 通过给当前的Session挂载一个组件 当销毁这个Session的时候 也会销毁这个组件
        // 这样就可以在登录的时候 给这个组件把AccountId 存到这个组件

        // 要检查当前缓存中是否存在该账号的数据
        var gameAccountManageComponent = scene.GetComponent<GameAccountManageComponent>();

        if (!gameAccountManageComponent.TryGet(uid, out var gameAccount))
        {
            // 如果缓存没有 表示已经下线了或根本不存在该账号 应该打印一个警告 因为正常情况不会出现
            Log.Warning($"GameAccountHelper Disconnect uid: {uid} not found");
            return;
        }
        // 为了防止逻辑的错误 加一个警告来排除下
        if (!scene.TryGetEntity<Session>(gameAccount.SessionRuntimeId, out var session))
        {
            // 如果没有找到对应的Session 那只有一种可能就是当前的连接会话已经断开了 一般情况下也不会出现 也需要警告
            Log.Warning($"GameAccountHelper Disconnect session uid: {uid} SessionRuntimeId: {gameAccount.SessionRuntimeId} not found");
            return;
        }

        if (gameAccount.IsHaveTimeOutComponent())
        {
            // 如果已经存在定时任务组件 表示当前已经有一个延时断开的任务了 不需要重复添加
            return;
        }
        // 下线处理
        if (timeOut <= 0)
        {
            // 如果小于等于0 立即执行下线处理
            await gameAccount.Disconnect();
            return;
        }
        // 如果不存在定时任务组件 那就添加并设置定时任务
        // 设置延时下线
        gameAccount.SetTimeOut(timeOut, gameAccount.Disconnect);
    }

    /// <summary>
    /// 执行该账号的断开逻辑 不到非必要不要使用这个接口 这个接口是内部使用
    /// </summary>
    /// <param name="self"></param>
    private static async FTask Disconnect(this GameAccount self)
    {
        Log.Debug("Disconnect");
        // 保存该账号信息到数据库
        await self.SaveDataBase();
        // 在缓存移除该账号 并执行Dispose方法
        self.Scene.GetComponent<GameAccountManageComponent>().Remove(self.Id);
    }

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