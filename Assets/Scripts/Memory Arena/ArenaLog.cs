using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.TestTools;
using System.Text.RegularExpressions;
#endif

public static class ArenaLog
{
    public enum Level { Info, Warning, Error, Success }

    public static bool EnableLogging => ArenaConfig.EnableLogging;

    public static List<string> OutputLog = new List<string>();

    public static void Log(object source, string message, Level level = Level.Info)
    {
        if (!EnableLogging) { return; }

        string color = level switch
        {
            Level.Info => "FFFFFF",
            Level.Warning => "FFFF00",
            Level.Error => "FF0000",
            Level.Success => "00FF00",
            _ => "FFFFFF"
        };

        string prefix = source switch
        {
            string s => $"[{s}] ",
            _ => $"[{source.GetType().ToString()}] "
        };

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
            case Level.Success:
                Debug.Log(formatted);
                break;
        }

        OutputLog.Add($"{level}: {formatted}");
    }

    public static void ClearOutputLog()
    {
        OutputLog.Clear();
        Debug.Log("<color=#00FFFF>OutputLog cleared.</color>");
    }

    public static void SaveOutputLog()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"ArenaLog_{timestamp}.txt";
        string path = Path.Combine(ArenaConfig.LoggingPath, filename);
        try
        {
            File.WriteAllLines(path, OutputLog);
            Debug.Log($"<color=#00FFFF>OutputLog saved to: {path}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=#FF0000>Failed to save OutputLog: {ex.Message}</color>");
        }
    }

#if UNITY_EDITOR
    public static void ExpectLog(LogType logType, string substring = "")
    {
        if (!ArenaConfig.EnableLogging) { return; }
        LogAssert.Expect(logType, new Regex(substring));
    }

    public static void ExpectAnyLog((LogType, string)[] expectations)
    {
        if (!EnableLogging) { return; }

        foreach (var (type, pattern) in expectations)
        {
            LogAssert.Expect(type, new Regex(pattern));
        }
    }
#endif
}
