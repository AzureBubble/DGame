using Fantasy;
using Fantasy.Async;

namespace System;

public static class AuthenticationHelper
{
    /// <summary>
    /// 注册新账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="source">注册来源</param>
    /// <returns></returns>
    public static async FTask<uint> Register(Scene scene, string username, string password, string source)
        => await scene.GetComponent<AuthenticationComponent>().Register(username, password, source);

    /// <summary>
    /// 移除账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="accountID">账户ID</param>
    /// <param name="source">移除原因/来源</param>
    /// <returns></returns>
    public static async FTask<uint> Register(Scene scene, long accountID, string source)
        => await scene.GetComponent<AuthenticationComponent>().Remove(accountID, source);
}