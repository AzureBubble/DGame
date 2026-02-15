namespace GameLogic
{
    /// <summary>
    /// JWT 令牌的 Payload 数据结构，用于存储登录后的连接信息。
    /// </summary>
    public sealed class Payload
    {
        /// <summary>
        /// 用户唯一标识 ID
        /// </summary>
        public long uid { get; set; }

        /// <summary>
        /// 目标服务器地址
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// 目标服务器端口号
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// 分配的 Scene ID
        /// </summary>
        public uint sceneId { get; set; }

        /// <summary>
        /// JWT 令牌过期时间（Unix 时间戳）
        /// </summary>
        public int exp { get; set; }

        /// <summary>
        /// JWT 令牌签发者（Issuer）
        /// </summary>
        public string iss { get; set; }

        /// <summary>
        /// JWT 令牌接收者（Audience）
        /// </summary>
        public string aud { get; set; }
    }
}