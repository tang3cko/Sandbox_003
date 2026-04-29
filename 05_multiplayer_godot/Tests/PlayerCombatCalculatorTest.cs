using Xunit;

namespace SwarmSurvivor.Tests;

public class PlayerCombatCalculatorTest
{
    [Fact]
    public void IsInvincible_BeforeWindow_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.IsInvincible(0.0, 0.0));
    }

    [Fact]
    public void IsInvincible_DuringWindow_ReturnsTrue()
    {
        Assert.True(PlayerCombatCalculator.IsInvincible(1.0, 1.4));
    }

    [Fact]
    public void IsInvincible_AtBoundary_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.IsInvincible(1.4, 1.4));
    }

    [Fact]
    public void IsInvincible_AfterWindow_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.IsInvincible(2.0, 1.4));
    }

    [Fact]
    public void NextInvincibleUntil_AddsDuration()
    {
        Assert.Equal(5.5, PlayerCombatCalculator.NextInvincibleUntil(5.0), 5);
    }

    [Fact]
    public void NextInvincibleUntil_DurationIsHalfSecond()
    {
        Assert.Equal(0.5f, PlayerCombatCalculator.InvincibilityDuration);
    }

    [Fact]
    public void Sequence_TwoHitsWithinWindow_SecondGated()
    {
        double firstHitTime = 1.0;
        double invincibleUntil = PlayerCombatCalculator.NextInvincibleUntil(firstHitTime);

        Assert.Equal(1.5, invincibleUntil, 5);
        Assert.True(PlayerCombatCalculator.IsInvincible(1.3, invincibleUntil));
    }

    [Fact]
    public void RespawnInvincibilityDuration_IsTwoSeconds()
    {
        Assert.Equal(2.0f, PlayerCombatCalculator.RespawnInvincibilityDuration);
    }

    [Fact]
    public void NextRespawnInvincibleUntil_AddsRespawnDuration()
    {
        Assert.Equal(7.0, PlayerCombatCalculator.NextRespawnInvincibleUntil(5.0), 5);
    }

    [Fact]
    public void NextRespawnInvincibleUntil_LongerThanNormalIframe()
    {
        Assert.True(PlayerCombatCalculator.RespawnInvincibilityDuration > PlayerCombatCalculator.InvincibilityDuration);
    }

    [Fact]
    public void ComputeRespawnHealth_FullFraction_ReturnsMax()
    {
        Assert.Equal(100, PlayerCombatCalculator.ComputeRespawnHealth(100, 1.0f));
    }

    [Fact]
    public void ComputeRespawnHealth_HalfFraction_ReturnsHalf()
    {
        Assert.Equal(50, PlayerCombatCalculator.ComputeRespawnHealth(100, 0.5f));
    }

    [Fact]
    public void ComputeRespawnHealth_ZeroFraction_ReturnsZero()
    {
        Assert.Equal(0, PlayerCombatCalculator.ComputeRespawnHealth(100, 0f));
    }

    [Fact]
    public void ComputeRespawnHealth_FractionAboveOne_ClampsToMax()
    {
        Assert.Equal(100, PlayerCombatCalculator.ComputeRespawnHealth(100, 1.5f));
    }

    [Fact]
    public void ComputeRespawnHealth_NegativeFraction_ClampsToZero()
    {
        Assert.Equal(0, PlayerCombatCalculator.ComputeRespawnHealth(100, -0.5f));
    }

    [Fact]
    public void CanRespawn_PositiveRespawns_ReturnsTrue()
    {
        Assert.True(PlayerCombatCalculator.CanRespawn(100, 3));
    }

    [Fact]
    public void CanRespawn_ZeroRespawns_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.CanRespawn(100, 0));
    }

    [Fact]
    public void CanRespawn_NegativeRespawns_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.CanRespawn(100, -1));
    }

    [Fact]
    public void CanRespawn_ZeroMaxHealth_ReturnsFalse()
    {
        Assert.False(PlayerCombatCalculator.CanRespawn(0, 3));
    }
}
