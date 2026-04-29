using System;

namespace SwarmSurvivor;

public static class PlayerCombatCalculator
{
    public const float InvincibilityDuration = 0.5f;
    public const float RespawnInvincibilityDuration = 2.0f;

    public static bool IsInvincible(double currentTimeSec, double invincibleUntilSec)
    {
        return currentTimeSec < invincibleUntilSec;
    }

    public static double NextInvincibleUntil(double currentTimeSec)
    {
        return currentTimeSec + InvincibilityDuration;
    }

    public static double NextRespawnInvincibleUntil(double currentTimeSec)
    {
        return currentTimeSec + RespawnInvincibilityDuration;
    }

    public static int ComputeRespawnHealth(int maxHealth, float healthFraction)
    {
        float fraction = Math.Clamp(healthFraction, 0f, 1f);
        return (int)(maxHealth * fraction);
    }

    public static bool CanRespawn(int maxHealth, int respawnsRemaining)
    {
        return maxHealth > 0 && respawnsRemaining > 0;
    }
}
