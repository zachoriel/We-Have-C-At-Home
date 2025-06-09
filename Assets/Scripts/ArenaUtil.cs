using UnityEngine;

public static class ArenaUtil
{
    /// <summary>
    /// Validates that a value is a power of two.
    /// Throws, asserts, or logs depending on usage context.
    /// </summary>
    public static bool ValidatePowerOfTwo(int value, string context, bool shouldThrow = false)
    {
        bool isValid = (value & (value - 1)) == 0;
        if (!isValid)
        {
            string message = $"ArenaAllocator: Invalid value in {context} ({value}) — must be a power of two.";

            if (shouldThrow)
            {
                throw new System.ArgumentException(message);
            }

            Debug.Assert(false, message);
            ArenaLog.Log("ArenaAllocator", "A non-fatal allocation issue occurred. See assertion details above.", ArenaLog.Level.Warning);
        }

        return isValid;
    }

    /// <summary>
    /// Calculates the next power of two greater than or equal to the given value.
    /// 
    /// This is useful for determining a safe memory alignment size,
    /// especially when aligning based on data size. For example:
    /// - Input: 5 → Output: 8
    /// - Input: 8 → Output: 8
    /// - Input: 13 → Output: 16
    /// 
    /// The method uses bitwise operations to efficiently "round up"
    /// to the next power of two, and includes an optional clamp (e.g. to 64)
    /// to avoid extreme over-alignment.
    /// 
    /// NOTE: The returned value will always be ≥ 1.
    /// </summary>
    public static int GetNextPowerOfTwo(int value)
    {
        if (value < 1) { return 1; }

        // Fast power-of-two round-up
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value |= value >> 32;
        value++;
        value = Mathf.Min(value, 64);

        return value;
    }
}
