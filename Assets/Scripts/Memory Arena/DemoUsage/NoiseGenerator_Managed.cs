using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public class NoiseGenerator_Managed : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width = 1024;
    [SerializeField] private int height = 1024;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private bool useBurst = true;
    [SerializeField] private bool runAllBenchmarks = false;

    private float[] buffer;
    private Texture2D outputTexture;

    private List<BenchmarkRecord> benchmarkLog = new List<BenchmarkRecord>();
    private int benchmarkIndex = 0;
    private string[] benchmarkSequence = { "Managed + Burst", "Managed + No Burst" };
    private bool hasBenchmarked = false;

    private void Start()
    {
        buffer = new float[width * height];

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
            ArenaLog.Log($"NoiseGenerator_Managed", "Ran all benchmarks, safe to export.", ArenaLog.Level.Success);
            return;
        }

        string label = benchmarkSequence[benchmarkIndex];
        switch (label)
        {
            case "Managed + Burst":
                RunBurstManagedVersion(label);
                ArenaLog.Log($"NoiseGenerator_Managed", "Ran Burst benchmark", ArenaLog.Level.Success);
                break;
            case "Managed + No Burst":
                RunNoBurstManagedVersion(label);
                ArenaLog.Log($"NoiseGenerator_Managed", "Ran No Burst benchmark", ArenaLog.Level.Success);
                break;
        }

        benchmarkIndex++;
    }

    private void RunManualBenchmark()
    {
        string label = useBurst ? "Managed + Burst" : "Managed + No Burst";

        if (useBurst)
        {
            RunBurstManagedVersion(label);
        }
        else
        {
            RunNoBurstManagedVersion(label);
        }

        hasBenchmarked = true;
        ArenaLog.Log($"NoiseGenerator_Managed", "Ran benchmark, safe to export.", ArenaLog.Level.Success);
    }

    private void RunBurstManagedVersion(string label)
    {
        int total = width * height;
        int gcBefore = GC.CollectionCount(0);
        float start = Time.realtimeSinceStartup;

        NativeArray<float> nativeBuffer = new NativeArray<float>(total, Allocator.TempJob);

        GenerateManagedNoiseJob job = new GenerateManagedNoiseJob
        {
            buffer = nativeBuffer,
            width = width,
            height = height,
            seed = Time.time
        };

        JobHandle handle = job.Schedule(total, 64);
        handle.Complete();

        nativeBuffer.CopyTo(buffer);
        nativeBuffer.Dispose();

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

    private void RunNoBurstManagedVersion(string label)
    {
        int total = width * height;
        int gcBefore = GC.CollectionCount(0);
        float start = Time.realtimeSinceStartup;

        for (int index = 0; index < total; index++)
        {
            int x = index % width;
            int y = index / width;

            uint hash = (uint)(math.hash(new int2(x, y)) + (uint)(Time.time * 1000f));
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
        string path = Path.Combine(ArenaConfig.LoggingPath, "ManagedBenchmarks.csv");
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
        ArenaLog.Log("NoiseGenerator_Managed", $"Benchmark results exported to {path}.", ArenaLog.Level.Success);
    }

    private void UpdateTextureFromBuffer()
    {
        Color[] colors = new Color[buffer.Length];
        for (int i = 0; i < buffer.Length; i++)
        {
            float v = buffer[i];
            colors[i] = new Color(v, v, v);
        }

        outputTexture.SetPixels(colors);
        outputTexture.Apply();
    }

    [BurstCompile]
    public struct GenerateManagedNoiseJob : IJobParallelFor
    {
        public NativeArray<float> buffer;
        public int width;
        public int height;
        [ReadOnly] public float seed;

        public void Execute(int index)
        {
            int x = index % width;
            int y = index / width;

            uint hash = (uint)(math.hash(new int2(x, y)) + (uint)(seed * 1000f));
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