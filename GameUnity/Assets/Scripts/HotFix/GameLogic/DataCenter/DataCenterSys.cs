using System.Collections.Generic;
using DGame;
using Fantasy;
using Fantasy.Async;

namespace GameLogic
{
    /// <summary>
    /// 数据中心模块
    /// </summary>
    public class DataCenterSys : Singleton<DataCenterSys>, IUpdate
    {
        private readonly List<IDataCenterModule> m_dataCenterModuleList = new List<IDataCenterModule>();

        protected override void OnInit()
        {
            RegCmdHandle();
            InitModule();
            InitOtherModule();
        }

        private void RegCmdHandle()
        {

        }

        #region 网络操作

        public async FTask Register(string address, int port, string userName, string password)
        {
            GameClient.Instance.Connect(address, port);
            GameClient.Instance.Status = GameClientStatus.StatusRegister;
            var response = (A2C_RegisterResponse)await GameClient.Instance.Call(new C2A_RegisterRequest()
            {
                UserName = userName,
                Password = password
            });
            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                DLogger.Warning($"Error: {response.ErrorCode}");
                return;
            }
            DLogger.Info("Registered Successfully");
            GameEvent.Get<ILoginUI>().OnRegister();
        }

        public async FTask Login(string address, int port, string userName, string password)
        {
            GameClient.Instance.Connect(address, port);
            GameClient.Instance.Status = GameClientStatus.StatusLogin;
            var response = (A2C_LoginResponse)await GameClient.Instance.Call(new C2A_LoginRequest()
            {
                UserName = userName,
                Password = password,
                LoginType = 1
            });

            if (response.ErrorCode != 0)
            {
                UIModule.Instance.ShowTipsUI(response.ErrorCode);
                DLogger.Warning($"Error: {response.ErrorCode}");
                return;
            }
            DLogger.Info($"Login Successfully Token: {response.Token}");
            JwtParseHelper.Parse(response.Token);
            GameEvent.Get<ILoginUI>().OnLogin();
        }

        #endregion

        #region Module相关

        private void InitOtherModule()
        {
        }

        private void InitModule()
        {
        }

        public void RegisterModule(IDataCenterModule module)
        {
            if (m_dataCenterModuleList.Contains(module))
            {
                return;
            }

            module.OnInit();
            m_dataCenterModuleList.Add(module);
        }

        #endregion

        public void OnUpdate()
        {
            foreach (var module in m_dataCenterModuleList)
            {
                module.OnUpdate();
            }
        }

        #region PlayerData相关

        /// <summary>
        /// 当前玩家数据
        /// </summary>
        public PlayerData CurPlayerData { get; private set; }

        /// <summary>
        /// 当前玩家RoleID
        /// </summary>
        public ulong CurRoleID => CurPlayerData != null ? CurPlayerData.RoleID : 0;

        public bool TryGetCurPlayerData(out PlayerData playerData)
        {
            playerData = CurPlayerData;
            return playerData != null;
        }

        public bool TryGetCurRoleID(out ulong roleID)
        {
            roleID = CurRoleID;
            return roleID > 0;
        }

        public bool CheckIsSelfRoleID(ulong roleID) => roleID == CurRoleID;

        #endregion

        public void ClearClientData()
        {
            if (CurPlayerData != null)
            {
                UIModule.Instance.CloseAllWindows();
                for (int i = 0; i < m_dataCenterModuleList.Count; i++)
                {
                    m_dataCenterModuleList[i].OnRoleLogout();
                }
            }
        }
    }
}