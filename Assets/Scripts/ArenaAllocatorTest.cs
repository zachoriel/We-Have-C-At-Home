using System;
using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

public class ArenaAllocatorTest : MonoBehaviour
{
    private ArenaAllocator memoryArena;

    private struct TestStruct
    {
        public int a;
        public float b;
    }

    [SerializeField] private float timeBetweenTestsInSeconds = 2f;

    private IEnumerator Start()
    {
        ArenaLog.ClearOutputLog();

        int arenaSize = 256;
        memoryArena = new ArenaAllocator(arenaSize, Allocator.Persistent);

        yield return new WaitForSeconds(timeBetweenTestsInSeconds);
        PrintTestMessage("Begin Test 1: Valid Allocation Test");
        TestValidAllocation();
        PrintTestMessage("End Test 1");

        yield return new WaitForSeconds(timeBetweenTestsInSeconds);
        PrintTestMessage("Begin Test 2: Invalid Alignment Test");
        TestInvalidAlignment();
        PrintTestMessage("End Test 2");

        yield return new WaitForSeconds(timeBetweenTestsInSeconds);
        PrintTestMessage("Begin Test 3: Out of Memory Test");
        TestOutOfMemory();
        PrintTestMessage("End Test 3");

        yield return new WaitForSeconds(timeBetweenTestsInSeconds);
        PrintTestMessage("Begin Test 4: Reset Test");
        memoryArena.Reset();
        PrintTestMessage("End Test 4");

        yield return new WaitForSeconds(timeBetweenTestsInSeconds);
        PrintTestMessage("Begin Test 4: Reallocation After Reset Test");
        TestValidAllocation();
        PrintTestMessage("End Test 5");

        memoryArena.Dispose();

        yield return null;
    }

    private unsafe void TestValidAllocation()
    {
        int size = UnsafeUtility.SizeOf<TestStruct>();
        void* ptr = memoryArena.Allocate(size);
        Assert.IsTrue(ptr != null, "Expected valid allocation.");
        TestStruct* s = (TestStruct*)ptr;
        s->a = 123;
        s->b = 456.789f;
        Debug.Log($"Allocated TestStruct at: 0x{(ulong)ptr:X}, a={s->a}, b={s->b}");
    }

    private unsafe void TestInvalidAlignment()
    {
        void* ptr = memoryArena.Allocate(32, 10); // 10 is not a power of 2.

        bool isPowerOfTwo = (10 & (10 - 1)) == 0;
        if (!isPowerOfTwo && ptr == null)
        {
            Debug.Log("Alignment test passed: invalid alignment was rejected.");
        }
        else
        {
            Debug.LogWarning("Alignment test failed: invalid alignment was accepted.");
        }
    }

    private unsafe void TestOutOfMemory()
    {
        // Allocate way more than we have available.
        void* ptr = memoryArena.Allocate(9999, 16);
        Assert.IsTrue(ptr == null, "Expected allocation to fail due to OOM.");
    }

    [ContextMenu("Save Output Log")]
    private void SaveOutputLog()
    {
        ArenaLog.SaveOutputLog();
    }

    private void PrintTestMessage(string title)
    {
        Debug.Log($"<color=cyan>----- {title} -----</color>");
    }
}
