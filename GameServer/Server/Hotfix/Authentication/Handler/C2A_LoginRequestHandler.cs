using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace System;

public sealed class C2A_LoginRequestHandler : MessageRPC<C2A_LoginRequest, A2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2A_LoginRequest request, A2C_LoginResponse response, Action reply)
    {
        var scene = session.Scene;
        var result = await AuthenticationHelper.Login(scene, request.UserName, request.Password);
        response.ErrorCode = result.errorCode;
        response.Token = AuthenticationJwtHelper.GetToken(scene, result.accountId, "127.0.0.1", 8080);
    }
}