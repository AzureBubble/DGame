namespace GameLogic
{
    [ClientSaveData("SystemSaveData")]
    public class SystemSaveData : BaseClientSaveData
    {
        public enum SaveType
        {
            Max,
        }

        public int[] SettingParams { get; private set; } = new int[(int)SaveType.Max];

        public SystemSaveData()
        {
        }

        public static SystemSaveData Get => BaseClientSaveData.Get<SystemSaveData>();
    }
}