using UnityEngine;

[CreateAssetMenu(menuName = "Arena/ArenaConfigAsset")]
public class ArenaConfigAsset : ScriptableObject
{
    public bool EnableLogging = true;
    public bool TrackAllocations = true;
    public bool TrackAlignmentLoss = true;
    public string LoggingPath = "";
}
