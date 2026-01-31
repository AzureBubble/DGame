using Newtonsoft.Json;

namespace GameLogic
{
    public abstract class BaseClientSaveData
    {
        private string m_saveKey;

        public void Init(string saveKey)
        {
            m_saveKey = saveKey;
            Load();
        }

        private void Load()
        {
            string jsonStr = DGame.Utility.PlayerPrefsUtil.GetString(m_saveKey);

            if (!string.IsNullOrEmpty(jsonStr))
            {
                JsonConvert.PopulateObject(jsonStr, this);
            }
        }

        public void Save()
            => DGame.Utility.PlayerPrefsUtil.SetString(m_saveKey,
                JsonConvert.SerializeObject(this, Formatting.None));

        public static T Get<T>() where T : BaseClientSaveData, new()
            => ClientSaveDataMgr.Instance.GetSaveData<T>();
    }
}