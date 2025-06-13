using UnityEngine;

[CreateAssetMenu(menuName = "Arena/ArenaConfigAsset")]
public class ArenaConfigAsset : ScriptableObject
{
    public bool EnableLogging = true;
    public bool TrackAllocations = true;
    public bool TrackAlignmentLoss = true;
    public KeyCode RunBenchmarkKey = KeyCode.Space;
    public KeyCode BenchmarkExportKey = KeyCode.B;
    public string LoggingPath = "";

    private void OnValidate()
    {
        ArenaConfig.EnableLogging = EnableLogging;
        ArenaConfig.TrackAllocations = TrackAllocations;
        ArenaConfig.TrackAlignmentLoss = TrackAlignmentLoss;
        ArenaConfig.RunBenchmarkKey = RunBenchmarkKey;
        ArenaConfig.BenchmarkExportKey = BenchmarkExportKey;
        if (!string.IsNullOrEmpty(LoggingPath))
        {
            ArenaConfig.LoggingPath = LoggingPath;
        }
    }
}
