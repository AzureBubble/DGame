using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Helper;

namespace System;

public sealed class AuthenticationComponentDestroySystem : DestroySystem<AuthenticationComponent>
{
    protected override void Destroy(AuthenticationComponent self)
    {
        foreach (var account in self.CacheAccountList.Values.ToArray())
        {
            account.Dispose();
        }
        self.CacheAccountList.Clear();
    }
}

internal static class AuthenticationComponentSystem
{
    /// <summary>
    /// 鉴权注册接口
    /// </summary>
    internal static async FTask<uint> Register(this AuthenticationComponent self, string userName, string password,
        string source)
    {
        // 1、检查传递的参数是否完整以及合法
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            // 代表注册账号参数不完整或不合法
            return 1;
        }

        var userNameHashCode = userName.GetHashCode();
        var scene = self.Scene;

        // 利用协程锁 解决异步原子性问题
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.Authentication_RegisterLock, userNameHashCode))
        {
            // 利用缓存来减少频繁请求数据或缓存的压力
            if (self.CacheAccountList.TryGetValue(userName, out var account))
            {
                Log.Info($"[Register] 缓存中的数据");
                // 代表用户已经存在
                return 2;
            }

            Log.Info($"[Register] 数据库中的数据");
            // 2、数据库查询账号是否存在
            var worldDatabase = scene.World.Database;
            bool isExist = await worldDatabase.Exist<Account>(d => d.Username == userName);

            if (isExist)
            {
                // 代表用户已经存在
                return 2;
            }

            // 3、执行到这里 表示数据库或缓存没有该账号的注册信息 需要创建一个
            account = Entity.Create<Account>(scene, true, true);
            account.Username = userName;
            account.Password = password;
            account.CreateTime = TimeHelper.Now;

            // 4、写入实体到数据库中
            await worldDatabase.Save(account);
            // 5、把账号缓存到字典中
            self.CacheAccountList.TryAdd(userName, account);
            // 添加TimeOut组件用来定时清除账号缓存
            account.AddComponent<AccountTimeOutComponent>().TimeOut(4000);

            Log.Info($"[Register] source: [{source}]注册一个账号 用户名: [{userName}] 账户ID: [{account.Id}]");
            // 代表注册成功
            return 0;
        }
    }

    /// <summary>
    /// 删除鉴权服务器账号缓存
    /// </summary>
    /// <param name="self"></param>
    /// <param name="userName">账户名</param>
    /// <param name="isDispose">是否销毁</param>
    internal static void RemoveCache(this AuthenticationComponent self, string userName, bool isDispose)
    {
        if (!self.CacheAccountList.Remove(userName, out var account))
        {
            return;
        }

        Log.Info($"[RemoveCache] Remove cache source: 用户名: [{userName}] 账号缓存字典数量: {self.CacheAccountList.Count}");

        if (isDispose)
        {
            account.Dispose();
        }
    }

    /// <summary>
    /// 移除账号
    /// </summary>
    /// <param name="self"></param>
    /// <param name="accountID">账号ID</param>
    /// <param name="source">移除原因/来源</param>
    /// <returns></returns>
    internal static async FTask<uint> Remove(this AuthenticationComponent self, long accountID, string source)
    {
        var scene = self.Scene;
        // 这里没必要加协程锁
        using (var @lock = await scene.CoroutineLockComponent.Wait((int)LockType.Authentication_RemoveLock, accountID))
        {
            // 2、数据库查询账号是否存在
            var worldDatabase = scene.World.Database;
            await worldDatabase.Remove<Account>(accountID);
            Log.Info($"[Remove] source: [{source}]移除账号成功accountID: {accountID}");
            return 0;
        }
    }
}