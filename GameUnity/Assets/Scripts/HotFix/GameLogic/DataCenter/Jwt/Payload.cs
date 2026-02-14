namespace GameLogic
{
    public sealed class Payload
    {
        /// <summary>
        /// UID
        /// </summary>
        public long uid { get; set; }

        /// <summary>
        /// 目标服务器地址
        /// </summary>
        public string address { get; set; }

        /// <summary>
        /// 目标服务器地址端口号
        /// </summary>
        public int port { get; set; }

        /// <summary>
        /// 分配的Scene的Id
        /// </summary>
        public uint sceneId { get; set; }

        /// <summary>
        ///
        /// </summary>
        public int exp { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string iss { get; set; }

        /// <summary>
        ///
        /// </summary>
        public string aud { get; set; }
    }
}