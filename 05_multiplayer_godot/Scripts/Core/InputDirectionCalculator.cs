namespace SwarmSurvivor;

using System;

public static class InputDirectionCalculator
{
    public const float DeadzoneSquared = 0.01f;

    // Returns normalized (X, Z) direction with deadzone gate.
    // hasInput is true when input magnitude exceeds deadzone (caller decides whether to move).
    public static (float DirX, float DirZ, bool HasInput) NormalizeWithDeadzone(
        float inputX, float inputZ)
    {
        var lenSq = inputX * inputX + inputZ * inputZ;
        if (lenSq <= DeadzoneSquared)
        {
            return (0f, 0f, false);
        }

        var len = MathF.Sqrt(lenSq);
        return (inputX / len, inputZ / len, true);
    }

    // Computes velocity vector components from normalized direction and speed.
    // Returns (0, 0) if hasInput is false.
    public static (float VelX, float VelZ) ComputeVelocity(
        float normalizedX, float normalizedZ,
        float speed, bool hasInput)
    {
        if (!hasInput)
        {
            return (0f, 0f);
        }

        return (normalizedX * speed, normalizedZ * speed);
    }

    // Computes look target (X, Z) given player position and direction.
    public static (float TargetX, float TargetZ) ComputeLookTarget(
        float playerX, float playerZ,
        float directionX, float directionZ)
    {
        return (playerX + directionX, playerZ + directionZ);
    }
}
