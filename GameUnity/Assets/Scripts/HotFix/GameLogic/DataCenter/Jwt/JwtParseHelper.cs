using System;
using DGame;

namespace GameLogic
{
    public static class JwtParseHelper
    {
        /// <summary>
        /// 解析登录Token JWT 性能更好 内存分配一次 可读性差
        /// </summary>
        /// <param name="token"></param>
        /// <param name="payloadEntry"></param>
        public static bool Parse(string token, out Payload payloadEntry)
        {
            payloadEntry = null;

            try
            {
                // JWT通常是由三部分组成 Header Payload Signature
                // 如果没有三部分则为非法的
                // 用 Span 避免创建数组
                var tokenSpan = token.AsSpan();

                // 查找第一个点
                var firstDot = tokenSpan.IndexOf('.');

                if (firstDot == -1)
                {
                    DLogger.Error($"[ParseJwt] Invalid Jwt Token: {token}");
                    return false;
                }

                // 查找第二个点
                var secondDot = tokenSpan.Slice(firstDot + 1).IndexOf('.');

                if (secondDot == -1)
                {
                    DLogger.Error($"[ParseJwt] Invalid Jwt Token: {token}");
                    return false;
                }

                // 提取 Payload 部分
                var payloadSpan = tokenSpan.Slice(firstDot + 1, secondDot);

                // 原地修改字符，避免创建新字符串
                Span<char> basePayload = stackalloc char[payloadSpan.Length];
                payloadSpan.CopyTo(basePayload);

                // 填充并转字符串 除数是 4 余数只可能是 0、1、2、3 最大补齐是3
                // 所以统一+3覆盖所有情况 除4向下取整得到+1的数量 *4就是补齐整除4的数量
                // (6+3) / 4 * 4 = 9 / 4 * 4 = 2.25 * 4 = 2 * 4 = 8 补齐 2
                // (13+3) / 4 * 4 = 16 / 4 * 4 = 4 / 4 * 4 = 4 * 4 = 16 补齐3
                var paddedLength = (basePayload.Length + 3) / 4 * 4;
                basePayload = stackalloc char[paddedLength];
                payloadSpan.CopyTo(basePayload);

                // "-" 替代成了 "+"  "_" 替换成了 "/" 填充 '=' 一次完成
                for (int i = 0; i < paddedLength; i++)
                {
                    if (i < payloadSpan.Length)
                    {
                        basePayload[i] = payloadSpan[i] == '-'
                            ? '+'
                            : payloadSpan[i] == '_'
                                ? '/'
                                : payloadSpan[i];
                    }
                    else
                    {
                        basePayload[i] = '=';
                    }
                }

                var basePayloadStr = new string(basePayload);
                var bytes = Convert.FromBase64String(basePayloadStr);
                var payload = System.Text.Encoding.UTF8.GetString(bytes);
                payloadEntry = DGame.Utility.JsonUtil.ToObject<Payload>(payload);
                DLogger.Info($"[ParseJwt] JWT parse success payload: {payload}");
                return true;
            }
            catch (Exception e)
            {
                DLogger.Error($"[ParseJwt] JWT parse fail error: {e}");
                return false;
            }
        }

        // 性能稍差 内存分配三次 但代码可读性更高 客户端仅在登录的时候调用一次 性能无影响
        // public static bool Parse(string token, out Payload payloadEntry)
        // {
        //     payloadEntry = null;
        //     try
        //     {
        //         // JWT通常是由三部分组成 Header Payload Signature
        //         var tokens = token.Split('.');
        //
        //         if (tokens.Length != 3)
        //         {
        //             DLogger.Error($"[ParseJwt] Invalid Jwt Token: {token}");
        //             return false;
        //         }
        //
        //         // JWT 的Payload不是标准的 Base64 格式 因为C#的Convert要求是要一个标准的 Base64 格式
        //         // JWT 的Payload是 Base64URL 的格式 里面的 "-" 替代成了 "+"  "_" 替换成了 "/" 需要把这些还原成 Base64 格式
        //         var basePayload = tokens[1].Replace('-', '+').Replace('_', '/');
        //         // 因为 Base64 的编码长度需要是4的倍数 如果不是 需要把这个长度用=来填充 使其长度符合要求
        //         // switch (basePayload.Length % 4)
        //         // {
        //         //     // case 0:
        //         //     //     // 如果余数是 0 表示长度已经是4的倍数 不用处理
        //         //     //     break;
        //         //     //
        //         //     // case 1:
        //         //     //     // 如果余数是 1 表示格式不正确 不是服务器发放的 Token 也不需要处理
        //         //     //     break;
        //         //
        //         //     case 2:
        //         //         basePayload += "==";
        //         //         break;
        //         //
        //         //     case 3:
        //         //         basePayload += "=";
        //         //         break;
        //         // }
        //         // PadRight一行搞定
        //         basePayload = basePayload.PadRight((basePayload.Length + 3) / 4 * 4, '=');
        //
        //         // 将修复后的字符串解码成数组
        //         var basePayloadBytes = Convert.FromBase64String(basePayload);
        //         var payload = System.Text.Encoding.UTF8.GetString(basePayloadBytes);
        //         payloadEntry = DGame.Utility.JsonUtil.ToObject<Payload>(payload);
        //         DLogger.Info($"[ParseJwt] JWT parse success payload: {payload}");
        //         return true;
        //     }
        //     catch (Exception e)
        //     {
        //         DLogger.Error($"[ParseJwt] JWT parse fail error: {e}");
        //         return false;
        //     }
        // }
    }
}