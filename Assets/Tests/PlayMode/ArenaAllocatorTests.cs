using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class ArenaAllocatorTests
{
    private Dictionary<int, ArenaAllocator> liveArenas;
    private System.Random rng;
    private const int ArenaSize = 256;

    private struct TestStruct
    {
        public int a;
        public float b;
    }

    [SetUp]
    public void SetUp()
    {
        rng = new System.Random();
        liveArenas = new Dictionary<int, ArenaAllocator>();

        int arenaCount = rng.Next(2, 5); // 2 to 4 arenas.
        for (int i = 0; i < arenaCount; i++)
        {
            var arena = new ArenaAllocator(i, ArenaSize, Allocator.Temp);
            liveArenas[i] = arena;
        }
    }

    [TearDown]
    public void TearDown()
    {
        ArenaMonitor.PrintSummary(liveArenas);
        ArenaLog.SaveOutputLog();

        foreach (var arena in liveArenas.Values)
        {
            arena.Dispose();
        }
    }

    [Test]
    public unsafe void ValidSmartAllocation_Works()
    {
        foreach (var id in liveArenas.Keys.ToArray())
        {
            var arena = liveArenas[id];

            void* ptr = arena.SmartAllocate<TestStruct>("SmartTest");
            Assert.IsTrue(ptr != null);

            TestStruct* s = (TestStruct*)ptr;
            s->a = 42;
            s->b = 3.14f;
            Assert.AreEqual(42, s->a);
            Assert.AreEqual(3.14f, s->b);

            liveArenas[id] = arena;
        }
    }

    [Test]
    public unsafe void ManualAlignmentAllocation_TracksPadding()
    {
        foreach (var id in liveArenas.Keys.ToArray())
        {
            var arena = liveArenas[id];

            // Initial allocation to make offset > 0.
            arena.Allocate(8, 8, "Pre-misalignment");

            // Perform the real test allocation (which should have padding since offset > 0).
            int size = UnsafeUtility.SizeOf<TestStruct>();
            void* ptr = arena.Allocate(size, 32, "ManualAlignment");
            Assert.IsTrue(ptr != null);

            // Over-alignment should be tracked if enabled
            if (ArenaConfig.TrackAlignmentLoss)
            {
                Assert.IsTrue(arena.GetOverAlignment() > 0, $"Expected padding but got {arena.GetOverAlignment()}.");
            }

            liveArenas[id] = arena;
        }
    }

    [Test]
    public unsafe void InvalidAlignment_IsRejected()
    {
        foreach (var pair in liveArenas)
        {
            ArenaLog.ExpectAnyLog(new[]
            {
                (LogType.Assert, "power of two"),
                (LogType.Warning, "non-fatal allocation")
            });

            void* ptr = pair.Value.Allocate(64, 10); // Not power of two.
            Assert.IsTrue(ptr == null);
        }
    }

    [Test]
    public unsafe void OutOfMemory_IsHandled()
    {
        foreach (var pair in liveArenas)
        {
            ArenaLog.ExpectLog(LogType.Error, "Out of memory");
            void* ptr = pair.Value.Allocate(9999, 16);
            Assert.IsTrue(ptr == null);
        }
    }

    [Test]
    public unsafe void Reset_ClearsState()
    {
        foreach (var id in liveArenas.Keys.ToArray())
        {
            var arena = liveArenas[id];

            // Allocate some data for us to reset.
            void* ptr = arena.SmartAllocate<TestStruct>("SmartTest");
            Assert.IsTrue(ptr != null);

            TestStruct* s = (TestStruct*)ptr;
            s->a = 42;
            s->b = 3.14f;
            Assert.AreEqual(42, s->a);
            Assert.AreEqual(3.14f, s->b);

            liveArenas[id] = arena;

            // Reset the data.
            arena.Reset();
            Assert.AreEqual(0, arena.GetOffset());
            Assert.AreEqual(0, arena.GetOverAlignment());
            Assert.AreEqual(0, ArenaMonitor.GetArenaRecords(arena.GetID()).Length);

            liveArenas[id] = arena;
        }
    }

    [Test]
    public void Dispose_ClearsMemory()
    {
        foreach (var id in liveArenas.Keys.ToArray())
        {
            var arena = liveArenas[id];

            arena.Dispose();
            Assert.IsFalse(arena.IsCreated);
        }
    }

    [Test]
    public void Constructor_InvalidAlignment_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            var badArena = new ArenaAllocator(99, 128, Allocator.Temp, 10);
        });
    }
}