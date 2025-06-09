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
            ArenaLog.Log("A non-fatal allocation issue occurred. See assertion details above.", ArenaLog.Level.Warning);
        }

        return isValid;
    }
}
