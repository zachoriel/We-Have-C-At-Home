using UnityEngine;

public static class ArenaLog
{
    public enum Level { Info, Warning, Error }

    public static bool EnableLogging = true;

    public static void Log(string message, Level level = Level.Info)
    {
        if (!EnableLogging) { return; }

        string color = level switch
        {
            Level.Info => "00FF00",
            Level.Warning => "FFFF00",
            Level.Error => "FF0000",
            _ => "FFFFFF"
        };

        string prefix = "[ArenaAllocator] ";
        string formatted = $"<color=#{color}>{prefix}{message}</color>";

        switch (level)
        {
            case Level.Info:
                Debug.Log(formatted);
                break;
            case Level.Warning:
                Debug.LogWarning(formatted);
                break;
            case Level.Error:
                Debug.LogError(formatted);
                break;
        }
    }
}
