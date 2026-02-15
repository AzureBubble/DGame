using System.Authentication;
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
            // 如果在Gate的缓存中已经存在了该账号那只能以下几种可能
            // 1、同一个客户端发送了重复登录的请求数据
            // 2、客户端经历了断线 又重新连接到这个服务器上 （断线重连）
            // 3、多个客户端同时登录这个账号 （顶号）
            if (session.RuntimeId == gameAccount.SessionRuntimeId)
            {
                // 如果执行到这里 说明是客户端发送了多次登录请求 这样的情况下 直接返回就可以了 不需要任何操作
                return;
            }

            Log.Debug("检测到当前账号的Session不是同一个");
            if (scene.TryGetEntity<Session>(gameAccount.SessionRuntimeId, out var oldSession))
            {
                Log.Debug("当前账号的Session已经存在 所以需要发送一个重复登录协议给客户端 并断开Session");
                // 如果能查到旧的Session 表示当前的会话还是存在的 有如下几个可能
                // 1、客户端断线重连 要给这个Session发送一个消息 通知它有人登录了
                // 2、其他的客户端登录了这个账号 要给这个Session 发送一个消息 通知它有人登录了
                // 给旧的客户端发送一个重复登录的消息 如果当前客户端是自己上次登录的 发送也收不到
                oldSession.Send(new G2C_RepeatLogin());
                // 延迟销毁旧的会话 延迟3秒 不定时销毁 有可能消息没有发送到客户端就销毁了
                oldSession.SetTimeOut(3000);
            }

        }
        Log.Debug("加入缓存中");
        gameAccount.SessionRuntimeId = session.RuntimeId;
        response.GameAccountInfo = gameAccount.GetGameAccountInfo();
        Log.Debug($"当前Gate服务器: {scene.SceneConfigId} accountId: {accountId}");
    }
}