using System;
using System.Collections.Generic;
using System.Reflection;
using DGame;

namespace GameLogic
{
    public class ClientSaveDataMgr : Singleton<ClientSaveDataMgr>
    {
        private readonly Dictionary<string, BaseClientSaveData> m_saveDataDict
            = new Dictionary<string, BaseClientSaveData>();
        private readonly Dictionary<Type, ClientSaveDataAttribute> m_cacheAttributeDict
            = new Dictionary<Type, ClientSaveDataAttribute>();

        public T GetSaveData<T>() where T : BaseClientSaveData, new()
        {
            string key = GetStorageKey<T>();
            if (!m_saveDataDict.TryGetValue(key, out var saveData))
            {
                saveData = new T();
                saveData.Init(key);
                m_saveDataDict[key] = saveData;
            }

            return saveData as T;
        }

        public void SaveAllClientData()
        {
            foreach (var saveData in m_saveDataDict.Values)
            {
                saveData.Save();
            }
        }

        private string GetStorageKey<T>() where T : BaseClientSaveData, new()
        {
            var type = typeof(T);
            if (!m_cacheAttributeDict.TryGetValue(type, out var attr))
            {
                attr = type.GetCustomAttribute<ClientSaveDataAttribute>();
                if (attr == null)
                {
                    throw new DGameException($"未标记 SaveData: {type.Name}");
                }
                m_cacheAttributeDict[type] = attr;
            }

            if (attr.PerRoleID && DataCenterSys.Instance.TryGetCurRoleID(out var roleID))
            {
                return $"saveData_{attr.SaveKey}_{roleID}";
            }

            return $"saveData_{attr.SaveKey}";
        }

        protected override void OnDestroy()
        {
            m_saveDataDict.Clear();
            m_cacheAttributeDict.Clear();
        }
    }
}