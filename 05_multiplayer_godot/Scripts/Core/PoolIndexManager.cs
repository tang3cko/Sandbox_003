namespace SwarmSurvivor;

// Generic pool-index manager that tracks free/in-use slots for any object pool.
// The pool's actual storage (arrays of nodes, materials, etc.) is owned by the
// caller; this class only manages a parallel bool[] for slot occupancy.
//
// Designed for future use cases such as pooling MuzzleFlashFX nodes
// (currently allocates 6 Godot objects per shot; ~360/min for 4 players).
// Intentionally has zero Godot dependencies so it remains pure and testable.
public static class PoolIndexManager
{
    // Sentinel returned when no slot is available.
    public const int UnallocatedIndex = -1;

    // Allocates a free slot from the pool by linear scan.
    // 'inUse' is modified in-place: the chosen slot is set to true.
    // Returns the index of the newly-allocated slot, or UnallocatedIndex (-1)
    // if the pool is full, null, or zero-length.
    public static int Allocate(bool[] inUse)
    {
        if (inUse == null)
        {
            return UnallocatedIndex;
        }

        for (int i = 0; i < inUse.Length; i++)
        {
            if (!inUse[i])
            {
                inUse[i] = true;
                return i;
            }
        }

        return UnallocatedIndex;
    }

    // Releases a slot back to the pool.
    // No-op if 'inUse' is null, index is out of range, or the slot is already free.
    public static void Release(bool[] inUse, int index)
    {
        if (inUse == null)
        {
            return;
        }

        if (index < 0 || index >= inUse.Length)
        {
            return;
        }

        if (inUse[index])
        {
            inUse[index] = false;
        }
    }

    // Counts how many slots are currently in use. Returns 0 for null arrays.
    public static int CountInUse(bool[] inUse)
    {
        if (inUse == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < inUse.Length; i++)
        {
            if (inUse[i])
            {
                count++;
            }
        }

        return count;
    }

    // Counts how many slots are currently free. Returns 0 for null arrays.
    public static int CountFree(bool[] inUse)
    {
        if (inUse == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < inUse.Length; i++)
        {
            if (!inUse[i])
            {
                count++;
            }
        }

        return count;
    }
}
