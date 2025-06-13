using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public unsafe class NoiseGenerator_Unmanaged : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width = 1024;
    [SerializeField] private int height = 1024;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private bool useBurst = true;
    [SerializeField] private bool runAllBenchmarks = false;

    private ArenaAllocator* arena;
    private ArenaArray<float> buffer;
    private Texture2D outputTexture;

    private List<BenchmarkRecord> benchmarkLog = new List<BenchmarkRecord>();
    private int benchmarkIndex = 0;
    private string[] benchmarkSequence = { "Arena + Burst", "Arena + No Burst" };
    private bool hasBenchmarked = false;

    private unsafe void Start()
    {
        arena = (ArenaAllocator*)UnsafeUtility.Malloc(sizeof(ArenaAllocator), 64, Allocator.Persistent);
        *arena = new ArenaAllocator(0, width * height * sizeof(float), Allocator.Persistent);
        buffer = new ArenaArray<float>(arena, width * height, "NoiseField");

        outputTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        outputTexture.filterMode = FilterMode.Point;
        targetMaterial.mainTexture = outputTexture;
    }

    private void Update()
    {
        if (runAllBenchmarks && !hasBenchmarked)
        {
            RunNextBenchmark();
        }
        else if (!runAllBenchmarks && !hasBenchmarked)
        {
            if (Input.GetKeyDown(ArenaConfig.RunBenchmarkKey))
            {
                RunManualBenchmark();
            }
        }

        if (Input.GetKeyDown(ArenaConfig.BenchmarkExportKey))
        {
            ExportBenchmarksToCSV();
        }
    }

    private void RunNextBenchmark()
    {
        if (benchmarkIndex >= benchmarkSequence.Length)
        {
            hasBenchmarked = true;
            ArenaLog.Log($"NoiseGenerator_Unmanaged", "Ran all benchmarks, safe to export.", ArenaLog.Level.Success);
            return;
        }

        string label = benchmarkSequence[benchmarkIndex];
        switch (label)
        {
            case "Arena + Burst":
                RunBurstArenaVersion(label);
                ArenaLog.Log($"NoiseGenerator_Unmanaged", "Ran Burst benchmark", ArenaLog.Level.Success);
                break;
            case "Arena + No Burst":
                RunNoBurstArenaVersion(label);
                ArenaLog.Log($"NoiseGenerator_Unmanaged", "Ran No Burst benchmark", ArenaLog.Level.Success);
                break;
        }

        benchmarkIndex++;
    }

    private void RunManualBenchmark()
    {
        string label = useBurst ? "Arena + Burst" : "Arena + No Burst";

        if (useBurst)
        {
            RunBurstArenaVersion(label);
        }
        else
        {
            RunNoBurstArenaVersion(label);
        }

        hasBenchmarked = true;
        ArenaLog.Log($"NoiseGenerator_Unmanaged", "Ran benchmark, safe to export.", ArenaLog.Level.Success);
    }

    private void RunBurstArenaVersion(string label)
    {
        int total = width * height;
        int gcBefore = GC.CollectionCount(0);
        float start = Time.realtimeSinceStartup;

        GenerateNoiseJob job = new GenerateNoiseJob
        {
            buffer = buffer,
            width = width,
            height = height,
            seed = Time.time // Change over time for animation
        };

        JobHandle handle = job.Schedule(total, 64);
        handle.Complete();

        float end = Time.realtimeSinceStartup;
        int gcAfter = GC.CollectionCount(0);
        long memoryUsed = GC.GetTotalMemory(false);

        benchmarkLog.Add(new BenchmarkRecord
        {
            label = label,
            ms = (end - start) * 1000f,
            memoryBytes = memoryUsed,
            gcCollections = gcAfter - gcBefore,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        UpdateTextureFromBuffer();
    }

    private void RunNoBurstArenaVersion(string label)
    {
        int total = width * height;
        int gcBefore = GC.CollectionCount(0);
        float start = Time.realtimeSinceStartup;

        for (int index = 0; index < total; index++)
        {
            int x = index % width;
            int y = index / width;

            float seed = Time.time;
            uint hash = math.hash(new int2(x, y)) + (uint)(seed * 1000f);
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(hash);
            buffer[index] = rand.NextFloat();
        }

        float end = Time.realtimeSinceStartup;
        int gcAfter = GC.CollectionCount(0);
        long memoryUsed = GC.GetTotalMemory(false);

        benchmarkLog.Add(new BenchmarkRecord
        {
            label = label,
            ms = (end - start) * 1000f,
            memoryBytes = memoryUsed,
            gcCollections = gcAfter - gcBefore,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        });

        UpdateTextureFromBuffer();
    }

    private void ExportBenchmarksToCSV()
    {
        string path = Path.Combine(ArenaConfig.LoggingPath, "ArenaBenchmarks.csv");
        bool fileExists = File.Exists(path);

        using StreamWriter writer = new StreamWriter(path, true);
        if (!fileExists)
        {
            writer.WriteLine("Label,Milliseconds,MemoryBytes,GCCollections,Timestamp");
        }

        foreach (var record in benchmarkLog)
        {
            string line = $"{record.label},{record.ms:F3},{record.memoryBytes},{record.gcCollections},{record.timestamp}";
            writer.WriteLine(line);
        }

        benchmarkLog.Clear();
        ArenaLog.Log("NoiseGenerator_Unmanaged", $"Benchmark results exported to {path}.", ArenaLog.Level.Success);
    }

    private void UpdateTextureFromBuffer()
    {
        var pixels = new float[width * height];
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

    private void OnDestroy()
    {
        if (arena != null)
        {
            arena->Dispose();
            UnsafeUtility.Free(arena, Allocator.Persistent);
            arena = null;
        }
    }

    [BurstCompile]
    public unsafe struct GenerateNoiseJob : IJobParallelFor
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
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(hash);
            buffer[index] = rand.NextFloat();
        }
    }

    public struct BenchmarkRecord
    {
        public string label;
        public float ms;
        public long memoryBytes;
        public int gcCollections;
        public string timestamp;
    }
}
