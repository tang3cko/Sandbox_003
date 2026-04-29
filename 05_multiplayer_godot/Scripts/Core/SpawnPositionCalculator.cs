namespace SwarmSurvivor;

using System;

public static class SpawnPositionCalculator
{
    public const int PlayerSpawnSlots = 8;
    public const float PlayerSpawnRadius = 4f;

    /// <summary>
    /// Returns (x, z) on a circle at angle = (slotIndex * Tau / PlayerSpawnSlots).
    /// slotIndex is computed from peerId: serverPeerId → 0, others → peerId mod PlayerSpawnSlots.
    /// </summary>
    public static (float X, float Z) ComputePlayerSpawnPosition(long peerId, long serverPeerId)
    {
        int slotIndex = peerId == serverPeerId ? 0 : (int)(peerId % PlayerSpawnSlots);
        float angle = slotIndex * MathF.Tau / PlayerSpawnSlots;
        return (MathF.Cos(angle) * PlayerSpawnRadius, MathF.Sin(angle) * PlayerSpawnRadius);
    }

    /// <summary>
    /// Picks a spawn position on a circle around origin from pre-rolled [0,1) randoms.
    /// angle = rng01_angle * Tau. dist = minDist + rng01_dist * (maxDist - minDist).
    /// </summary>
    public static (float X, float Z) ComputeRandomSpawnPosition(
        double rng01_angle,
        double rng01_dist,
        float minDist,
        float maxDist)
    {
        float angle = (float)(rng01_angle * 2.0 * Math.PI);
        float dist = minDist + (float)rng01_dist * (maxDist - minDist);
        return (MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);
    }
}
