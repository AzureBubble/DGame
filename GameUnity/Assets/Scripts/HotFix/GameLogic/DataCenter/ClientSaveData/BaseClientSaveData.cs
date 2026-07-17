using System;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using DGame;
using Newtonsoft.Json;
using UnityEngine;

namespace GameLogic
{
    public enum ClientSaveDataStorageMode
    {
        /// <summary>
        /// 使用Unity PlayerPrefs存储，适合少量轻量数据
        /// </summary>
        PlayerPrefs,

        /// <summary>
        /// 使用persistentDataPath下的JSON文件存储，适合体量更大的客户端数据
        /// </summary>
        JsonFile,
    }

    public abstract class BaseClientSaveData
    {
        private const string JSON_FILE_DIRECTORY = "ClientSaveData";

        private string m_saveKey;
        private ClientSaveDataStorageMode m_storageMode;
        // JsonFile模式首次找不到文件时，会尝试从同key的PlayerPrefs读取并迁移。
        private bool m_needMigratePlayerPrefsToJson;

        /// <summary>
        /// JSON存档目录。
        /// </summary>
        public static string JsonSaveDirectoryPath
            => Path.Combine(Application.persistentDataPath, JSON_FILE_DIRECTORY);

        /// <summary>
        /// 当前存档已保存的数据版本。
        /// </summary>
        [JsonProperty]
        public int SaveDataVersion { get; private set; }

        /// <summary>
        /// 当前代码支持的存档版本；子类字段结构变更时递增。
        /// </summary>
        protected virtual int CurrentSaveDataVersion => 1;

        /// <summary>
        /// 初始化保存数据
        /// </summary>
        /// <param name="saveKey">保存数据的键名</param>
        /// <param name="storageMode">保存数据的存储方式</param>
        public void Init(string saveKey, ClientSaveDataStorageMode storageMode)
        {
            m_saveKey = saveKey;
            m_storageMode = storageMode;
            Load();
        }

        /// <summary>
        /// 加载数据
        /// <remarks>子类可重写实现解密</remarks>
        /// </summary>
        protected virtual void Load()
        {
            try
            {
                m_needMigratePlayerPrefsToJson = false;
                string jsonStr = ReadJsonFromStorage();

                if (!string.IsNullOrEmpty(jsonStr))
                {
                    JsonConvert.PopulateObject(jsonStr, this);
                    if (TryUpgradeSaveDataVersion() || m_needMigratePlayerPrefsToJson)
                    {
                        Save();
                    }
                }
                else
                {
                    SaveDataVersion = CurrentSaveDataVersion;
                }
            }
            catch (Exception e)
            {
                LogStorageError("Load", e, GetLogFilePath());
                BackupCorruptJsonFile();
            }
            finally
            {
                m_needMigratePlayerPrefsToJson = false;
            }
        }

        /// <summary>
        /// 保存数据到本地存储
        /// <remarks>子类可重写实现加密</remarks>
        /// </summary>
        public virtual void Save()
        {
            try
            {
                WriteJsonToStorage(JsonConvert.SerializeObject(this, Formatting.None));
            }
            catch (Exception e)
            {
                LogStorageError("Save", e, GetLogFilePath());
            }
        }

