using UnityEngine;

public static class ArenaConfig
{
    /// <summary>
    /// Enable or disable ArenaLog debug output globally.
    /// </summary>
    public static bool EnableLogging = true;

    /// <summary>
    /// Enable or disable tracking of allocation metadata.
    /// </summary>
    public static bool TrackAllocations = true;

    /// <summary>
    /// Enable or disable tracking of over-alignment bytes.
    /// </summary>
    public static bool TrackAlignmentLoss = true;

    /// <summary>
    /// Where to store output logs & benchmark results.
    /// </summary>
    public static string LoggingPath = Application.persistentDataPath;

    private const string ConfigResourcePath = "ArenaConfig/ArenaConfig";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LoadSettingsFromAsset()
    {
        var configAsset = Resources.Load<ArenaConfigAsset>(ConfigResourcePath);
        if (configAsset != null)
        {
            EnableLogging = configAsset.EnableLogging;
            TrackAllocations = configAsset.TrackAllocations;
            TrackAlignmentLoss = configAsset.TrackAlignmentLoss;

            if (!string.IsNullOrEmpty(configAsset.LoggingPath))
            {
                LoggingPath = configAsset.LoggingPath;
            }
        }
        else
        {
            Debug.LogWarning($"ArenaConfigAsset not found at Resources/{ConfigResourcePath}. Using default config.");
        }
    }
}
