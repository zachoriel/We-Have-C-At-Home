using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public unsafe struct ArenaAllocator : IDisposable
{
    private byte* basePtr;
    private int capacity;
    private int offset;
    private Allocator allocator;

    public bool IsCreated => basePtr != null;

    public ArenaAllocator(int capacityInBytes, Allocator allocator, int arenaAlignment = 64)
    {
        ArenaUtil.ValidatePowerOfTwo(arenaAlignment, "ArenaAllocator constructor", shouldThrow: true);

        capacity = capacityInBytes;
        offset = 0;
        this.allocator = allocator;
        basePtr = (byte*)UnsafeUtility.Malloc(capacity, arenaAlignment, allocator);
        ArenaLog.Log($"Allocated {capacity} bytes.");
    }

    public void* Allocate(int sizeInBytes, int alignment = 16)
    {
        if (!ArenaUtil.ValidatePowerOfTwo(alignment, "Allocate()", shouldThrow: false))
        {
            return null;
        }

        long alignedOffset = (offset + alignment - 1) & ~(alignment - 1);
        long remaining = capacity - alignedOffset;
        if (alignedOffset + sizeInBytes > capacity)
        {
            ArenaLog.Log($"Out of memory! Requested {sizeInBytes} bytes at aligned offset {alignedOffset}, but only {remaining} bytes remain.", ArenaLog.Level.Error);
            return null;
        }

        void* ptr = basePtr + alignedOffset;
        offset = (int)(alignedOffset + sizeInBytes);

        ArenaLog.Log($"Allocated {sizeInBytes} bytes at offset {alignedOffset}. (Next offset: {offset}).");

        return ptr;
    }

    public void Reset()
    {
        ArenaLog.Log("Resetting offset to 0.");
        offset = 0;
    }

    public void Dispose()
    {
        if (basePtr != null)
        {
            UnsafeUtility.Free(basePtr, allocator);
            basePtr = null;
            ArenaLog.Log("Freed arena memory.");
        }
    }
}