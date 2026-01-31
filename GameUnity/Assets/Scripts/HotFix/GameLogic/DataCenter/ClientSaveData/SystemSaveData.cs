namespace GameLogic
{
    [ClientSaveData("SystemSaveData")]
    public class SystemSaveData : BaseClientSaveData
    {
        public enum SaveType
        {
            Max,
        }

        public int[] SettingParams { get; } = new int[(int)SaveType.Max];

        public SystemSaveData()
        {
        }

        public static SystemSaveData Get => BaseClientSaveData.Get<SystemSaveData>();
    }
}