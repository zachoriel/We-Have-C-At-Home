using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

public unsafe struct ArenaAllocator : IDisposable
{
    private int id;
    private byte* basePtr;
    private int capacity;
    private int offset;
    private long totalAlignmentPadding;
    private Allocator allocator;

    public bool IsCreated => basePtr != null;
    public int GetID() => id;
    public int GetCapacity() => capacity;
    public int GetOffset() => offset;
    public long GetOverAlignment() => totalAlignmentPadding;

    public ArenaAllocator(int id, int capacityInBytes, Allocator allocator, int arenaAlignment = 64)
    {
        ArenaUtil.ValidatePowerOfTwo(arenaAlignment, "ArenaAllocator constructor", shouldThrow: true);

        this.id = id;
        capacity = capacityInBytes;
        offset = 0;
        totalAlignmentPadding = 0;
        this.allocator = allocator;
        basePtr = (byte*)UnsafeUtility.Malloc(capacity, arenaAlignment, allocator);
        ArenaLog.Log(this, $"Allocated {capacity} bytes.", ArenaLog.Level.Success);
    }

    public void* Allocate(int sizeInBytes, int alignment = 16, string tag = "")
    {
        if (!ArenaUtil.ValidatePowerOfTwo(alignment, "Allocate()", shouldThrow: false))
        {
            return null;
        }

        long offsetBeforeAlignment = offset;
        long alignedOffset = (offset + alignment - 1) & ~(alignment - 1);
        long remaining = capacity - alignedOffset;
        if (alignedOffset + sizeInBytes > capacity)
        {
            ArenaLog.Log(this, $"Out of memory! Requested {sizeInBytes} bytes at aligned offset {alignedOffset}, but only {remaining} bytes remain.",
                ArenaLog.Level.Error);
            return null;
        }

        void* ptr = basePtr + alignedOffset;
        offset = (int)(alignedOffset + sizeInBytes);

        int alignmentPadding = (int)(alignedOffset - offsetBeforeAlignment);

        ArenaLog.Log(this, $"Allocated {sizeInBytes} bytes at offset {alignedOffset}. (Next offset: {offset}).", ArenaLog.Level.Success);

        if (ArenaConfig.TrackAlignmentLoss)
        {
            totalAlignmentPadding += alignmentPadding;
        }
        ArenaMonitor.RecordAllocation(this, (int)alignedOffset, sizeInBytes, alignment, alignmentPadding, tag);

        return ptr;
    }

    public void* SmartAllocate<T>(string tag = "") where T : unmanaged
    {
        int size = UnsafeUtility.SizeOf<T>();
        int alignment = ArenaUtil.GetNextPowerOfTwo(size);
        return Allocate(size, alignment, tag);
    }

    public void Reset()
    {
        ArenaLog.Log(this, "Resetting offset to 0.", ArenaLog.Level.Success);
        offset = 0;
        totalAlignmentPadding = 0;

        ArenaMonitor.ClearArenaRecords(this.GetID());
    }

    public void Dispose()
    {
        if (basePtr != null)
        {
            UnsafeUtility.Free(basePtr, allocator);
            basePtr = null;
            ArenaLog.Log(this, $"Freed {offset} bytes of arena memory.", ArenaLog.Level.Success);
            offset = 0;
            totalAlignmentPadding = 0;
            ArenaMonitor.ClearArenaRecords(this.GetID());
        }
    }
}