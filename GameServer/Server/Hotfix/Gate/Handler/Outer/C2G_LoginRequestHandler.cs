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

        var scene = session.Scene;
        if (!GateJwtHelper.ValidateToken(scene, request.Token, out var accountId))
        {
            // 如果失败 恶意攻击  session.Dispose();
            Log.Error("恶意攻击");
            session.Dispose();
            return;
        }

        Log.Debug("检查该账号是否存在");
        // 在缓存中检查该账号是否存在
        var gameAccountManageComponent = scene.GetComponent<GameAccountManageComponent>();
        if (!gameAccountManageComponent.TryGet(accountId, out var gameAccount))
        {
            // 首先到数据库查询是否存在这个账号
            gameAccount = await GameAccountHelper.LoadDataBase(scene, accountId);
            // 如果有就直接加入缓存中即可
            if (gameAccount == null)
            {
                Log.Debug("不存在 表示需要创建新账号");
                // 如果没有就创建新的 然后保存到数据库中
                // 如果不存在 表示需要创建新账号
                gameAccount = await GameAccountFactory.Create(scene, accountId);
            }
            // 把创建完成的账号存入缓存中
            gameAccountManageComponent.Add(gameAccount);
        }
        else
        {
            Log.Debug("在缓存中");
        }
        Log.Debug("加入缓存中");
        response.GameAccountInfo = gameAccount.GetGameAccountInfo();
        Log.Debug($"当前Gate服务器: {scene.SceneConfigId} accountId: {accountId}");
    }
}