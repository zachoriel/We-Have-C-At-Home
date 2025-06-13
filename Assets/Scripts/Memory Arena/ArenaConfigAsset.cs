using UnityEngine;

[CreateAssetMenu(menuName = "Arena/ArenaConfigAsset")]
public class ArenaConfigAsset : ScriptableObject
{
    public bool EnableLogging = true;
    public bool TrackAllocations = true;
    public bool TrackAlignmentLoss = true;
    public KeyCode benchmarkExportKey = KeyCode.B;
    public string LoggingPath = "";

    private void OnValidate()
    {
        ArenaConfig.EnableLogging = EnableLogging;
        ArenaConfig.TrackAllocations = TrackAllocations;
        ArenaConfig.TrackAlignmentLoss = TrackAlignmentLoss;
        ArenaConfig.benchmarkExportKey = benchmarkExportKey;
        if (!string.IsNullOrEmpty(LoggingPath))
        {
            ArenaConfig.LoggingPath = LoggingPath;
        }
    }
}
