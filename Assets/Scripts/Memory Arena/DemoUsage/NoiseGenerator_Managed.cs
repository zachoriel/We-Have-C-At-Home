using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;

public class NoiseGenerator_Managed : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int width = 256;
    [SerializeField] private int height = 256;
    [SerializeField] private Material targetMaterial;
    [SerializeField] private bool useBurst = true;

    private float[] buffer;
    private Texture2D outputTexture;

    private Dictionary<string, BenchmarkResult> benchmarks = new Dictionary<string, BenchmarkResult>();

    private void Start()
    {
        buffer = new float[width * height];

        outputTexture = new Texture2D(width, height, TextureFormat.RFloat, false);
        outputTexture.filterMode = FilterMode.Point;
        targetMaterial.mainTexture = outputTexture;
    }

    private void Update()
    {
        if (useBurst) { RunBurstManagedVersion(); }
        else { RunNoBurstManagedVersion(); }
    }

    private void RunBurstManagedVersion()
    {
        string label = "Managed + Burst";

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

        benchmarks[label] = new BenchmarkResult
        {
            ms = (end - start) * 1000f,
            memoryBytes = memoryUsed,
            gcCollections = gcAfter - gcBefore
        };

        UpdateTextureFromBuffer();
    }

    private void RunNoBurstManagedVersion()
    {
        string label = "Managed + No Burst";

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

    public struct BenchmarkResult
    {
        public float ms;
        public long memoryBytes;
        public int gcCollections;
    }
}