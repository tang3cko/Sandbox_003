namespace SwarmSurvivor;

using System.Collections.Generic;

public static class PlayerLifecycleCalculator
{
    // Given pairs of (isDead, isQueuedForDeletion), determines if all non-queued players are dead.
    // Returns false if there are zero non-queued players.
    public static bool AreAllPlayersDead(IEnumerable<(bool IsDead, bool IsQueuedForDeletion)> playerStates)
    {
        if (playerStates == null) return false;

        int total = 0;
        foreach (var state in playerStates)
        {
            if (state.IsQueuedForDeletion) continue;
            total++;
            if (!state.IsDead) return false;
        }
        return total > 0;
    }
}
