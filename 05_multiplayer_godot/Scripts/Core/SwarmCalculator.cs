using System;

namespace SwarmSurvivor;

public struct SwarmEntityState
{
    public float PositionX;
    public float PositionZ;
    public float VelocityX;
    public float VelocityZ;
    public int Health;
    public int MaxHealth;
    public int EnemyTypeIndex;
    public float DamageFlashTimer;
    public float DeathTimer;
}

public readonly record struct MoveResult(float NewX, float NewZ, float VelX, float VelZ);

public readonly record struct SeparationResult(float VelX, float VelZ);

public readonly record struct DamageEntityResult(int NewHealth, float FlashTimer, bool IsDead);

public readonly record struct DeathTickResult(float NewDeathTimer, bool ShouldRemove);

public static class SwarmCalculator
{
    public const float AliveMarker = -1f;
    public const float DefaultFlashDuration = 0.15f;
    public const float DefaultDeathDuration = 0.5f;

    public static SwarmEntityState CreateEntity(
        float posX, float posZ, int health, int enemyTypeIndex)
    {
        return new SwarmEntityState
        {
            PositionX = posX,
            PositionZ = posZ,
            VelocityX = 0f,
            VelocityZ = 0f,
            Health = health,
            MaxHealth = health,
            EnemyTypeIndex = enemyTypeIndex,
            DamageFlashTimer = 0f,
            DeathTimer = AliveMarker,
        };
    }

    public static bool IsAlive(float deathTimer)
    {
        return deathTimer < 0f;
    }

    public static bool IsDying(float deathTimer)
    {
        return deathTimer >= 0f;
    }

    public static MoveResult MoveToward(
        float posX, float posZ, float targetX, float targetZ, float speed, float dt)
    {
        float dx = targetX - posX;
        float dz = targetZ - posZ;
        float distSq = dx * dx + dz * dz;

        if (distSq < 0.0001f)
        {
            return new MoveResult(posX, posZ, 0f, 0f);
        }

        float dist = MathF.Sqrt(distSq);
        float nx = dx / dist;
        float nz = dz / dist;

        float velX = nx * speed;
        float velZ = nz * speed;
        float newX = posX + velX * dt;
        float newZ = posZ + velZ * dt;

        return new MoveResult(newX, newZ, velX, velZ);
    }

    public static SeparationResult CalculateSeparation(
        float posX, float posZ,
        float otherX, float otherZ,
        float separationRadius, float separationForce)
    {
        float dx = posX - otherX;
        float dz = posZ - otherZ;
        float distSq = dx * dx + dz * dz;
        float radiusSq = separationRadius * separationRadius;

        if (distSq >= radiusSq || distSq < 0.0001f)
        {
            return new SeparationResult(0f, 0f);
        }

        float dist = MathF.Sqrt(distSq);
        float nx = dx / dist;
        float nz = dz / dist;
        float strength = (1f - dist / separationRadius) * separationForce;

        return new SeparationResult(nx * strength, nz * strength);
    }

    public static DamageEntityResult TakeDamage(int currentHealth, int damage, float flashDuration)
    {
        int newHealth = Math.Max(0, currentHealth - damage);
        bool isDead = newHealth <= 0;
        return new DamageEntityResult(newHealth, flashDuration, isDead);
    }

    public static DeathTickResult TickDeath(float deathTimer, float dt, float deathDuration)
    {
        float newTimer = deathTimer + dt;
        bool shouldRemove = newTimer >= deathDuration;
        return new DeathTickResult(newTimer, shouldRemove);
    }

    public static float DistanceSquared(float ax, float az, float bx, float bz)
    {
        float dx = ax - bx;
        float dz = az - bz;
        return dx * dx + dz * dz;
    }

    public static int FindNearestTargetIndex(
        float fromX, float fromZ,
        float[] targetX, float[] targetZ,
        int targetCount)
    {
        if (targetCount <= 0) return -1;
        int nearest = 0;
        float nearestDistSq = DistanceSquared(fromX, fromZ, targetX[0], targetZ[0]);
        for (int i = 1; i < targetCount; i++)
        {
            float distSq = DistanceSquared(fromX, fromZ, targetX[i], targetZ[i]);
            if (distSq < nearestDistSq)
            {
                nearest = i;
                nearestDistSq = distSq;
            }
        }
        return nearest;
    }

    public static float ClampToArena(float value, float arenaHalfSize)
    {
        return Math.Clamp(value, -arenaHalfSize, arenaHalfSize);
    }

    public static float NormalizeDeathProgress(float deathTimer, float deathDuration)
    {
        if (deathDuration <= 0f) return 1f;
        return Math.Clamp(deathTimer / deathDuration, 0f, 1f);
    }

    public static float TickFlash(float flashTimer, float dt)
    {
        return Math.Max(0f, flashTimer - dt);
    }
}
