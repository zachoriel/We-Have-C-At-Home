using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

/// <summary>
/// A Burst-safe, fixed-length array backed by arena-allocated unmanaged memory.
/// Like NativeArray, but without ownership or disposal — memory is released when the parent ArenaAllocator is disposed or reset.
/// </summary>
/// <typeparam name="T">Only unmanaged value types are supported (e.g. int, float, Vector3, MyUnmanagedStruct).</typeparam>
[DebuggerDisplay("Length = {length}")]
public unsafe struct ArenaArray<T> where T : unmanaged
{
    [NativeDisableUnsafePtrRestriction] private void* data;
    private int length;

    public ArenaArray(ArenaAllocator* arena, int length, string tag = "ArenaArray")
    {
        if (!UnsafeUtility.IsUnmanaged<T>())
        {
            throw new InvalidOperationException($"ArenaArray<T> requires T to be unmanaged. Type {typeof(T)} is not.");
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be greater than zero.");
        }

        int totalSize = UnsafeUtility.SizeOf<T>() * length;
        int alignment = ArenaUtil.GetNextPowerOfTwo(UnsafeUtility.SizeOf<T>());

        data = arena->Allocate(totalSize, alignment, tag);

        if (data == null)
        {
            throw new InvalidOperationException("Arena allocation failed for ArenaArray.");
        }

        this.length = length;
    }

    public int Length => length;
    public bool IsCreated => data != null;
    public void* GetRawPtr() => data;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            var val = UnsafeUtility.ReadArrayElement<T>(data, index);   
            return val;
        }
        set
        {
            if (index < 0 || index >= length)
            {
                throw new IndexOutOfRangeException();
            }

            UnsafeUtility.WriteArrayElement(data, index, value);
        }
    }

    public void CopyFrom(T[] source)
    {
        if (source == null) { throw new ArgumentNullException(nameof(source)); }
        if (source.Length != length)
        {
            throw new ArgumentException("Source array length does not match ArenaArray length.");
        }

        for (int i = 0; i < length; i++)
        {
            UnsafeUtility.WriteArrayElement(data, i, source[i]);
        }
    }

    public void CopyTo(T[] destination)
    {
        if (destination == null) { throw new ArgumentNullException(nameof(destination)); }
        if (destination.Length != length)
        {
            throw new ArgumentException("Destination array length does not match ArenaArray length.");
        }

        for (int i = 0; i < length; i++)
        {
            destination[i] = UnsafeUtility.ReadArrayElement<T>(data, i);
        }
    }

    /// <summary>
    /// WARNING: Incompatible with Burst or class usage. See GetBurstSafeEnumerator() for that functionality.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(this);
    public ref struct Enumerator
    {
        private ArenaArray<T> array;
        private int index;

        public Enumerator(ArenaArray<T> array)
        {
            this.array = array;
            this.index = -1;
        }

        public bool MoveNext()
        {
            index++;
            return index < array.length;
        }

        public T Current => array[index];
    }

    /// <summary>
    /// WARNING: Incompatible with for/foreach — requires manual iteration (e.g. var enumerator = myArray.GetBurstSafeEnumerator(); then:
    /// while (enumerator.MoveNext()) { ... }).
    /// </summary>
    public BurstSafeEnumerator GetBurstSafeEnumerator() { return new BurstSafeEnumerator(data, length); }
    public struct BurstSafeEnumerator
    {
        private void* data;
        private int length;
        private int index;

        public BurstSafeEnumerator(void* data, int length)
        {
            this.data = data;
            this.length = length;
            index = -1;
        }

        public bool MoveNext() => ++index < length;
        public T Current => UnsafeUtility.ReadArrayElement<T>(data, index);
    }
}