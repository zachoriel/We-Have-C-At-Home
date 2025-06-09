using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class ArenaAllocatorTests
{
    private ArenaAllocator arena;
    private const int ArenaID = 1;
    private const int ArenaSize = 256;

    private struct TestStruct
    {
        public int a;
        public float b;
    }

    [SetUp]
    public void SetUp()
    {
        arena = new ArenaAllocator(ArenaID, ArenaSize, Allocator.Temp);
    }

    [TearDown]
    public void TearDown()
    {
        arena.Dispose();
    }

    [Test]
    public unsafe void ValidSmartAllocation_Works()
    {
        void* ptr = arena.SmartAllocate<TestStruct>("SmartTest");
        Assert.IsTrue(ptr != null);

        TestStruct* s = (TestStruct*)ptr;
        s->a = 42;
        s->b = 3.14f;
        Assert.AreEqual(42, s->a);
        Assert.AreEqual(3.14f, s->b);
    }

    [Test]
    public unsafe void ManualAlignmentAllocation_TracksPadding()
    {
        int size = UnsafeUtility.SizeOf<TestStruct>();
        void* ptr = arena.Allocate(size, 32, "ManualAlignment");
        Assert.IsTrue(ptr != null);

        // Over-alignment should be tracked if enabled
        if (ArenaConfig.TrackAlignmentLoss)
        {
            Assert.IsTrue(arena.GetOverAlignment() >= 0);
        }
    }

    [Test]
    public unsafe void Constructor_InvalidAlignment_Throws()
    {
        Assert.Throws<System.ArgumentException>(() =>
        {
            var invalidArena = new ArenaAllocator(99, 128, Allocator.Temp, 10); // 10 is not power of 2
        });
    }


    [Test]
    public unsafe void Allocate_InvalidAlignment_LogsAssertAndWarning()
    {
        ArenaLog.ExpectAnyLog(new[]
        {
            (LogType.Assert, "power of two"),
            (LogType.Warning, "non-fatal allocation")
        });

        void* ptr = arena.Allocate(64, 10); // Not power of two.
        Assert.IsTrue(ptr == null);
    }

    [Test]
    public unsafe void OutOfMemory_IsHandled()
    {
        ArenaLog.ExpectLog(LogType.Error, "Out of memory");

        void* ptr = arena.Allocate(9999, 16);
        Assert.IsTrue(ptr == null);
    }

    [Test]
    public void Reset_ClearsState()
    {
        arena.Reset();
        Assert.AreEqual(0, arena.GetOffset());
        Assert.AreEqual(0, arena.GetOverAlignment());
        Assert.AreEqual(0, ArenaMonitor.GetArenaRecords(arena.GetID()).Length);
    }

    [Test]
    public void Dispose_ClearsMemory()
    {
        arena.Dispose();
        Assert.IsFalse(arena.IsCreated);
    }
}