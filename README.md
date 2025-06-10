# Introduction

This project is a Memory Arena framework inspired by [git-amend's recent YouTube video on the subject](https://www.youtube.com/watch?v=qIJxPAJ3R-I). 

I understood maybe 20% on the first watch. But that 20% hooked me enough to go down a very deep rabbit hole, and this project is the result.

# Overview
One of the biggest drawbacks to memory-managed frameworks like C# is the performance hit that you suffer as a result of garbage collection. This is especially true in Unity, which uses a non-generational and incremental garbage collector, and is more prone to memory fragmentation issues and doesn't handle small, frequent allocations as well. In most use-cases, this additional overhead doesn't make or break a project. Thoughtful system design and best-practices in OOP can get you pretty far, performance-wise. However, sometimes you just need to squeeze out as much raw performance as you can, or maybe you just wanna learn something new while doing something kinda unhinged (***cough***). That's what this is for!

### Enter: 'unsafe'.

Unsafe code is a language feature that allows you to bypass the compiler's memory safety checks and directly allocate, manipulate, and deallocate memory via pointers. In Unity, you also get access to Malloc(), Free(), and other fun toys via the UnsafeUtility class (Unity.Collections.LowLevel.Unsafe namespace). 

Has this little experiment made me wanna become a system-level developer?\
Absolutely not.\
But I had fun and learned a lot, and that's what matters, right? 😄

# Memory Arena

A memory arena is a low-level memory management technique where a large block of memory is allocated up front, and then smaller allocations are made from that block manually. This eliminates per-allocation overhead, enables predictable performance, and is especially useful in high-performance or real-time applications like games or simulations.

# Current Features

- Struct-based memory management for supporting basic types and other features depending on alignment:
  - **4-byte**: int, float, bool, Color32
  - **8-byte**: double, pointers (on 64-bit systems)
  - **16-byte**: Vector3, float4, Matrix4x4
  - **32-byte**: SIMD instructions
  - **64-byte**: CPU cache line alignment, allowing for Burst Jobs and other multi-threaded environments
- Manual and smart allocation
  - Manual: allows custom alignment setting
  - Smart: automatically sets alignment to the next power of 2 greater than or equal to the size of the allocation (minimizes waste)
- Arena monitoring system which logs a record of each allocation including offset, size, wasted bytes, an optional tag, and the arena it belongs to
- Multiple arena support
- Over-alignment (waste) tracking and logging
- Fully integrated with Unity's Test Framework, including a preset unit test suite with pass/fail reporting
- Custom color-coded logger which can optionally be saved to a timestamped text file
- Extensive guardrails against invalid alignment, OOM errors, segfaults, etc.
- Scriptable config toggles for dev/runtime tuning

# Planned Features

- Collection support (via custom lightweight containers for ArenaList<T>, ArenaMap<TKey, TValue>, etc.
- Burst/Jobs integration
- Custom editor windows for visualizing real-time allocations (bar graphs?)
- Packaging for real-world tooling and reuse

# Screenshots

## Unit Tests

**Test Suite**:\
![TestSuite](https://github.com/user-attachments/assets/c5c93156-ab39-4a19-91b7-dc2385d51199)

**Out of Memory Unit Test Output**:\
![UnitTestOutput](https://github.com/user-attachments/assets/c00b597a-73bb-40ab-8c71-5b9ca71c0d5d)

**Alignment Padding Tracker Test Output**:\
![UnitTestOutputLog](https://github.com/user-attachments/assets/af972bb8-3869-436b-868a-8ae1f716984f)

## Code Examples

**GetNextPowerOfTwo**:\
![GetNextPowerOfTwo](https://github.com/user-attachments/assets/63800a36-4ae6-4c99-823d-3b6357791b59)

**Allocate Methods**:\
![AllocateMethods](https://github.com/user-attachments/assets/b470a200-2d09-448d-9fea-55e30c4f6d22)

**Allocate Calls**:\
![AllocateCalls](https://github.com/user-attachments/assets/031cd8b9-7026-4f13-b202-24233f70ee0e)

**Dispose**:\
![Dispose](https://github.com/user-attachments/assets/93ad0aa7-b16e-428f-b8b0-0255e754e9fa)
