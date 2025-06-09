using UnityEngine;
using System.Collections.Generic;
using System;

public static class ArenaLog
{
    public enum Level { Info, Warning, Error }

    public static bool EnableLogging = true;

    public static List<string> OutputLog = new List<string>();

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
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        try
        {
            System.IO.File.WriteAllLines(path, OutputLog);
            Debug.Log($"<color=#00FFFF>OutputLog saved to: {path}</color>");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"<color=#FF0000>Failed to save OutputLog: {ex.Message}</color>");
        }
    }
}
