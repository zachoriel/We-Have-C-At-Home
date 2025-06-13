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
    /// The key that runs performance benchmarks.
    /// </summary>
    public static KeyCode RunBenchmarkKey = KeyCode.Space;

    /// <summary>
    /// The key that exports performance benchmark results.
    /// </summary>
    public static KeyCode BenchmarkExportKey = KeyCode.B;

    /// <summary>
    /// Where to store output logs & benchmark results.
    /// </summary>
    public static string LoggingPath = Application.persistentDataPath;
}
