using System;
using System.IO;
using DGame;
using Fantasy;
using Fantasy.Async;
using Fantasy.Network;
using Fantasy.Network.Interface;

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
        private readonly NetworkProtocolType ProtocolType = NetworkProtocolType.KCP;
        public GameClientStatus Status { get; set; } = GameClientStatus.StatusInit;

        private string m_lastAddress = string.Empty;
        private float m_lastLogDisconnectErrTime = 0f;

        public Scene Scene { get; private set; }

        public async FTask InitAsync()
        {
            await Fantasy.Platform.Unity.Entry.Initialize();
            Scene = await Fantasy.Platform.Unity.Entry.CreateScene();
            DLogger.Info("Fantasy 初始化完成!");
        }

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
            if (Scene.Session == null || Scene.Session.IsDisposed)
            {
                Scene.Connect(address, ProtocolType, OnConnectComplete, OnConnectFail, OnConnectDisconnect, false);
            }
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

        public bool IsStatusCanSendMsg(uint protocolCode)
        {
            bool canSend = false;

            if (Status == GameClientStatus.StatusLogin)
            {
                canSend = protocolCode == OuterOpcode.A2C_LoginResponse;
            }
            if (Status == GameClientStatus.StatusRegister)
            {
                canSend = protocolCode == OuterOpcode.C2A_RegisterRequest;
            }
            if (Status == GameClientStatus.StatusEnter)
            {
                canSend = true;
            }

            if (!canSend)
            {
                float nowTime = GameTime.UnscaledTime;
                if (m_lastLogDisconnectErrTime + 5f > nowTime)
                {
                    DLogger.Error($"[GameClient] GameClient disconnect, send msg failed, protocolCode[{protocolCode}]");
                    m_lastLogDisconnectErrTime = nowTime;
                }
            }

            return canSend;
        }

        public void Send<T>(T message, uint rpcID = 0, long routeID = 0) where T : IMessage
        {
            if (Scene.Session == null)
            {
                Log.Error("Send Message Failed Because Session Is Null");
                return;
            }

            if (IsStatusCanSendMsg(rpcID))
            {
                Scene.Session.Send(message, rpcID, routeID);
            }
        }

        public async FTask<IResponse> Call<T>(T request, long routeId = 0) where T : IRequest
        {
            if (Scene == null || Scene.Session == null || Scene.Session.IsDisposed)
            {
                return null;
            }

            if (IsStatusCanSendMsg(request.OpCode()))
            {
                var requestCallback = await Scene.Session.Call(request, routeId);
                return requestCallback;
            }

            return null;
        }

        protected override void OnDestroy()
        {
            Scene?.Dispose();
        }

        public void RegisterMsgHandler(uint protocolCode, Action<IResponse> ctx)
        {
            if (Scene == null || Scene.Session == null || Scene.Session.IsDisposed)
            {
                return;
            }
            Scene.GetComponent<MessageDispatcherComponent>()?.RegisterMsgHandler(protocolCode, ctx);
        }

        public void UnRegisterMsgHandler(uint protocolCode, Action<IResponse> ctx)
        {
            if (Scene == null || Scene.Session == null || Scene.Session.IsDisposed)
            {
                return;
            }
            Scene.GetComponent<MessageDispatcherComponent>()?.UnRegisterMsgHandler(protocolCode, ctx);
        }
    }
}