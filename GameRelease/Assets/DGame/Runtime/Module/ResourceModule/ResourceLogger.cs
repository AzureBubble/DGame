namespace DGame
{
    public class ResourceLogger : YooAsset.ILogger
    {
        public void Log(string message)
        {
            Debugger.Info(message);
        }

        public void Warning(string message)
        {
            Debugger.Warning(message);
        }

        public void Error(string message)
        {
            Debugger.Error(message);
        }

        public void Exception(System.Exception exception)
        {
            Debugger.Fatal(exception.Message);
        }
    }
}