using System.Collections.Generic;

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