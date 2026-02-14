using System.IdentityModel.Tokens.Jwt;
using Fantasy;
using Microsoft.IdentityModel.Tokens;

namespace Hotfix.Gate;

public static class GateJwtHelper
{
    /// <summary>
    /// 验证登录Token是否合法
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="token">Token</param>
    /// <param name="accountId">验证成功 返回accountId</param>
    /// <returns></returns>
    public static bool ValidateToken(Scene scene, string token, out long accountId)
    {
        accountId = 0;
        if (!ValidateToken(scene, token, out JwtPayload payload))
        {
            // 如果令牌验证失败 表示当前令牌不合法 返回False 让上层处理
            return false;
        }

        var sceneId = Convert.ToInt64(payload["sceneId"]);
        accountId =  Convert.ToInt64(payload["uid"]);
        // 不等于当前Scene的ConfigID的话 就表示不应该连接到当前的Gate里
        // 所以需要关闭当前连接
        return sceneId == scene.SceneConfigId;
    }

    /// <summary>
    /// 验证登录Token
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="token">token</param>
    /// <param name="payload"></param>
    /// <returns></returns>
    private static bool ValidateToken(Scene scene, string token, out JwtPayload payload)
        => scene.GetComponent<GateJwtComponent>().ValidateToken(token, out payload);
}