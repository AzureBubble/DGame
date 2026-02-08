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
    /// <returns></returns>
    public static async FTask<uint> Register(Scene scene, string username, string password)
        => await scene.GetComponent<AuthenticationComponent>().Register(username, password);
}