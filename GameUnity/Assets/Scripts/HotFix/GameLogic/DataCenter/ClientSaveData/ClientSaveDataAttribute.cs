namespace GameLogic
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class ClientSaveDataAttribute : System.Attribute
    {
        public string SaveKey { get; private set; }
        public bool PerRoleID { get; private set; }

        public ClientSaveDataAttribute(string saveKey, bool perRoleID = false)
        {
            SaveKey = saveKey;
            PerRoleID = perRoleID;
        }
    }
}