using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace System;

public sealed class C2A_RegisterRequestHandler : MessageRPC<C2A_RegisterRequest, A2C_RegisterResponse>
{
    protected override async FTask Run(Session session, C2A_RegisterRequest request, A2C_RegisterResponse response, Action reply)
    {
        if (!session.CheckInterval(2000))
        {
            // 返回这个 3 代表操作过于频繁
            response.ErrorCode = 3;
            return;
        }
        // var authenticationSessionComponent = session.GetComponent<AuthenticationSessionComponent>();
        //
        // if (authenticationSessionComponent == null)
        // {
        //     // 如果没有这个组件 就挂载组件 设置间隔时间是 2 秒
        //     authenticationSessionComponent = session.AddComponent<AuthenticationSessionComponent>();
        //     authenticationSessionComponent.SetInterval(2000);
        //     // 3秒后自动销毁的Session
        //     authenticationSessionComponent.TimeOut(5000);
        // }
        // else
        // {
        //     if (!authenticationSessionComponent.CheckInterval())
        //     {
        //
        //     }
        // }
        session.SetTimeOut(3000);
        response.ErrorCode = await AuthenticationHelper.Register(session.Scene, request.UserName, request.Password, "用户注册");
    }
}