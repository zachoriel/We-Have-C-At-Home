# Memory Arena for Unity: Unsafe Memory Handled Safely, Zero Garbage Collection

This is a high-performance memory management toolkit for Unity, inspired by [git-amend's excellent primer on memory arenas](https://www.youtube.com/watch?v=qIJxPAJ3R-I). It began as an experiment in unsafe code and evolved into a full-featured arena-based allocator with custom containers, tracking, logging, and benchmark tooling — all built to answer a single question:

> *How much performance and memory control can you reclaim from Unity's managed heap without going insane?*

And the answer is... it depends.\
But here's a hint:
<p align="center">
  <img src="https://github.com/user-attachments/assets/ec49c806-97d3-4c3e-aea5-43dd6ac24979" width="600"/>
</p>
<p align="center"><em>Zero GC collections with Arena + Burst. Full control. No stutters.</em></p>

## What Is a Memory Arena?

A memory arena is a low-level memory management technique where a large block of memory is allocated up front, and then smaller allocations are made from that block manually. This eliminates per-allocation overhead, enables predictable performance, and is especially useful in high-performance or real-time applications like games or simulations.

## Why Use a Memory Arena in Unity?

Unity’s garbage collector (GC) is non-generational, incremental, and prone to stutters in GC-heavy workloads. Even Burst and Jobs don’t eliminate the problem if your allocations aren’t tightly scoped. Arena-based memory solves this by letting you allocate once, write in-place, and reset or dispose deterministically — zero GC involved.

**But don't just take my word for it — I measured it.**

---

## Benchmark Experiment - Design and Methodology

To test real-world impact, I implemented a sustained procedural workload that mimics conditions seen in simulation, terrain generation, and real-time systems:

> **Scenario**: Procedurally generate a 1024x1024 noise texture repeatedly, over 500 cycles, allocating a new buffer every 5 frames.

1024x1024 is a rather extreme texture size for noise generation, but I felt this was a reasonable tradeoff given that the benchmark only generates one texture at a time. In real systems, you might generate many smaller buffers per frame — the pressure adds up either way.

Each memory strategy was tested under identical conditions:
- Total allocations: 500 (one per cycle, sustained)
- Buffers: `float[]` (managed) or `ArenaArray<float>` (arena)
- Burst: Enabled or disabled via toggle
- GC tracking: `GC.GetTotalMemory()` and `GC.CollectionCount()`
- Timing: Frame-by-frame measurements using Time.realtimeSinceStartup
- Reset behavior: Arena memory was explicitly `Reset()` after each cycle; managed memory was left to accumulate (as it would in a standard Unity framework).

This simulation represents:
- Consistent short-lived allocation churn
- Heavy per-frame compute on large buffers
- Controlled memory lifecycle for comparison

## Benchmark Experiment - Results

The experiment resulted in 2,000 rows of CSV data. These results were aggregated and visualized to evaluate both **performance speed** and **memory pressure** over time:

| Strategy                     | Avg Time (ms) | Avg Memory (MB)  | GC Collections|
|------------------------------|---------------|------------------|---------------|
| Managed Memory + Burst       | ~4.27ms       | ~1,689 MB        | 55            |
| Managed Memory + No Burst    | ~55.92ms      | ~1,682 MB        | 58            |
| Arena Memory + No Burst      | ~59.31ms      | ~673 MB          | 2             |
| Arena Memory + Burst         | ~1.25ms       | ~675 MB          | 0             |

### Key Takeaways
- **Burst** is by far the biggest speed booster, but **Arena** reduces memory footprint dramatically.
- **Arena + No Burst** performs nearly on-par with **Managed + No Burst** in raw speed, but with less than half of the memory usage, and a complete elimination of GC calls.
- **Arena + Burst** offers the best of both worlds: the lowest memory footprint and fastest execution.

And because visual data is fun, here are some charts and graphs of the above findings:

![MillisecondsPerCycleChart](https://github.com/user-attachments/assets/85772674-6537-4ae8-8860-c1c52f094823)
![MemoryUsagePerCycleChart](https://github.com/user-attachments/assets/ea1fc947-4372-46ca-a12a-afc270ef2c11)
![FrameTimeByStrategyGraph](https://github.com/user-attachments/assets/3a0289e4-a1af-4f3e-a1bf-ae281a505ce5)
![MemoryUsageByStrategyGraph](https://github.com/user-attachments/assets/200df555-e3f4-40e9-941b-4c186fa9a9ca)
![GCsByStrategyGraph](https://github.com/user-attachments/assets/ec49c806-97d3-4c3e-aea5-43dd6ac24979)

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
- Extensive guardrails against invalid alignment, OOM errors, segfaults, etc.

---

## Planned Features

- Editor windows for allocation & performance visualization
- Packaging for real-world use

---

## How To Use

### Demo Scene

(ToDo: screenshots + steps)

### Unit Testing

(ToDo: screenshots + steps)

### Code Examples

(ToDo: screenshots + steps)
