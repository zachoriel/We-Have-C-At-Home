using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// An unmanaged container that hooks-into an arena allocator for high-performance collections.
/// This list does not own its memory and does not support manual disposal. Its backing memory is freed
/// when the parent ArenaAllocator is reset or disposed. Use this in place of NativeList if you want
/// integration with memory arenas and automatic disposal, and are okay with fixed capacities.
/// </summary>
/// <typeparam name="T">Only unmanaged value types are supported (e.g. int, float, Vector3, MyUnmanagedStruct).</typeparam>
[DebuggerDisplay("Length = {length}, Capacity = {capacity}")]
public unsafe struct ArenaList<T> where T : unmanaged
{
    private void* data;
    private int count;
    private int capacity;

    public ArenaList(ArenaAllocator* arena, int capacity, string tag = "ArenaList")
    {
        // Slightly redundant, but UnsafeUtility is stricter than C#'s 'unmanaged', so I think the extra guardrail is worth it.
        if (!UnsafeUtility.IsUnmanaged<T>())
        {
            throw new InvalidOperationException($"ArenaList<T> requires T to be unmanaged. Type {typeof(T)} is not.");
        }

        data = arena->SmartAllocate<T>(tag);

        if (data == null)
        {
            throw new InvalidOperationException("Arena allocation failed for ArenaList.");
        }

        this.count = 0;
        this.capacity = capacity;

        ArenaLog.Log("ArenaList", $"Allocated a new ArenaList in arena {arena->GetID()}. Capacity: {capacity}, Length: {count}, Tag: {tag}.", ArenaLog.Level.Success);
    }

    public int Count => count;
    public int Capacity => capacity;

    public void Add(T value)
    {
        if (count >= capacity)
        {
            throw new IndexOutOfRangeException($"ArenaList capacity exceeded: {count + 1} / {capacity}");
        }

        UnsafeUtility.WriteArrayElement(data, count, value);
        count++;

        ArenaLog.Log("ArenaList", $"New value {value} added to list, new length: {count}, old length: {count - 1}.", ArenaLog.Level.Success);
    }

    public void AddMultiple(ReadOnlySpan<T> values)
    {
        if (count + values.Length > capacity)
        {
            throw new IndexOutOfRangeException("Not enough room in ArenaList to AddMultiple.");
        }

        for (int i = 0; i < values.Length; i++)
        {
            UnsafeUtility.WriteArrayElement(data, count + i, values[i]);
        }

        count += values.Length;
    }

    public void RemoveAt(int index = -1)
    {
        if (count == 0)
        {
            throw new InvalidOperationException("Cannot remove from an empty ArenaList.");
        }

        // If index is not provided or is -1, remove the last element
        if (index == -1)
        {
            index = count - 1;
        }

        if (index < 0 || index >= count)
        {
            throw new IndexOutOfRangeException();
        }

        var valueToRemove = UnsafeUtility.ReadArrayElement<T>(data, index);
        // Shift elements left.
        for (int i = index; i < count - 1; i++)
        {
            var next = UnsafeUtility.ReadArrayElement<T>(data, i + 1);
            UnsafeUtility.WriteArrayElement(data, i, next);
        }
        count--;

        ArenaLog.Log("ArenaList", $"Value {valueToRemove} removed from ArenaList. New length is: {count}.", ArenaLog.Level.Success);
    }

    public void InsertAt(int index, T value)
    {
        if (index < 0 || index > count)
        {
            throw new IndexOutOfRangeException();
        }
        if (count >= capacity)
        {
            throw new IndexOutOfRangeException($"ArenaList capacity exceeded: {count + 1} / {capacity}");
        }

        // Shift elements right.
        for (int i = count; i > index; i--)
        {
            var prev = UnsafeUtility.ReadArrayElement<T>(data, i - 1);
            UnsafeUtility.WriteArrayElement(data, i, prev);
        }

        UnsafeUtility.WriteArrayElement(data, index, value);
        count++;

        ArenaLog.Log("ArenaList", $"Value {value} added to ArenaList at index {index}. New length is: {count}.", ArenaLog.Level.Success);
    }

    public void Clear()
    {
        count = 0;

        ArenaLog.Log("ArenaList", $"ArenaList cleared, new length: {count}.", ArenaLog.Level.Success);
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            var val = UnsafeUtility.ReadArrayElement<T>(data, index);
            ArenaLog.Log("ArenaList", $"ArenaList index {index} value is: {val}", ArenaLog.Level.Success);
            return val;
        }
        set
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException();
            }

            UnsafeUtility.WriteArrayElement(data, index, value);

            ArenaLog.Log("ArenaList", $"ArenaList index {index} set to {value}.", ArenaLog.Level.Success);
        }
    }

    public readonly bool IsCreated => data != null;

    public T[] ToArray()
    {
        var array = new T[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = UnsafeUtility.ReadArrayElement<T>(data, i);
        }

        if (array.Length == 0)
        {
            ArenaLog.Log("ArenaList", "Array length is 0.", ArenaLog.Level.Warning);
        }
        else
        {
            ArenaLog.Log("ArenaList", $"Converted ArenaList to array.", ArenaLog.Level.Success);
        }

        return array;
    }

    public Enumerator GetEnumerator() => new Enumerator(this);
    public ref struct Enumerator
    {
        private ArenaList<T> list;
        private int index;

        public Enumerator(ArenaList<T> list)
        {
            this.list = list;
            this.index = -1;
        }

        public bool MoveNext()
        {
            index++;
            return index < list.count;
        }

        public T Current => list[index];
    }
}
