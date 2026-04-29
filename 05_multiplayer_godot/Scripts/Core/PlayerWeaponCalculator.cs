using System;

namespace SwarmSurvivor;

public static class PlayerWeaponCalculator
{
    public const float MinFireDirectionDist = 0.1f;
    public const float DefaultMuzzleOffset = 0.6f;
    public const float DefaultSpreadAngleStep = 0.2f;
    public const float DefaultFacingFallbackX = 1f;
    public const float DefaultFacingFallbackZ = 0f;
    // Skip firing when nearest target is farther than (weapon range * this multiplier).
    public const float FireRangeMultiplier = 2f;

    public static (float NewTimer, bool ShouldFire) TickFireTimer(
        float currentTimer, float dt, float fireRate, float speedMultiplier)
    {
        float newTimer = currentTimer - dt;
        if (newTimer <= 0f)
        {
            return (1f / (fireRate * speedMultiplier), true);
        }
        return (newTimer, false);
    }

    public static (float DirX, float DirZ) SelectFireDirection(
        float distToNearest, float toNearestX, float toNearestZ,
        float facingX, float facingZ, float minDist)
    {
        if (distToNearest <= minDist)
        {
            float facingLen = MathF.Sqrt(facingX * facingX + facingZ * facingZ);
            if (facingLen < 0.01f)
            {
                return (DefaultFacingFallbackX, DefaultFacingFallbackZ);
            }
            return (facingX / facingLen, facingZ / facingLen);
        }
        return (toNearestX / distToNearest, toNearestZ / distToNearest);
    }

    public static (float DirX, float DirZ) ApplyAngleSpread(
        float baseDirX, float baseDirZ,
        int projectileIndex, int totalProjectiles, float spreadAngleStep)
    {
        float angleOffset = totalProjectiles > 1
            ? (projectileIndex - totalProjectiles / 2f) * spreadAngleStep
            : 0f;
        float cos = MathF.Cos(angleOffset);
        float sin = MathF.Sin(angleOffset);
        float dirX = baseDirX * cos - baseDirZ * sin;
        float dirZ = baseDirX * sin + baseDirZ * cos;
        return (dirX, dirZ);
    }

    public static (float MuzzleX, float MuzzleZ) ComputeMuzzlePosition(
        float playerX, float playerZ, float dirX, float dirZ, float muzzleOffset)
    {
        return (playerX + dirX * muzzleOffset, playerZ + dirZ * muzzleOffset);
    }
}
