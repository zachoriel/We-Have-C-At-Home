using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public unsafe class NoiseGenerator_Unmanaged : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width = 256;
    [SerializeField] private int height = 256;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private bool useBurst = true;

    private ArenaAllocator* arena;
    private ArenaArray<float> buffer;
    private Texture2D outputTexture;

    private Dictionary<string, BenchmarkResult> benchmarks = new Dictionary<string, BenchmarkResult>();

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
        if (useBurst) { RunBurstArenaVersion(); }
        else { RunNoBurstArenaVersion(); }
    }

    private void RunBurstArenaVersion()
    {
        string label = "Arena + Burst";

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

        benchmarks[label] = new BenchmarkResult
        {
            ms = (end - start) * 1000f,
            memoryBytes = memoryUsed,
            gcCollections = gcAfter - gcBefore
        };

        UpdateTextureFromBuffer();
    }

    private void RunNoBurstArenaVersion()
    {
        string label = "Arena + No Burst";

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

        benchmarks[label] = new BenchmarkResult
        {
            ms = (end - start) * 1000f,
            memoryBytes = memoryUsed,
            gcCollections = gcAfter - gcBefore
        };

        UpdateTextureFromBuffer();
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

    public struct BenchmarkResult
    {
        public float ms;
        public long memoryBytes;
        public int gcCollections;
    }
}
