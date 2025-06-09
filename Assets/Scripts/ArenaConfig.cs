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
}
