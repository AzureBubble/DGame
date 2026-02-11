using System;
using DGame;

namespace GameLogic
{
    /// <summary>
    /// 网络客户端状态。
    /// </summary>
    public enum GameClientStatus
    {
        /// <summary>
        /// 初始化
        /// </summary>
        StatusInit,

        /// <summary>
        /// 连接成功服务器
        /// </summary>
        StatusConnected,

        /// <summary>
        /// 重新连接
        /// </summary>
        StatusReconnect,

        /// <summary>
        /// 断开连接
        /// </summary>
        StatusClose,

        /// <summary>
        /// 登录中
        /// </summary>
        StatusLogin,

        /// <summary>
        /// 注册
        /// </summary>
        StatusRegister,

        /// <summary>
        /// AccountLogin成功，进入服务器了
        /// </summary>
        StatusEnter,
    }

    public sealed class GameClient : Singleton<GameClient>
    {
        public GameClientStatus Status { get; set; } = GameClientStatus.StatusInit;

        private string m_lastAddress = string.Empty;
        private float m_lastLogDisconnectErrTime = 0f;

        public void Connect(string address, bool reconnect = false)
        {
            if (Status == GameClientStatus.StatusConnected || Status == GameClientStatus.StatusLogin ||
                Status == GameClientStatus.StatusEnter)
            {
                return;
            }

            if (reconnect)
            {

            }

            m_lastAddress = address;
            Status = reconnect ? GameClientStatus.StatusReconnect : GameClientStatus.StatusInit;
        }

        private void OnConnectComplete()
        {
            Status = GameClientStatus.StatusConnected;
            DLogger.Info("[GameClient] Connected to server success");
        }

        private void OnConnectFail()
        {
            Status = GameClientStatus.StatusClose;
            DLogger.Info("[GameClient] Connected to server fail");
        }

        private void OnConnectDisconnect()
        {
            Status = GameClientStatus.StatusClose;
            DLogger.Info("[GameClient] Disconnected to server");
        }

        public bool IsStatusCanSendMsg(int protocolCode)
        {
            bool canSend = false;

            // 自行根据类型处理是否可以发送消息给服务器
            if (Status == GameClientStatus.StatusLogin)
            {
                canSend = true;
            }
            if (Status == GameClientStatus.StatusRegister)
            {
                canSend = true;
            }
            if (Status == GameClientStatus.StatusEnter)
            {
                canSend = true;
            }

            if (!canSend)
            {
                float nowTime = GameTime.UnscaledTime;
                if (m_lastLogDisconnectErrTime + 5f < nowTime)
                {
                    DLogger.Error($"[GameClient] GameClient disconnect, send msg failed, protocolCode[{protocolCode}]");
                    m_lastLogDisconnectErrTime = nowTime;
                }
            }

            return canSend;
        }

        public void Send<T>(T message, uint rpcID = 0, long routeID = 0)
        {
        }

        public void Call<T>(T request, long routeId = 0)
        {
        }

        public void RegisterMsgHandler(uint protocolCode, Action xtc)
        {
        }

        public void UnRegisterMsgHandler(uint protocolCode, Action xtc)
        {
        }
    }
}