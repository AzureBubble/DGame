namespace GameLogic
{
    public static class ClientSaveDataHelper
    {
        public static int GetSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType)
            => saveData.SettingParams[(int)saveType];

        public static void SaveSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType,
            int value)
        {
            saveData?.SetSystemSettingVal(saveType, value);
            saveData?.Save();
        }

        public static void SetSystemSettingVal(this SystemSaveData saveData, SystemSaveData.SaveType saveType,
            int value)
            => saveData.SettingParams[(int)saveType] = value;
    }
}