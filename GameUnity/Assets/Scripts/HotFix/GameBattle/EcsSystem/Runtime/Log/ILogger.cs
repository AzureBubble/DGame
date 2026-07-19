namespace GameBattle
{
    public interface ILogger
    {
        void Log(object message);
        void Info(object message);
        void Warning(object message);
        void Error(object message);
        void Exception(System.Exception exception);
        void SetLogLevel(bool debug, bool info, bool warning, bool error);
    }
}