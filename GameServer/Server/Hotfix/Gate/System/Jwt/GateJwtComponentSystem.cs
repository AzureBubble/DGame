using System.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Fantasy;
using Fantasy.Entitas.Interface;
using Microsoft.IdentityModel.Tokens;
#pragma warning disable CS8625 // 无法将 null 字面量转换为非 null 的引用类型。

namespace Hotfix.Gate;

public sealed class GateJwtComponentAwakeSystem : AwakeSystem<GateJwtComponent>
{
    protected override void Awake(GateJwtComponent self)
    {
        self.Awake();
    }
}

public static class GateJwtComponentSystem
{
    public static void Awake(this GateJwtComponent self)
    {
        var rsa = RSA.Create();
        rsa.ImportRSAPublicKey(Convert.FromBase64String(self.PublicKeyPem), out _);
        self.SigningCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
        // 创建 TokenValidationParameters 对象 用于配置验证参数
        self.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = false,                   // 禁止令牌验证时间是否过期
            ValidateIssuer = true,                      // 验证发行者
            ValidateAudience = true,                    // 验证受众
            ValidateIssuerSigningKey = true,            // 验证签名密钥
            ValidIssuer = "Fantasy",                    // 有效的发行者
            ValidAudience = "Fantasy",                  // 有效的受众
            IssuerSigningKey = new RsaSecurityKey(rsa)  // RSA公钥作为签名密钥
        };
    }

    /// <summary>
    /// 验证登录Token
    /// </summary>
    /// <param name="self"></param>
    /// <param name="token">token</param>
    /// <param name="payload"></param>
    /// <returns></returns>
    public static bool ValidateToken(this GateJwtComponent self, string token, out JwtPayload payload)
    {
        payload = null;

        try
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();
            jwtSecurityTokenHandler.ValidateToken(token, self.TokenValidationParameters, out _);
            payload = jwtSecurityTokenHandler.ReadJwtToken(token).Payload;
            return true;
        }
        catch (SecurityTokenInvalidAudienceException e)
        {
            Log.Error($"验证受众失败: {e}");
            return false;
        }
        catch (SecurityTokenInvalidIssuerException e)
        {
            Log.Error($"验证发行者失败: {e}");
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"{e}");
            return false;
        }
    }
}