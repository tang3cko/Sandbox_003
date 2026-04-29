namespace SwarmSurvivor;

public static class NearestEntityFinder
{
    public static int FindNearestAliveIndex(
        float fromX, float fromZ,
        float[] posX, float[] posZ,
        float[] deathTimer,
        int activeCount)
    {
        int nearest = -1;
        float nearestDistSq = float.MaxValue;

        for (int i = 0; i < activeCount; i++)
        {
            if (SwarmCalculator.IsDying(deathTimer[i])) continue;

            float distSq = SwarmCalculator.DistanceSquared(
                posX[i], posZ[i], fromX, fromZ);

            if (distSq < nearestDistSq)
            {
                nearest = i;
                nearestDistSq = distSq;
            }
        }

        return nearest;
    }

    public static (float X, float Z) FindNearestAlivePosition(
        float fromX, float fromZ,
        float[] posX, float[] posZ,
        float[] deathTimer,
        int activeCount)
    {
        int idx = FindNearestAliveIndex(fromX, fromZ, posX, posZ, deathTimer, activeCount);
        if (idx >= 0)
        {
            return (posX[idx], posZ[idx]);
        }
        return (fromX, fromZ);
    }
}
