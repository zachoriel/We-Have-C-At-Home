# Memory Arena for Unity: Unsafe Memory Handled Safely, Zero Garbage Collection, Full Control

![Unity 6000.0.42](https://img.shields.io/badge/unity-6000.0.42%2B-blue?logo=unity)
![Last Commit](https://img.shields.io/github/last-commit/zachoriel/We-Have-C-At-Home)

This is a high-performance memory management toolkit for Unity, inspired by [git-amend's excellent primer on memory arenas](https://www.youtube.com/watch?v=qIJxPAJ3R-I). It started as an experiment to learn more about unsafe code and evolved into a full-featured arena-based allocator with custom containers, tracking, logging, and benchmark tooling — all built to answer a single question:

> *How much performance and memory control can you reclaim from Unity's managed heap without going insane?*

And the answer is... it depends.\
But here's a hint:
<p align="center">
  <img src="https://github.com/user-attachments/assets/ec49c806-97d3-4c3e-aea5-43dd6ac24979" width="600"/>
</p>
<p align="center"><em>Zero garbage collection with Arena + Burst. Full control. No stutters. No compromises.</em></p>

###### More graphs, charts, and data tables can be found below. 😎

<details>
<summary>Table of Contents</summary>

- [Repository Overview](#repository-overview)
- [What Is a Memory Arena?](#what-is-a-memory-arena)
- [What Is Burst?](#what-is-burst)
- [Why Use a Memory Arena in Unity?](#why-use-a-memory-arena-in-unity)
- [Benchmark Experiment - Design and Methodology](#benchmark-experiment---design-and-methodology)
- [Benchmark Experiment - Results](#benchmark-experiment---results) <-- click here if you just wanna see the tables & graphs
- [When Should You Use ArenaAllocator?](#when-should-you-use-arenaallocator)
- [Current Features](#current-features)
- [Things I May Add in the Future](#things-i-may-add-in-the-future)
- [How To Use](#how-to-use)
</details>

## Repository Overview

#### Core Scripts
- [ArenaAllocator](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/ArenaAllocator.cs)
- [ArenaMonitor](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/ArenaMonitor.cs)
- [ArenaLog](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/ArenaLog.cs)
- [ArenaArray](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/CustomCollections/ArenaArray.cs)
- [ArenaList](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/CustomCollections/ArenaList.cs)
- [NoiseGenerator_Unmanaged (demo usage)](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/DemoUsage/NoiseGenerator_Unmanaged.cs)
- [ArenaAllocatorTests (unit test suite)](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Tests/PlayMode/ArenaAllocatorTests.cs)

## What Is a Memory Arena?

A memory arena is a low-level memory management technique where a large block of memory is allocated up front, and then smaller allocations are made from that block manually. This eliminates per-allocation overhead, enables predictable performance, and is especially useful in high-performance or real-time applications like games or simulations.

## What Is Burst?

Burst refers to the "Burst compiler" — a Unity package that works with Unity's Job System to execute highly-optimized C# code. It adds a small amount of code complexity to your project, but yields significant speed gains in execution time. My memory arena is designed to be used alongside Burst/Jobs, but it is not a hard requirement. The benchmarks shown here include tests both with and without Burst, for empirical comparison of performance benefit.

## Why Use a Memory Arena in Unity?

Unity’s garbage collector (GC) is non-generational, incremental, and prone to stutters in GC-heavy workloads. Even Burst and Jobs don’t eliminate the problem if your allocations aren’t tightly scoped. Arena-based memory solves this by letting you allocate once, write in-place, and reset or dispose deterministically — zero GC involved.

**But you don't have to take my word for it — I measured it.**

---

## Benchmark Experiment - Design and Methodology

To test real-world impact, I implemented a sustained procedural workload that mimics conditions seen in simulation, terrain generation, and real-time systems:

> **Scenario**: Procedurally generate a 1024x1024 noise texture repeatedly, over 500 cycles, allocating a new buffer every 5 frames.

1024x1024 is a fairly extreme texture size for procedural noise, but I chose it intentionally as a tradeoff. The benchmark only generates one texture at a time, which helps isolate allocation behavior and performance impact. In real-world systems, you'd often see many smaller allocations per frame — but the cumulative memory pressure and GC risk scale similarly. This setup simulates that load while keeping the test controlled and interpretable.

<details>
  
<summary>Summary (For Non-Programmers)</summary>

I tested four variations of a system that generates data over time. Some used Unity’s built-in memory tools, others used my custom arena system. I tracked speed, memory usage, and how often Unity had to “clean up” memory (garbage collection — GC).

The results: my arena system reduced memory usage significantly (up to 60%), ran a little faster, and avoided performance stutters caused by uncontrollable memory cleanup; resulting in a smoother experience for processes that are usually prone to freezes/spikes.
</details>

<details>

<summary>Technical Explanation (For Programmers)</summary>

Each memory strategy was tested under identical conditions:
- Total allocations: 500 (one per cycle, sustained)
- Buffers: `float[]` (managed) or `ArenaArray<float>` (arena)
- Burst: Enabled or disabled via toggle
- GC tracking: `GC.GetTotalMemory()` and `GC.CollectionCount()`
- Timing: Frame-by-frame measurements using Time.realtimeSinceStartup
- Reset behavior: Arena memory was explicitly `Reset()` after each cycle; managed memory was left to accumulate (as it would in a standard Unity framework).
- Environment: Unity Editor, with custom logging enabled for ArenaAllocator (note that this introduces some artificial overhead).

This simulation represents:
- Consistent short-lived allocation churn
- Heavy per-frame compute on large buffers
- Controlled memory lifecycle for comparison
</details>

I then ran the experiment 10 more times to check for deviation. Below are the results:

## Benchmark Experiment - Results

### First Test Run (control group)

The experiment resulted in 2,000 rows of CSV data. These results were aggregated and visualized to evaluate both **performance speed** and **memory pressure** over time:

<div align="center">
  
| Strategy                       | Avg Time (ms)   | Avg Memory (MB)    | GC Collections  |
|:------------------------------:|:---------------:|:------------------:|:---------------:|
| Managed Memory + Burst         | ~4.27ms         | ~1,689 MB          | 55              |
| Managed Memory + No Burst      | ~55.92ms        | ~1,682 MB          | 58              |
| Arena Memory + No Burst        | ~59.31ms        | ~673 MB            | 2               |
| Arena Memory + Burst           | ~1.25ms         | ~675 MB            | 0               |

</div>

### Key Takeaways
- **Burst** is by far the biggest speed booster, but **Arena** reduces memory footprint dramatically.
- **Arena + No Burst** performs nearly on-par with **Managed + No Burst** in raw speed, but with less than half of the memory usage, and a near-complete elimination of GC calls.
- **Arena + Burst** offers the best of both worlds: the lowest memory footprint and fastest execution.

And because visual data is fun, here are some charts and graphs that bring the data to life:

![MillisecondsPerCycleChart](https://github.com/user-attachments/assets/85772674-6537-4ae8-8860-c1c52f094823)
<p align="center"><em> ^ Note the flat green line. while the other methods are either slow, unpredictable, or both, <b>Arena + Burst</b> remains fast and steady throughout. No GC = no spikes, more determinism.</em></p>

![MemoryUsagePerCycleChart](https://github.com/user-attachments/assets/ea1fc947-4372-46ca-a12a-afc270ef2c11)
<p align="center"><em>^ Note: while the theoretical peak allocation is ~2GB for 500 x 4MB 1024x1024 buffers, actual memory use under managed memory exceeded 2.7GB due to GC fragmentation, large object heap behavior, and editor overhead. This reflects typical worst-case bloat/pressure when relying on the managed allocator.</em></p>

![FrameTimeByStrategyGraph](https://github.com/user-attachments/assets/3a0289e4-a1af-4f3e-a1bf-ae281a505ce5)
![MemoryUsageByStrategyGraph](https://github.com/user-attachments/assets/200df555-e3f4-40e9-941b-4c186fa9a9ca)
![GCsByStrategyGraph](https://github.com/user-attachments/assets/ec49c806-97d3-4c3e-aea5-43dd6ac24979)

### Additional 10 Test Runs

These tests resulted in a combined 20,000 rows of CSV data, which were aggregated and visualized with the same metrics as the control group:

<div align="center">
  
| Strategy                       | Avg Time (ms) Per Run   | Avg Memory (MB) Per Run   | Avg GC Collections Per Run             |
|:------------------------------:|:-----------------------:|:-------------------------:|:--------------------------------------:|
| Managed Memory + Burst         | ~3.84ms                 | ~1,651 MB                 | 48                                     |
| Managed Memory + No Burst      | ~54.25ms                | ~1,648 MB                 | 55                                     |
| Arena Memory + No Burst        | ~59.75ms                | ~629 MB                   | 0.1 (1 GC call across all runs)        |
| Arena Memory + Burst           | ~1.27ms                 | ~630 MB                   | 0.3 (3 GC calls across all runs)       |

</div>

### Key Takeaways
- Performance metrics remained consistent with the control group, with minor deviation assumed to be from random editor overhead oddities, incidental editor GC, etc.
- **Arena + Burst** remains by far the most performant system, with minor speed increases over **Managed Memory + Burst** and still ~2.6x less memory footprint.
- **Burst** is still the heavy-lifter for raw speed gains.
- **Arena** near-completely eliminates GC calls, resulting in steady and predictable overhead.
- **Arena *without* Burst** introduces minor speed overhead compared to **Managed *without* Burst**. This suggests that **Arena + Burst** is the optimal workflow, but you can still yield significant memory gains with minimal ms impact without **Burst**.

![AverageMillisecondsPerCycleChart_LargeDataset](https://github.com/user-attachments/assets/31403822-6865-45b4-a007-031088103357)
<p align="center"><em>That near-flat Arena + Burst line amidst the sea of volatility that is Managed + Burst is <b>so</b> satisfying.</em></p>

![MemoryUsagePerCycleChart_LargeDataset](https://github.com/user-attachments/assets/fe869077-5f6e-4cf6-a73d-5274179a3c4c)

The raw CSV files for these tests can be found in Assets/Logs if you want to check them for yourself.

**Important Caveat**: As mentioned, these benchmarks were run in-editor with logging enabled for the ArenaAllocator (~600 log entries per test run of the Arena strategy). Real-world performance in builds and/or with logging disabled may be even faster, especially for Arena memory. Anecdotally, my Arena + Burst runs in a build environment with no logging consistently averaged about 0.8ms and 0 GC calls.

---

## When Should You Use ArenaAllocator?

Use it when you need **tight control over memory** or want to **avoid GC stutters** — especially in systems that allocate frequently or work with large buffers.

Ideal use cases include:

### Procedural Generation
- Terrain heightmaps, noise layers, structure gen \
**Why**: You can pre-allocate, write in-place, and reset every frame

### Real-Time Simulation Buffers
- Particles, fluids, heatmaps, cellular automata \
**Why**: Thousands of updates per frame with no GC

### AI & Pathfinding
- NavMesh fields, A* sets, LOS caches, utility maps \
**Why**: Short-lived buffers that burst jobs can tear through

### Custom Physics / Collision Grids
- BVH trees, spatial grids, contact caches \
**Why**: Reset on every tick for deterministic behavior

### Massive Game State / ECS Buffers
- Turn systems, card game branches, dense game logic \
**Why**: Emulates ECS-style temporary buffers

### Image Processing / Volume Data
- Edge detection, compute frame analysis, 3D terrain edits \
**Why**: Avoids GC in high-volume 2D/3D byte or float arrays

### Editor Tooling / Import Pipelines
- Asset importers, mesh post-processing, one-time builders \
**Why**: Avoids churn during editor operations

---

## Current Features

- **ArenaAllocator** with manual or smart allocation
  - Manual: allows custom alignment setting & size calculation
  - Smart: automatically sets alignment to the next power of 2 greater than or equal to the size of the allocation (minimizes waste)
- **ArenaArray<T>** and **ArenaList<T>** containers
  - ArenaArray<T>: A Burst-compatible, fixed-length array backed by arena-allocated unmanaged memory. Like `NativeArray<T>`, but without ownership or disposal — memory is released when the parent `ArenaAllocator` is disposed or reset.
  - ArenaList<T>: A Burst-compatible, unmanaged container that hooks into an arena allocator for high-performance collections. Like `ArenaArray<T>`, this structure does not own its memory and is managed by `ArenaAllocator`'s disposal or reset. Use this in place of `NativeList<T>` if you want integration with memory arenas and automatic disposal, and are okay with      fixed capacities. (ArenaLists can be manually resized via reallocation, but do not grow dynamically like standard Lists.)
- Custom logging with color coding, source labeling, timestamps, and export
- Togglable monitoring system with per-arena allocation tracking (offsets, sizes, tags, over-alignment)
- Multiple arena support with ID-based isolation
- Over-alignment tracking to monitor memory waste
- Fully compatible with Burst and `IJobParallelFor`
- No GC, optional disposal — reset the arena, not the heap
- Fully integrated with Unity's Test Framework, including a premade unit test suite with pass/fail reporting
- Extensive guardrails against memory leaks, invalid alignment, OOM errors, segfaults, etc.

---

## Things I May Add in the Future

- Editor windows for allocation & performance visualization, maybe a CSV importer that plots data to graphs automatically
- Arena wizards & packaging for real-world use
- ArenaHashmap container

---

## How To Use

This project is currently a research-focused case study, not a production-ready library. That said, if you ***really*** want to plug this into your project, here is an ***extremely barebones*** collection of code that you may find useful for getting started.

`var arena = new ArenaAllocator(id: 0, capacityInBytes: 1024 * 1024, Allocator.Persistent);` -- arena has methods like Allocate, SmartAllocate, Reset, and Dispose.

`var array = new ArenaArray<float>(arena, length: 256, "ExampleBuffer");` -- ArenaArray hooks into an arena for allocation in its constructor, so you don't have to worry about its lifetime management.

I also highly recommend taking a look at both [NoiseGenerator_Unmanaged](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Scripts/Memory%20Arena/DemoUsage/NoiseGenerator_Unmanaged.cs) and [ArenaAllocatorTests](https://github.com/zachoriel/We-Have-C-At-Home/blob/main/Assets/Tests/PlayMode/ArenaAllocatorTests.cs) to see real examples ArenaAllocator in action. 

---

You know what's funny? I learned more about memory management by working on this project than I ever did in any of the C++ courses I took in college. I guess making it work in an environment where you're *not supposed* to do it forces you to think about the concepts in a different way.
