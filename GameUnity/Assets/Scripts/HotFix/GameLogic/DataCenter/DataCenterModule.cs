namespace GameLogic
{
    public interface IDataCenterModule
    {
        void OnInit();

        void OnRoleLogin();

        void OnRoleLogout();

        void OnUpdate();

        void OnMainPlayerMapChange();
    }

    public class DataCenterModule<T> : IDataCenterModule where T : new()
    {
        private static T m_instance;
        public static T Instance => m_instance != null ? m_instance : m_instance = new T();

        public void OnInit() { }

        public void OnRoleLogin() { }

        public void OnRoleLogout() { }

        public void OnUpdate() { }

        public void OnMainPlayerMapChange() { }
    }
}