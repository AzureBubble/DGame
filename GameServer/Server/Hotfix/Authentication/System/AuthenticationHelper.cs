using Fantasy;
using Fantasy.Async;

namespace System;

public static class AuthenticationHelper
{
    /// <summary>
    /// 登录账号
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns></returns>
    public static async FTask<(uint errorCode, long accountId)> Login(Scene scene, string username, string password)
        => await scene.GetComponent<AuthenticationComponent>().Login(username, password);

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
    /// 删除登录账号缓存 仅供内部调用 不明白原理不要调用 否则后果自负
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="key">缓存Key</param>
    /// <param name="isDispose">是否销毁</param>
    internal static void RemoveLoginCache(Scene scene, string key, bool isDispose)
        => scene.GetComponent<AuthenticationComponent>().RemoveLoginCache(key, isDispose);

    /// <summary>
    /// 移除鉴权服务器账号缓存
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="userName">账户名</param>
    /// <param name="isDispose">是否销毁</param>
    public static void RemoveCache(Scene scene, string userName, bool isDispose)
        => scene.GetComponent<AuthenticationComponent>().RemoveCache(userName, isDispose);

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