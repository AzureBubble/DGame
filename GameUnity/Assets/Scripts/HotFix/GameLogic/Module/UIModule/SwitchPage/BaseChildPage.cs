namespace GameLogic
{
    public class ChildPageShareData
    {
        private object[] m_shareDatas = new object[3];
        public object ShareData1 => m_shareDatas[0];
        public object ShareData2 => m_shareDatas[1];
        public object ShareData3 => m_shareDatas[2];

        public void SetShareData(int index, object shareData)
        {
            if (index < 0 || index >= m_shareDatas.Length)
            {
                return;
            }
            m_shareDatas[index] = shareData;
        }
    }

    public class BaseChildPage : UIWidget
    {
        protected ChildPageShareData m_shareData;
        protected SwitchPageMgr m_switchPageMgr;

        public void Init(ChildPageShareData shareData, SwitchPageMgr switchPageMgr)
        {
            m_shareData = shareData;
            m_switchPageMgr = switchPageMgr;
        }

        public virtual void OnPageShowed(int oldShowType, int newShowType)
        { }

        public virtual void RefreshCurrentChildPage()
        { }

        public object ShareData1 {get => m_shareData.ShareData1; set => m_shareData.SetShareData(0, value); }
        public object ShareData2 {get => m_shareData.ShareData2; set => m_shareData.SetShareData(1, value); }
        public object ShareData3 {get => m_shareData.ShareData3; set => m_shareData.SetShareData(2, value); }
    }
}