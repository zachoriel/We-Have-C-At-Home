using System.Collections;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Assertions;

public class ArenaAllocatorTest : MonoBehaviour
{
    private ArenaAllocator memoryArena1;
    private ArenaAllocator memoryArena2;

    private struct TestStruct
    {
        public int a;
        public float b;
    }

    [SerializeField] private float timeBetweenTestsInSeconds = 2f;

    private IEnumerator Start()
    {
        ArenaLog.ClearOutputLog();

        try
        {
            int arenaSize = 256;
            memoryArena1 = new ArenaAllocator(1, arenaSize, Allocator.Persistent);

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 1: Valid Allocation Test");
            TestValidAllocation(ref memoryArena1, true);
            PrintTestMessage("End Test 1");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 2: Invalid Alignment Test");
            TestInvalidAlignment(ref memoryArena1);
            PrintTestMessage("End Test 2");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 3: Out of Memory Test");
            TestOutOfMemory(ref memoryArena1);
            PrintTestMessage("End Test 3");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 4: Reset Test");
            memoryArena1.Reset();
            PrintTestMessage("End Test 4");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 5: Reallocation After Reset Test");
            TestValidAllocation(ref memoryArena1, true, "First Test Tag");
            TestValidAllocation(ref memoryArena1, false, "Second Test Tag");
            TestValidAllocation(ref memoryArena1, true);
            PrintTestMessage("End Test 5");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            PrintTestMessage("Begin Test 6: Second Arena");
            memoryArena2 = new ArenaAllocator(2, arenaSize, Allocator.Persistent);
            TestValidAllocation(ref memoryArena2, true);
            TestValidAllocation(ref memoryArena2, false);
            TestValidAllocation(ref memoryArena2, true);
            PrintTestMessage("End Test 6");

            yield return new WaitForSeconds(timeBetweenTestsInSeconds);
            ArenaMonitor.PrintSummary();
        }
        finally
        {
            memoryArena1.Dispose();
            memoryArena2.Dispose();
        }

        yield return null;
    }

    private void OnDisable()
    {
        memoryArena1.Dispose();
        memoryArena2.Dispose();
    }

    private unsafe void TestValidAllocation(ref ArenaAllocator arena, bool smartAlloc, string tag = "")
    {
        void* ptr;
        if (smartAlloc)
        {
            ptr = arena.SmartAllocate<TestStruct>(tag);
        }
        else
        {
            // Manually set a larger alignment than needed to test over-alignment tracking.
            int size = UnsafeUtility.SizeOf<TestStruct>();
            ptr = arena.Allocate(size, 32, tag);
        }
        Assert.IsTrue(ptr != null, "Expected valid allocation.");

        TestStruct* s = (TestStruct*)ptr;
        s->a = 123;
        s->b = 456.789f;
        Debug.Log($"Allocated TestStruct at: 0x{(ulong)ptr:X}, a={s->a}, b={s->b}");
    }

    private unsafe void TestInvalidAlignment(ref ArenaAllocator arena)
    {
        void* ptr = arena.Allocate(32, 10); // 10 is not a power of 2.

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

    private unsafe void TestOutOfMemory(ref ArenaAllocator arena)
    {
        // Allocate way more than we have available.
        void* ptr = arena.Allocate(9999, 16);
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