        /// <summary>
        /// 异步保存数据到本地存储，JsonFile模式会切到线程池执行文件写入
        /// </summary>
        public virtual async UniTask SaveAsync()
        {
            if (m_storageMode == ClientSaveDataStorageMode.PlayerPrefs)
            {
                Save();
                return;
            }

            string jsonStr;
            string filePath = GetJsonFilePath();
            try
            {
                jsonStr = JsonConvert.SerializeObject(this, Formatting.None);
            }
            catch (Exception e)
            {
                LogStorageError("Serialize", e, filePath);
                return;
            }

            Exception exception = null;
            await UniTask.SwitchToThreadPool();
            try
            {
                WriteJsonFile(filePath, jsonStr);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            if (exception != null)
            {
                LogStorageError("SaveAsync", exception, filePath);
            }
        }

        protected static T Get<T>() where T : BaseClientSaveData, new()
            => ClientSaveDataMgr.Instance.GetSaveData<T>();

        /// <summary>
        /// 存档版本升级入口；子类按oldVersion分段补齐或迁移字段。
        /// </summary>
        /// <param name="oldVersion">旧存档版本，未带版本字段的历史存档为0</param>
        /// <param name="newVersion">当前目标版本</param>
        protected virtual void OnUpgradeData(int oldVersion, int newVersion)
        {
        }

        /// <summary>
        /// 从当前存储后端读取JSON字符串。
        /// JsonFile模式下若文件不存在，会尝试读取旧PlayerPrefs数据用于懒迁移。
        /// </summary>
        protected string ReadJsonFromStorage()
        {
            switch (m_storageMode)
            {
                case ClientSaveDataStorageMode.JsonFile:
                    string filePath = GetJsonFilePath();
                    if (File.Exists(filePath))
                    {
                        return File.ReadAllText(filePath, Encoding.UTF8);
                    }

                    string playerPrefsJson = DGame.Utility.PlayerPrefsUtil.GetString(m_saveKey);
                    m_needMigratePlayerPrefsToJson = !string.IsNullOrEmpty(playerPrefsJson);
                    return playerPrefsJson;
                case ClientSaveDataStorageMode.PlayerPrefs:
                default:
                    return DGame.Utility.PlayerPrefsUtil.GetString(m_saveKey);
            }
        }

        /// <summary>
        /// 将JSON字符串写入当前存储后端
        /// </summary>
        protected void WriteJsonToStorage(string jsonStr)
        {
            switch (m_storageMode)
            {
                case ClientSaveDataStorageMode.JsonFile:
                    string filePath = GetJsonFilePath();
                    WriteJsonFile(filePath, jsonStr);
                    break;
                case ClientSaveDataStorageMode.PlayerPrefs:
                default:
                    DGame.Utility.PlayerPrefsUtil.SetString(m_saveKey, jsonStr);
                    break;
            }
        }

        /// <summary>
        /// 获取当前存档对应的JSON文件路径
        /// </summary>
        protected string GetJsonFilePath()
            => Path.Combine(JsonSaveDirectoryPath, $"{GetSafeFileName(m_saveKey)}.json");

        /// <summary>
        /// 检查并升级存档版本；升级后由调用方保存当前对象。
        /// </summary>
        private bool TryUpgradeSaveDataVersion()
        {
            int currentVersion = CurrentSaveDataVersion;
            if (SaveDataVersion >= currentVersion)
            {
                return false;
            }

            int oldVersion = SaveDataVersion;
            OnUpgradeData(oldVersion, currentVersion);
            SaveDataVersion = currentVersion;
            return true;
        }

        /// <summary>
        /// JsonFile读取或反序列化失败时备份坏档，避免下次启动继续读取同一个坏文件。
        /// </summary>
        private void BackupCorruptJsonFile()
        {
            if (m_storageMode != ClientSaveDataStorageMode.JsonFile)
            {
                return;
            }

            string filePath = GetJsonFilePath();
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                string backupFilePath = $"{filePath}.corrupt";
                if (File.Exists(backupFilePath))
                {
                    backupFilePath = $"{filePath}.{DateTime.Now:yyyyMMddHHmmss}.corrupt";
                }

                File.Move(filePath, backupFilePath);
            }
            catch (Exception e)
            {
                LogStorageError("BackupCorruptJsonFile", e, filePath);
            }
        }

        private string GetLogFilePath()
            => m_storageMode == ClientSaveDataStorageMode.JsonFile ? GetJsonFilePath() : string.Empty;

        private void LogStorageError(string operation, Exception exception, string filePath)
        {
            DLogger.Error(
                $"[ClientSaveData] {operation} failed, saveKey={m_saveKey}, storageMode={m_storageMode}, filePath={filePath}, error={exception}");
        }

        /// <summary>
        /// 写入JSON文件，写入前确保目录存在。
        /// </summary>
        private static void WriteJsonFile(string filePath, string jsonStr)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, jsonStr, Encoding.UTF8);
        }

        private static string GetSafeFileName(string fileName)
        {
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName;
        }
    }
}
