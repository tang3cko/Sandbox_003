using System;

namespace SwarmSurvivor;

public static class ProjectileLifetimeCalculator
{
    public const float DefaultHitRadius = 0.8f;

    public static (float NewX, float NewZ) Step(
        float posX, float posZ, float dirX, float dirZ, float speed, float dt)
    {
        return (posX + dirX * speed * dt, posZ + dirZ * speed * dt);
    }

    public static float TickLifetime(float currentLifetime, float dt)
    {
        return MathF.Max(0f, currentLifetime - dt);
    }

    public static bool IsLifetimeExpired(float lifetime)
    {
        return lifetime <= 0f;
    }

    public static float ComputeInitialLifetime(float range, float speed)
    {
        return speed > 0f ? range / speed : 0f;
    }
}
