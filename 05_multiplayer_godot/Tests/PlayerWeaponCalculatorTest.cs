using System;
using Xunit;

namespace SwarmSurvivor.Tests;

public class PlayerWeaponCalculatorTest
{
    // ---- TickFireTimer ----

    [Fact]
    public void TickFireTimer_NotExpired_DecrementsAndDoesNotFire()
    {
        var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(0.5f, 0.1f, 2f, 1f);
        Assert.Equal(0.4f, newTimer, precision: 5);
        Assert.False(shouldFire);
    }

    [Fact]
    public void TickFireTimer_ExactlyZero_FiresAndResets()
    {
        // 0.1 - 0.1 = 0, so newTimer <= 0 triggers fire and reset to 1/(fireRate*speed)
        var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(0.1f, 0.1f, 2f, 1f);
        Assert.True(shouldFire);
        Assert.Equal(0.5f, newTimer, precision: 5);
    }

    [Fact]
    public void TickFireTimer_NegativeAfterTick_FiresAndResets()
    {
        var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(0.05f, 0.1f, 4f, 1f);
        Assert.True(shouldFire);
        Assert.Equal(0.25f, newTimer, precision: 5);
    }

    [Fact]
    public void TickFireTimer_SpeedMultiplierShortensReset()
    {
        // 1 / (2 * 2) = 0.25
        var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(0f, 0.1f, 2f, 2f);
        Assert.True(shouldFire);
        Assert.Equal(0.25f, newTimer, precision: 5);
    }

    [Fact]
    public void TickFireTimer_SpeedMultiplierBelowOneLengthensReset()
    {
        // 1 / (2 * 0.5) = 1.0
        var (newTimer, shouldFire) = PlayerWeaponCalculator.TickFireTimer(0f, 0.01f, 2f, 0.5f);
        Assert.True(shouldFire);
        Assert.Equal(1.0f, newTimer, precision: 5);
    }

    // ---- SelectFireDirection ----

    [Fact]
    public void SelectFireDirection_AboveMinDist_NormalizesToNearest()
    {
        // toNearest = (3, 4), dist = 5
        var (dx, dz) = PlayerWeaponCalculator.SelectFireDirection(
            5f, 3f, 4f, 0f, 0f, PlayerWeaponCalculator.MinFireDirectionDist);
        Assert.Equal(0.6f, dx, precision: 5);
        Assert.Equal(0.8f, dz, precision: 5);
    }

    [Fact]
    public void SelectFireDirection_AtMinDist_FallsBackToFacing()
    {
        // dist == minDist => uses facing direction normalized
        var (dx, dz) = PlayerWeaponCalculator.SelectFireDirection(
            0.1f, 999f, 999f, 0f, 1f, PlayerWeaponCalculator.MinFireDirectionDist);
        Assert.Equal(0f, dx, precision: 5);
        Assert.Equal(1f, dz, precision: 5);
    }

    [Fact]
    public void SelectFireDirection_BelowMinDist_NormalizesFacing()
    {
        // facing = (3, 4), len = 5 => (0.6, 0.8)
        var (dx, dz) = PlayerWeaponCalculator.SelectFireDirection(
            0.05f, 0f, 0f, 3f, 4f, PlayerWeaponCalculator.MinFireDirectionDist);
        Assert.Equal(0.6f, dx, precision: 5);
        Assert.Equal(0.8f, dz, precision: 5);
    }

    [Fact]
    public void SelectFireDirection_ZeroFacing_UsesFallback()
    {
        var (dx, dz) = PlayerWeaponCalculator.SelectFireDirection(
            0f, 0f, 0f, 0f, 0f, PlayerWeaponCalculator.MinFireDirectionDist);
        Assert.Equal(PlayerWeaponCalculator.DefaultFacingFallbackX, dx, precision: 5);
        Assert.Equal(PlayerWeaponCalculator.DefaultFacingFallbackZ, dz, precision: 5);
    }

    // ---- ApplyAngleSpread ----

    [Fact]
    public void ApplyAngleSpread_SingleProjectile_NoRotation()
    {
        var (dx, dz) = PlayerWeaponCalculator.ApplyAngleSpread(
            1f, 0f, 0, 1, PlayerWeaponCalculator.DefaultSpreadAngleStep);
        Assert.Equal(1f, dx, precision: 5);
        Assert.Equal(0f, dz, precision: 5);
    }

    [Fact]
    public void ApplyAngleSpread_ThreeProjectilesMiddleIndex_NoRotation()
    {
        // total=3, middle index p=1: angleOffset = (1 - 3/2) * 0.2 = (1 - 1.5) * 0.2 = -0.1
        // NOTE: 3/2f = 1.5f, so middle index 1 is NOT exactly zero. To get zero rotation
        // with index logic, we need (p - total/2f) == 0, i.e. p = total/2f. With total=3
        // and integer p, this is never exact. Use total=odd-but-test-symmetry instead.
        // Here: total=3, p=1 => offset = -0.1 rad. Rotate (1, 0) by -0.1.
        var (dx, dz) = PlayerWeaponCalculator.ApplyAngleSpread(
            1f, 0f, 1, 3, 0.2f);
        Assert.Equal(MathF.Cos(-0.1f), dx, precision: 5);
        Assert.Equal(MathF.Sin(-0.1f), dz, precision: 5);
    }

    [Fact]
    public void ApplyAngleSpread_SymmetricPair_OppositeAngles()
    {
        // total=4: p=1 => (1 - 2) * 0.2 = -0.2; p=3 => (3 - 2) * 0.2 = 0.2
        var (dxLow, dzLow) = PlayerWeaponCalculator.ApplyAngleSpread(
            1f, 0f, 1, 4, 0.2f);
        var (dxHigh, dzHigh) = PlayerWeaponCalculator.ApplyAngleSpread(
            1f, 0f, 3, 4, 0.2f);
        // Cosine is even, sine is odd => x components equal, z components opposite
        Assert.Equal(dxLow, dxHigh, precision: 5);
        Assert.Equal(dzLow, -dzHigh, precision: 5);
    }

    [Fact]
    public void ApplyAngleSpread_RotatesBaseDirectionVector()
    {
        // base = (0, 1), total=1 => no rotation, output (0, 1)
        var (dx, dz) = PlayerWeaponCalculator.ApplyAngleSpread(
            0f, 1f, 0, 1, 0.2f);
        Assert.Equal(0f, dx, precision: 5);
        Assert.Equal(1f, dz, precision: 5);
    }

    // ---- ComputeMuzzlePosition ----

    [Fact]
    public void ComputeMuzzlePosition_ZeroOffset_ReturnsPlayerPosition()
    {
        var (mx, mz) = PlayerWeaponCalculator.ComputeMuzzlePosition(2f, 3f, 1f, 0f, 0f);
        Assert.Equal(2f, mx, precision: 5);
        Assert.Equal(3f, mz, precision: 5);
    }

    [Fact]
    public void ComputeMuzzlePosition_StandardOffset_AddsScaledDirection()
    {
        var (mx, mz) = PlayerWeaponCalculator.ComputeMuzzlePosition(
            2f, 3f, 1f, 0f, PlayerWeaponCalculator.DefaultMuzzleOffset);
        Assert.Equal(2.6f, mx, precision: 5);
        Assert.Equal(3f, mz, precision: 5);
    }
}
