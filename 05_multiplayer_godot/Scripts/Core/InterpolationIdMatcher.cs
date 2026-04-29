namespace SwarmSurvivor;

using System.Collections.Generic;

public static class InterpolationIdMatcher
{
    // Builds a dictionary mapping previousFrame's IDs to their indices.
    // Caller fills the provided dictionary; this avoids allocation per snapshot
    // by reusing the same Dictionary instance.
    public static void BuildIdToIndex(
        int[] previousIds,
        int previousCount,
        Dictionary<int, int> outIdToIndex)
    {
        outIdToIndex.Clear();
        for (int j = 0; j < previousCount; j++)
        {
            outIdToIndex[previousIds[j]] = j;
        }
    }

    // Looks up oldIndex for a given ID. Returns -1 if not found.
    public static int FindOldIndex(
        int currentId,
        Dictionary<int, int> idToIndex)
    {
        return idToIndex.TryGetValue(currentId, out var idx) ? idx : -1;
    }
}
