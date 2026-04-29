using System;

namespace SwarmSurvivor;

public static class ContactCalculator
{
    public const float DefaultContactRadius = 1.5f;
    public const float DefaultContactDamageInterval = 1.0f;

    public static float TickCooldown(float currentCooldown, float dt)
    {
        return MathF.Max(0f, currentCooldown - dt);
    }

    public static bool IsCooldownReady(float cooldown)
    {
        return cooldown <= 0f;
    }

    public static bool IsWithinContactRadius(
        float entityX, float entityZ,
        float targetX, float targetZ,
        float radius)
    {
        float dx = entityX - targetX;
        float dz = entityZ - targetZ;
        float distSq = dx * dx + dz * dz;
        float radiusSq = radius * radius;
        return distSq <= radiusSq;
    }
}
