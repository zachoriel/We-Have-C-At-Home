using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public unsafe class NoiseGenerator_Unmanaged : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int width = 1024;
    [SerializeField] private int height = 1024;
    [SerializeField] private Material targetMaterial;

    [Header("Benchmark Settings")]
    [SerializeField] private int allocationsPerCycle = 1;
    [SerializeField] private int framesPerCycle = 5;
    [SerializeField] private int totalCycles = 100;
    [SerializeField] private bool useBurst = true;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Color runningColor = Color.yellow;
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Button[] controlButtons;

    private ArenaAllocator* arena;
    private List<ArenaArray<float>> arenaBuffers = new();
    private Texture2D outputTexture;

    private List<BenchmarkRecord> benchmarkLog = new();
    private int currentCycle = 0;
    private int currentFrameInCycle = 0;
    private bool runningBenchmark = false;
    private bool benchmarkComplete = false;

    public void RunBenchmark(bool burst)
    {
        if (arena == null)
        {
            int arenaSize = width * height * allocationsPerCycle * framesPerCycle * sizeof(float);
            arena = (ArenaAllocator*)UnsafeUtility.Malloc(sizeof(ArenaAllocator), 64, Allocator.Persistent);
            *arena = new ArenaAllocator(0, arenaSize, Allocator.Persistent);
        }

        arena->Reset();
        arenaBuffers.Clear();

        outputTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        outputTexture.filterMode = FilterMode.Point;
        targetMaterial.mainTexture = outputTexture;

        useBurst = burst;
        benchmarkComplete = false;
        runningBenchmark = true;

        currentCycle = 0;
        currentFrameInCycle = 0;

        statusText.text = "Running Benchmark";
        statusText.color = runningColor;
        foreach (var btn in controlButtons)
        {
            btn.interactable = false;
        }
    }

    private void Update()
    {
        if (runningBenchmark && !benchmarkComplete && currentCycle < totalCycles)
        {
            progressText.text = $"Cycle: {currentCycle + 1}/{totalCycles}";

            currentFrameInCycle++;
            int total = width * height;

            int gcBefore = GC.CollectionCount(0);
            float start = Time.realtimeSinceStartup;

            for (int i = 0; i < allocationsPerCycle; i++)
            {
                ArenaArray<float> buffer = new ArenaArray<float>(arena, total, "CycleBuffer");
                if (useBurst)
                {
                    GenerateNoiseBurst(buffer);
                }
                else
                {
                    GenerateNoiseNoBurst(buffer);
                }
                arenaBuffers.Add(buffer);
            }

            float end = Time.realtimeSinceStartup;
            int gcAfter = GC.CollectionCount(0);
            long memoryUsed = GC.GetTotalMemory(false);

            benchmarkLog.Add(new BenchmarkRecord
            {
                label = useBurst ? "Arena + Burst" : "Arena + No Burst",
                cycle = currentCycle,
                frame = currentFrameInCycle,
                ms = (end - start) * 1000f,
                memoryBytes = memoryUsed,
                gcCollections = gcAfter - gcBefore,
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            if (currentFrameInCycle >= framesPerCycle)
            {
                currentCycle++;
                currentFrameInCycle = 0;

                if (arenaBuffers.Count > 0)
                {
                    UpdateTextureFromBuffer(arenaBuffers[^1]);
                }

                arena->Reset();
                arenaBuffers.Clear();
            }
        }

        if (currentCycle >= totalCycles && !benchmarkComplete)
        {
            benchmarkComplete = true;
            runningBenchmark = false;

            ExportBenchmarksToCSV();

            statusText.text = "Benchmark Complete; Results Exported";
            statusText.color = completedColor;
            foreach (var btn in controlButtons)
            {
                btn.interactable = true;
            }

            arena->Reset();
            arenaBuffers.Clear();

            ArenaLog.Log("NoiseGenerator_Unmanaged", "All benchmark cycles complete; automatically exported results to CSV.", ArenaLog.Level.Success);
        }
    }

    private void GenerateNoiseNoBurst(ArenaArray<float> buffer)
    {
        for (int index = 0; index < buffer.Length; index++)
        {
            int x = index % width;
            int y = index / width;

            uint hash = (uint)(math.hash(new int2(x, y)) + (uint)(Time.time * 1000f));
            if (hash == 0) { hash = GuaranteeHashNonZero(hash, x, y); }
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(hash);
            buffer[index] = rand.NextFloat();
        }
    }

    private void GenerateNoiseBurst(ArenaArray<float> buffer)
    {
        var job = new GenerateNoiseJob
        {
            buffer = buffer,
            width = width,
            height = height,
            seed = Time.time
        };

        job.Schedule(buffer.Length, 64).Complete();
    }

    private void UpdateTextureFromBuffer(ArenaArray<float> buffer)
    {
        var pixels = new float[buffer.Length];
        buffer.CopyTo(pixels);

        Color[] colors = new Color[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            float v = pixels[i];
            colors[i] = new Color(v, v, v);
        }

        outputTexture.SetPixels(colors);
        outputTexture.Apply();
    }

    private void ExportBenchmarksToCSV()
    {
        string path = Path.Combine(ArenaConfig.LoggingPath, "ArenaBenchmarks_Cycles.csv");
        bool fileExists = File.Exists(path);

        using StreamWriter writer = new StreamWriter(path, true);
        if (!fileExists)
        {
            writer.WriteLine("Label,Cycle,FrameInCycle,Milliseconds,MemoryBytes,GCCollections,Timestamp");
        }

        foreach (var record in benchmarkLog)
        {
            string line = $"{record.label},{record.cycle},{record.frame},{record.ms:F3},{record.memoryBytes},{record.gcCollections},{record.timestamp}";
            writer.WriteLine(line);
        }

        benchmarkLog.Clear();
        ArenaLog.Log("NoiseGenerator_Unmanaged", $"Benchmark results exported to {path}.", ArenaLog.Level.Success);
    }

    private void OnDestroy()
    {
        if (arena != null)
        {
            arena->Dispose();
            UnsafeUtility.Free(arena, Allocator.Persistent);
            arena = null;
        }
    }

    /// <summary>
    /// In the extremely rare edge case that hash wraps around to 0 (this actually happened in my initial testing),
    /// fallback to a secondary deterministic hash. If the entropy Gods frown upon you and return that as 0 too,
    /// just give hash an easily auditable non-zero constant.
    /// </summary>
    private static uint GuaranteeHashNonZero(uint hash, int x, int y)
    {
        hash = (uint)math.hash(new int2(x + 1337, y + 7331));
        if (hash == 0) { hash = 0xBEEFCAFEu; } // Arbitrary non-zero sentinel.

        return hash;
    }

    [BurstCompile]
    public struct GenerateNoiseJob : IJobParallelFor
    {
        public ArenaArray<float> buffer;
        public int width;
        public int height;
        [ReadOnly] public float seed;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            uint hash = math.hash(new int2(x, y)) + (uint)(seed * 1000f);
            if (hash == 0) { hash = GuaranteeHashNonZero(hash, x, y); }
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(hash);
            buffer[index] = rand.NextFloat();
        }
    }

    public struct BenchmarkRecord
    {
        public string label;
        public int cycle;
        public int frame;
        public float ms;
        public long memoryBytes;
        public int gcCollections;
        public string timestamp;
    }
}
