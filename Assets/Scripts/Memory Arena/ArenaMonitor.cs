using System.Collections.Generic;

public struct ArenaAllocationRecord
{
    public int ArenaID;
    public int Offset;
    public int Size;
    public int Alignment;
    public int AlignmentPadding;
    public string Tag;
}

public static class ArenaMonitor
{
    private static List<ArenaAllocationRecord> records = new List<ArenaAllocationRecord>();

    public static bool IsTracking => ArenaConfig.TrackAllocations;

    public static void RecordAllocation(ArenaAllocator arena, int offset, int size, int alignment, int alignmentPadding, string tag = "")
    {
        if (!IsTracking) { return; }

        records.Add(new ArenaAllocationRecord
        {
            ArenaID = arena.GetID(),
            Offset = offset,
            Size = size,
            Alignment = alignment,
            AlignmentPadding = alignmentPadding,
            Tag = tag
        });
    }

    public static void ClearArenaRecords(int arenaID)
    {
        records.RemoveAll(r => r.ArenaID == arenaID);
    }

    public static ArenaAllocationRecord[] GetArenaRecords(int arenaID)
    {
        return records.FindAll(r => r.ArenaID == arenaID).ToArray();
    }

    public static void ClearAllRecords()
    {
        records.Clear();
    }

    public static void PrintSummary(Dictionary<int, ArenaAllocator> liveArenas)
    {
        if (!IsTracking)
        {
            ArenaLog.Log("ArenaMonitor", "ArenaMonitor is not tracking records (check ArenaConfig).", ArenaLog.Level.Warning);
            return;
        }
        else if (records.Count == 0)
        {
            ArenaLog.Log("ArenaMonitor", "No allocations recorded.", ArenaLog.Level.Warning);
            return;
        }

        var summary = $"{records.Count} total allocations tracked.\n";
        foreach (var kvp in liveArenas)
        {
            int id = kvp.Key;
            ArenaAllocator arena = liveArenas[id];
            float wasteRatio = arena.GetOverAlignment() / (float)arena.GetCapacity();

            summary += $"Arena {id} Data\n ------------\n" +
                $"Total bytes lost to over-alignment: {arena.GetOverAlignment()}\n" +
                $"Over-alignment padding accounts for {wasteRatio * 100}% of arena capacity.\n";

            foreach (var record in records)
            {
                if (record.ArenaID == id)
                {
                    summary += $"[Offset: {record.Offset}] Size: {record.Size} bytes | Alignment: {record.Alignment} | Alignment Padding: {record.AlignmentPadding}" +
                    (string.IsNullOrWhiteSpace(record.Tag) ? "" : $" | Tag: {record.Tag}") + "\n";
                }
            }
        }
        ArenaLog.Log("ArenaMonitor", summary.TrimEnd('\n'), ArenaLog.Level.Info);
    }
}
