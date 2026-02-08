using Fantasy;
using Fantasy.Async;
using Fantasy.Entitas;
using Fantasy.Helper;

namespace System;

internal static class AuthenticationComponentSystem
{
    /// <summary>
    /// 鉴权注册接口
    /// </summary>
    internal static async FTask<uint> Register(this AuthenticationComponent cmpt, string userName, string password)
    {
        // 1、检查传递的参数是否完整以及合法
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            // 代表注册账号参数不完整或不合法
            return 1;
        }

        // 2、数据库查询账号是否存在
        var worldDatabase = cmpt.Scene.World.Database;
        bool isExist = await worldDatabase.Exist<Account>(d => d.Username == userName);

        if (isExist)
        {
            // 代表用户已经存在
            return 2;
        }

        // 3、执行到这里 表示数据库或缓存没有该账号的注册信息 需要创建一个
        var account = Entity.Create<Account>(cmpt.Scene, true, true);
        account.Username = userName;
        account.Password = password;
        account.CreateTime = TimeHelper.Now;

        // 4、写入实体到数据库中
        await worldDatabase.Save(account);
        // 写入到数据库中后 account没有作用了 所以释放掉
        account.Dispose();

        // 代表注册成功
        return 0;
    }
}