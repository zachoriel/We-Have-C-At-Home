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

    [Test]
    public void ArenaList_Add_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 8, "IntList");

        list.Add(42);
        list.Add(99);

        Assert.AreEqual(2, list.Length);
        Assert.AreEqual(42, list[0]);
        Assert.AreEqual(99, list[1]);
    }

    [Test]
    public void ArenaList_Add_BeyondCapacity()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 1, "IntList");

        list.Add(25);

        bool threw = false;
        try
        {
            list.Add(26); // Beyond capacity.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_AddMultiple_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 8, "IntList");

        var source = new int[] { 4, 5, 6 };
        list.AddMultiple(source);

        Assert.AreEqual(3, list.Length);
        Assert.AreEqual(4, list[0]);
        Assert.AreEqual(5, list[1]);
        Assert.AreEqual(6, list[2]);
    }

    [Test]
    public void ArenaList_AddMultiple_BeyondCapacity()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 1, "IntList");

        list.Add(25);

        bool threw = false;
        try
        {
            var source = new int[] { 4, 5, 6 };
            list.AddMultiple(source); // Beyond capacity.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_Indexer_IndexOutOfBounds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 8, "IntList");

        list.Add(12);

        bool threw = false;
        try
        {
            list[1] = 50; // Out of bounds.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_Clear_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 5, "IntList");

        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);

        Assert.AreEqual(5, list.Length);

        list.Clear();

        Assert.AreEqual(0, list.Length);
    }

    [Test]
    public void ArenaList_RemoveAt_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 5, "IntList");

        list.Add(1);
        list.Add(2);
        list.Add(3);
        list.Add(4);
        list.Add(5);

        Assert.AreEqual(5, list.Length);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(3, list[2]);
        Assert.AreEqual(4, list[3]);
        Assert.AreEqual(5, list[4]);

        list.RemoveAt(2);

        Assert.AreEqual(4, list.Length);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(4, list[2]);
        Assert.AreEqual(5, list[3]);

        list.RemoveAt();

        Assert.AreEqual(3, list.Length);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(4, list[2]);
    }

    [Test]
    public void ArenaList_RemoveAt_EmptyList()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 5, "IntList");

        bool threw = false;
        try
        {
            list.RemoveAt(); // List is already empty.
        }
        catch (InvalidOperationException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected InvalidOperationException to be thrown.");
        Assert.AreEqual(0, list.Length, "Length should be 0.");
    }

    [Test]
    public void ArenaList_RemoveAt_IndexOutOfBounds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 3, "IntList");

        list.Add(12);

        bool threw = false;
        try
        {
            list.RemoveAt(2); // Out of bounds.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_InsertAt_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 8, "IntList");

        list.Add(1);
        list.Add(2);
        list.Add(4);
        list.Add(5);
        list.Add(6);

        Assert.AreEqual(5, list.Length);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(4, list[2]);
        Assert.AreEqual(5, list[3]);
        Assert.AreEqual(6, list[4]);

        list.InsertAt(2, 3);

        Assert.AreEqual(6, list.Length);
        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(3, list[2]);
        Assert.AreEqual(4, list[3]);
        Assert.AreEqual(5, list[4]);
        Assert.AreEqual(6, list[5]);
    }

    [Test]
    public void ArenaList_InsertAt_IndexOutOfBounds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 8, "IntList");

        list.Add(12);

        bool threw = false;
        try
        {
            list.InsertAt(3, 50); // Out of bounds.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_InsertAt_BeyondCapacity()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 1, "IntList");

        list.Add(25);

        bool threw = false;
        try
        {
            list.InsertAt(5, 12); // Beyond capacity.
        }
        catch (IndexOutOfRangeException) // Expected.
        {
            threw = true;
        }

        Assert.IsTrue(threw, "Expected IndexOutOfRangeException to be thrown.");
        Assert.AreEqual(1, list.Length, "Length should remain unchanged.");
    }

    [Test]
    public void ArenaList_ToArray_Succeeds()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 3, "IntList");

        list.Add(1);
        list.Add(2);
        list.Add(3);

        int[] testArray = list.ToArray();

        Assert.AreEqual(list.Length, testArray.Length);
        Assert.AreEqual(1, testArray[0]);
        Assert.AreEqual(2, testArray[1]);
        Assert.AreEqual(3, testArray[2]);
    }

    [Test]
    public void ArenaList_ToArray_Empty()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 3, "IntList");

        ArenaLog.ExpectLog(LogType.Warning, "length is 0");
        int[] testArray = list.ToArray();
        Assert.AreEqual(0, testArray.Length);
    }

    [Test]
    public void ArenaList_Enumerator_IteratesCorrectly()
    {
        var arena = liveArenas[0];
        var list = new ArenaList<int>(ref arena, 3, "IntList");

        list.Add(5);
        list.Add(15);
        list.Add(25);

        var collected = new List<int>();

        foreach (var value in list)
        {
            collected.Add(value);
        }

        Assert.AreEqual(3, collected.Count);
        Assert.AreEqual(5, collected[0]);
        Assert.AreEqual(15, collected[1]);
        Assert.AreEqual(25, collected[2]);

        collected.Clear();

        for (int i = 0; i < list.Length; i++)
        {
            collected.Add(list[i]);
        }

        Assert.AreEqual(3, collected.Count);
        Assert.AreEqual(5, collected[0]);
        Assert.AreEqual(15, collected[1]);
        Assert.AreEqual(25, collected[2]);
    }
}