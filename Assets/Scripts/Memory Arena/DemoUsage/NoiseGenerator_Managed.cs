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
    [Header("Texture Settings")]
    [SerializeField] private int width = 1024;
    [SerializeField] private int height = 1024;
    [SerializeField] private Material targetMaterial;

    [Header("Benchmark Settings")]
    [SerializeField] private int allocationsPerCycle = 1;
    [SerializeField] private int framesPerCycle = 5;
    [SerializeField] private int totalCycles = 100;
    [SerializeField] private bool useBurst = true;

    private List<float[]> managedBuffers = new List<float[]>();
    private Texture2D outputTexture;

    private List<BenchmarkRecord> benchmarkLog = new List<BenchmarkRecord>();
    private int currentCycle = 0;
    private int currentFrameInCycle = 0;
    private bool benchmarkComplete = false;

    private void Start()
    {
        outputTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        outputTexture.filterMode = FilterMode.Point;
        targetMaterial.mainTexture = outputTexture;
    }

    private void Update()
    {
        if (!benchmarkComplete && currentCycle < totalCycles)
        {
            currentFrameInCycle++;
            int total = width * height;

            int gcBefore = GC.CollectionCount(0);
            float start = Time.realtimeSinceStartup;

            for (int i = 0; i < allocationsPerCycle; i++)
            {
                float[] buffer = new float[total];
                if (useBurst)
                {
                    GenerateNoiseBurst(buffer);
                }
                else
                {
                    GenerateNoiseNoBurst(buffer);
                }
                managedBuffers.Add(buffer);
            }

            float end = Time.realtimeSinceStartup;
            int gcAfter = GC.CollectionCount(0);
            long memoryUsed = GC.GetTotalMemory(false);

            benchmarkLog.Add(new BenchmarkRecord
            {
                label = useBurst ? "Managed + Burst" : "Managed + No Burst",
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
            }

            UpdateTextureFromBuffer(managedBuffers[^1]);
        }

        if (currentCycle >= totalCycles && !benchmarkComplete)
        {
            benchmarkComplete = true;
            ArenaLog.Log("NoiseGenerator_Managed", "All benchmark cycles complete; ready to export.", ArenaLog.Level.Success);
        }

        if (Input.GetKeyDown(ArenaConfig.BenchmarkExportKey))
        {
            ExportBenchmarksToCSV();
        }
    }

    private void GenerateNoiseNoBurst(float[] buffer)
    {
        for (int index = 0; index < buffer.Length; index++)
        {
            int x = index % width;
            int y = index / width;

            uint hash = (uint)(math.hash(new int2(x, y)) + (uint)(Time.time * 1000f));
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random(hash);
            buffer[index] = rand.NextFloat();
        }
    }

    private void GenerateNoiseBurst(float[] buffer)
    {
        NativeArray<float> nativeBuffer = new NativeArray<float>(buffer.Length, Allocator.TempJob);

        var job = new GenerateManagedNoiseJob
        {
            buffer = nativeBuffer,
            width = width,
            height = height,
            seed = Time.time
        };

        job.Schedule(buffer.Length, 64).Complete();
        nativeBuffer.CopyTo(buffer);
        nativeBuffer.Dispose();
    }

    private void UpdateTextureFromBuffer(float[] buffer)
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

    private void ExportBenchmarksToCSV()
    {
        string path = Path.Combine(ArenaConfig.LoggingPath, "ManagedBenchmarks_Cycles.csv");
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
        ArenaLog.Log("NoiseGenerator_Managed", $"Benchmark results exported to {path}.", ArenaLog.Level.Success);
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
        public int cycle;
        public int frame;
        public float ms;
        public long memoryBytes;
        public int gcCollections;
        public string timestamp;
    }
}