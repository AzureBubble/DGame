namespace GameLogic
{
    public class LoginMgr : Singleton<LoginMgr>
    {
        private LoginSaveData m_loginSaveData;

        /// <summary>
        /// 是否第一次进入游戏打开用户隐私界面
        /// </summary>
        public bool IsFirstOpenUserPrivacy
        {
            get
            {
                return m_loginSaveData.IsFirstOpenUserPrivacy > 0;
            }
        }
        
        /// <summary>
        /// 是否设置同意隐私协议
        /// </summary>
        public bool IsAgreeUserPrivacy
        {
            get
            {
                return m_loginSaveData.SetAgreeUserPrivacy > 0;
            }
        }
        
        protected override void OnInit()
        {
            m_loginSaveData = ClientSaveDataMgr.Instance.GetSaveData<LoginSaveData>();
        }
        
        /// <summary>
        /// 设置第一次登录显示用户协议
        /// </summary>
        /// <param name="state"></param>
        public void SetFirstOpenUserPrivacy(bool state)
        {
            m_loginSaveData.IsFirstOpenUserPrivacy = state ? 1 : 0;
            m_loginSaveData.Save();
        }
        
        /// <summary>
        /// 设置同意用户隐私协议
        /// </summary>
        /// <param name="state"></param>
        public void SetAgreeUserPrivacy(bool state)
        {
            m_loginSaveData.SetAgreeUserPrivacy = state ? 1 : 0;
            m_loginSaveData.Save();
        }
    }
}