using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix.Gate;

public sealed class C2G_LoginRequestHandler : MessageRPC<C2G_LoginRequest, G2C_LoginResponse>
{
    protected override async FTask Run(Session session, C2G_LoginRequest request, G2C_LoginResponse response, Action reply)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            // 客户端漏传Token  response.ErrorCode = 1006;
            // 恶意攻击  session.Dispose();
            Log.Error("恶意攻击");
            session.Dispose();
            return;
        }
        Log.Debug($"Gate登录：{session.Scene.SceneConfigId}");
        if (!GateJwtHelper.ValidateToken(session.Scene, request.Token, out var accountId))
        {
            // 如果失败 恶意攻击  session.Dispose();
            Log.Error("恶意攻击");
            session.Dispose();
            return;
        }
        Log.Debug($"当前Gate服务器: {session.Scene.SceneConfigId} accountId: {accountId}");
        await FTask.CompletedTask;
    }
}