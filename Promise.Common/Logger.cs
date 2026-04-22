using NLog;

namespace Promise.Common;

public static class MainLogger
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static void Log(string info)
    {
        Logger.Info(info);
    }

    public static void LogError(string error)
    {
        Logger.Error(error);
    }
}
