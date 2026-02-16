using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

namespace Hotfix.Gate;

public sealed class C2G_GetAccountInfoRequestHandler : MessageRPC<C2G_GetAccountInfoRequest, G2C_GetAccountInfoResponse>
{
    protected override async FTask Run(Session session, C2G_GetAccountInfoRequest request, G2C_GetAccountInfoResponse response, Action reply)
    {
        var gameAccountFlagComponent = session.GetComponent<GameAccountFlagComponent>();

        if (gameAccountFlagComponent == null)
        {
            // 表示不应该访问这个协议 要先登录 才能获取GameAccountFlagComponent
            // response.ErrorCode = 1007;
            session.Dispose();
            return;
        }

        GameAccount gameAccount = gameAccountFlagComponent.GameAccount;

        if (gameAccount == null)
        {
            // 表示这个Account已经被销毁过 不是我们需要的
        }
        response.GameAccountInfo = gameAccount.GetGameAccountInfo();
        await FTask.CompletedTask;
    }
}