namespace SwarmSurvivor;

public static class SeparationForceCalculator
{
    // Computes the total separation force on entity 'selfIndex' from all other
    // non-dying entities. Skips j == selfIndex and dying entities.
    public static (float X, float Z) ComputeAccumulatedSeparation(
        int selfIndex,
        float[] posX, float[] posZ,
        float[] deathTimer,
        int activeCount,
        float separationRadius,
        float separationForce)
    {
        float sepX = 0f;
        float sepZ = 0f;

        for (int j = 0; j < activeCount; j++)
        {
            if (j == selfIndex) continue;
            if (SwarmCalculator.IsDying(deathTimer[j])) continue;

            var sep = SwarmCalculator.CalculateSeparation(
                posX[selfIndex], posZ[selfIndex],
                posX[j], posZ[j],
                separationRadius, separationForce);

            sepX += sep.VelX;
            sepZ += sep.VelZ;
        }

        return (sepX, sepZ);
    }
}
