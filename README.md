# Introduction

This project is a Memory Arena framework inspired by git-amend's recent YouTube video on the subject (which can be found here: https://www.youtube.com/watch?v=qIJxPAJ3R-I). 

On the first watch, I understood about 20% of it.

But that 20% interested me enough that I went down a very deep rabbit hole, and this project is the result of that research. 

# Overview
One of the biggest drawbacks to memory-managed frameworks like C# is the performance hit that you suffer as a result of garbage collection. This is especially true in Unity, which uses a non-generational and incremental garbage collector, and is more prone to memory fragmentation issues and doesn't handle small, frequent allocations as well. In most use-cases, this additional overhead doesn't make or break a project. Thoughtful system design and best-practices in OOP can get you pretty far, performance-wise. However, sometimes you just need to squeeze out as much raw performance as you can, or maybe you just wanna learn something new while doing something kinda unhinged (***cough***). That's what this is for!

### Enter: 'unsafe'.

Unsafe code is a language feature that allows you to bypass the compiler's memory safety checks and directly allocate, manipulate, and deallocate memory via pointers. In Unity, you also get access to Malloc(), Free(), and other fun toys via the UnsafeUtility class. 

# Memory Arena

[Explain what a memory arena is / does.]

# Current Features

- Struct-based memory management for supporting basic types and other features depending on alignment:
- - 4-byte: int, float, bool, Color32
  - 8-byte: double, pointers (on 64-bit systems)
  - 16-byte: Vector3, float4, Matrix4x4
  - 32-byte: SIMD instructions
  - 64-byte CPU cache line alignment, allowing for Burst Jobs and other multi-threaded environments
- Manual and smart allocation
- - Manual: allows custom alignment setting
  - Smart: automatically sets alignment to the next power of 2 greater than or equal to the size of the allocation (minimizes waste)
- Arena monitoring system which logs a record of each allocation including offset, size, wasted bytes, an optional tag, and the arena it belongs to
- Multiple arena support
- Over-alignment (waste) tracking and logging
- Full Unity Test Framework integration with a preset unit test suite
- Custom color-coded logger which can optionally be saved to a timestamped text file
- Extensive guardrails against invalid alignment, OOM errors, segfaults, etc.
- Scriptable config toggles for dev/runtime tuning

# Planned Features

- Collection support (via custom lightweight containers for ArenaList<T>, ArenaMap<TKey, TValue>, etc.
- Burst/Jobs integration
- Custom editor windows for visualizing real-time allocations (bar graphs?)
- Packaging for use as a tool in real development situations

# Screenshots

[This is where screenshots will go]
