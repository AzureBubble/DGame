using System;
using DGame;

namespace GameLogic
{
    public enum ClientConnectWatcherStatus
    {
        StatusInit,
        StatusReconnectAuto,
        StatusReconnectConfirm,
        StatusWaitExit
    }

    public class ClientConnectWatcher
    {
        private readonly int AUTO_RECONNECT_MAX_COUNT = 3;
        private readonly float GAME_CLIENT_MSG_TIME_OUT = 3f;

        private GameClient m_client;
        private ClientConnectWatcherStatus m_status = ClientConnectWatcherStatus.StatusInit;
        private float m_statusTime;
        private int m_reconnectCount;
        private int m_disconnectReason;
        private bool m_enabled;

        public bool Enabled
        {
            get => m_enabled;
            set
            {
                if (value != m_enabled)
                {
                    m_enabled = value;

                    if (m_enabled)
                    {
                        OnEnable();
                    }
                    else
                    {
                        OnDisable();
                    }
                }
            }
        }

        public ClientConnectWatcherStatus Status
        {
            get => m_status;
            set
            {
                if (value != m_status)
                {
                    m_status = value;
                    m_statusTime = GameTime.UnscaledTime;
                }
            }
        }

        public ClientConnectWatcher(GameClient client)
        {
            m_client = client;
            m_statusTime = GameTime.UnscaledTime;
            m_status = ClientConnectWatcherStatus.StatusInit;
        }

        private void OnEnable()
        {
            Reset();
        }

        private void OnDisable()
        {
            Reset();
        }

        public void Reset()
        {
            Status = ClientConnectWatcherStatus.StatusInit;
            m_reconnectCount = 0;
        }

        public void Update()
        {
            if (!m_enabled || m_client.IsStatusEnter)
            {
                return;
            }

            switch (m_status)
            {
                case ClientConnectWatcherStatus.StatusInit:
                    UpdateOnInitStatus();
                    break;

                case ClientConnectWatcherStatus.StatusReconnectAuto:
                    UpdateOnReconnectAutoStatus();
                    break;

                case ClientConnectWatcherStatus.StatusReconnectConfirm:
                    UpdateOnReconnectConfirmStatus();
                    break;

                case ClientConnectWatcherStatus.StatusWaitExit:
                    UpdateOnWaitExitAutoStatus();
                    break;
            }
        }

        public void Reconnect()
        {
            if (m_status == ClientConnectWatcherStatus.StatusReconnectConfirm)
            {
                Status = ClientConnectWatcherStatus.StatusReconnectAuto;
            }
        }

        private void UpdateOnWaitExitAutoStatus()
        {
        }

        private void UpdateOnReconnectConfirmStatus()
        {
        }

        private void UpdateOnReconnectAutoStatus()
        {
            if (m_client.IsStatusEnter)
            {
                Status = ClientConnectWatcherStatus.StatusInit;
                m_reconnectCount = 0;
                return;
            }
            if (m_statusTime + GAME_CLIENT_MSG_TIME_OUT < GameTime.UnscaledTime)
            {
                DLogger.Error("UpdateOnReconnectAuto timeout: {0}", GAME_CLIENT_MSG_TIME_OUT);
                // 切换状态回默认状态 下一帧继续判断是需要手动重连还是自动重连
                Status = ClientConnectWatcherStatus.StatusInit;
            }
        }

        private void UpdateOnInitStatus()
        {
            if (m_reconnectCount <= AUTO_RECONNECT_MAX_COUNT)
            {
                // 自动重连
                if (m_reconnectCount == 0)
                {
                    m_disconnectReason = m_client.LastNetErrorCode;
                }
                Status = ClientConnectWatcherStatus.StatusReconnectAuto;
                m_reconnectCount++;
                // 检查客户端版本
                CheckClientVersion();
                // 重连
                m_client.Reconnect();
            }
            else
            {
                // 玩家手动确认重连
                Status = ClientConnectWatcherStatus.StatusReconnectConfirm;
                m_reconnectCount++;
            }
        }

        /// <summary>
        /// 检查客户端版本
        /// </summary>
        public static void CheckClientVersion()
        {

        }
    }
}