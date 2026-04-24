using Xunit;

namespace SwarmSurvivor.Tests;

public class SwarmCalculatorTest
{
    [Fact]
    public void CreateEntity_SetsCorrectDefaults()
    {
        var entity = SwarmCalculator.CreateEntity(5f, 10f, 100, 2);

        Assert.Equal(5f, entity.PositionX);
        Assert.Equal(10f, entity.PositionZ);
        Assert.Equal(100, entity.Health);
        Assert.Equal(100, entity.MaxHealth);
        Assert.Equal(2, entity.EnemyTypeIndex);
        Assert.Equal(0f, entity.DamageFlashTimer);
        Assert.True(SwarmCalculator.IsAlive(entity.DeathTimer));
    }

    [Fact]
    public void IsAlive_ReturnsTrueForNegativeTimer()
    {
        Assert.True(SwarmCalculator.IsAlive(-1f));
    }

    [Fact]
    public void IsAlive_ReturnsFalseForZeroTimer()
    {
        Assert.False(SwarmCalculator.IsAlive(0f));
    }

    [Fact]
    public void IsDying_ReturnsTrueForZeroOrPositiveTimer()
    {
        Assert.True(SwarmCalculator.IsDying(0f));
        Assert.True(SwarmCalculator.IsDying(0.5f));
    }

    [Fact]
    public void IsDying_ReturnsFalseForNegativeTimer()
    {
        Assert.False(SwarmCalculator.IsDying(-1f));
    }

    [Fact]
    public void MoveToward_MovesInCorrectDirection()
    {
        var result = SwarmCalculator.MoveToward(0f, 0f, 10f, 0f, 5f, 1f);

        Assert.True(result.NewX > 0f);
        Assert.Equal(0f, result.NewZ, 3);
        Assert.True(result.VelX > 0f);
    }

    [Fact]
    public void MoveToward_RespectsSpeed()
    {
        var result = SwarmCalculator.MoveToward(0f, 0f, 100f, 0f, 3f, 1f);

        Assert.Equal(3f, result.NewX, 2);
        Assert.Equal(3f, result.VelX, 2);
    }

    [Fact]
    public void MoveToward_StopsAtTarget()
    {
        var result = SwarmCalculator.MoveToward(5f, 5f, 5f, 5f, 10f, 1f);

        Assert.Equal(5f, result.NewX);
        Assert.Equal(5f, result.NewZ);
        Assert.Equal(0f, result.VelX);
        Assert.Equal(0f, result.VelZ);
    }

    [Fact]
    public void MoveToward_ScalesWithDeltaTime()
    {
        var full = SwarmCalculator.MoveToward(0f, 0f, 10f, 0f, 5f, 1f);
        var half = SwarmCalculator.MoveToward(0f, 0f, 10f, 0f, 5f, 0.5f);

        Assert.Equal(full.NewX * 0.5f, half.NewX, 2);
    }

    [Fact]
    public void MoveToward_HandlesDiagonalMovement()
    {
        var result = SwarmCalculator.MoveToward(0f, 0f, 10f, 10f, 1f, 1f);

        Assert.True(result.NewX > 0f);
        Assert.True(result.NewZ > 0f);
        Assert.Equal(result.NewX, result.NewZ, 3);
    }

    [Fact]
    public void CalculateSeparation_PushesAway()
    {
        var result = SwarmCalculator.CalculateSeparation(
            5f, 5f, 5.5f, 5f, 2f, 10f);

        Assert.True(result.VelX < 0f);
        Assert.Equal(0f, result.VelZ, 2);
    }

    [Fact]
    public void CalculateSeparation_NoEffectBeyondRadius()
    {
        var result = SwarmCalculator.CalculateSeparation(
            0f, 0f, 10f, 0f, 2f, 10f);

        Assert.Equal(0f, result.VelX);
        Assert.Equal(0f, result.VelZ);
    }

    [Fact]
    public void CalculateSeparation_StrongerWhenCloser()
    {
        var close = SwarmCalculator.CalculateSeparation(
            0f, 0f, 0.5f, 0f, 2f, 10f);
        var far = SwarmCalculator.CalculateSeparation(
            0f, 0f, 1.5f, 0f, 2f, 10f);

        Assert.True(MathF.Abs(close.VelX) > MathF.Abs(far.VelX));
    }

    [Fact]
    public void CalculateSeparation_NoEffectAtSamePosition()
    {
        var result = SwarmCalculator.CalculateSeparation(
            5f, 5f, 5f, 5f, 2f, 10f);

        Assert.Equal(0f, result.VelX);
        Assert.Equal(0f, result.VelZ);
    }

    [Fact]
    public void TakeDamage_ReducesHealth()
    {
        var result = SwarmCalculator.TakeDamage(100, 30, 0.15f);

        Assert.Equal(70, result.NewHealth);
        Assert.False(result.IsDead);
        Assert.Equal(0.15f, result.FlashTimer);
    }

    [Fact]
    public void TakeDamage_KillsAtZero()
    {
        var result = SwarmCalculator.TakeDamage(30, 30, 0.15f);

        Assert.Equal(0, result.NewHealth);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void TakeDamage_ClampsToZero()
    {
        var result = SwarmCalculator.TakeDamage(10, 999, 0.15f);

        Assert.Equal(0, result.NewHealth);
        Assert.True(result.IsDead);
    }

    [Fact]
    public void TickDeath_ProgressesTimer()
    {
        var result = SwarmCalculator.TickDeath(0f, 0.1f, 0.5f);

        Assert.Equal(0.1f, result.NewDeathTimer, 3);
        Assert.False(result.ShouldRemove);
    }

    [Fact]
    public void TickDeath_SignalsRemoveWhenComplete()
    {
        var result = SwarmCalculator.TickDeath(0.4f, 0.2f, 0.5f);

        Assert.True(result.ShouldRemove);
    }

    [Fact]
    public void DistanceSquared_ReturnsCorrectValue()
    {
        float result = SwarmCalculator.DistanceSquared(0f, 0f, 3f, 4f);

        Assert.Equal(25f, result, 3);
    }

    [Fact]
    public void DistanceSquared_ZeroForSamePoint()
    {
        float result = SwarmCalculator.DistanceSquared(5f, 5f, 5f, 5f);

        Assert.Equal(0f, result);
    }

    [Fact]
    public void ClampToArena_ClampsOverflow()
    {
        Assert.Equal(20f, SwarmCalculator.ClampToArena(25f, 20f));
        Assert.Equal(-20f, SwarmCalculator.ClampToArena(-25f, 20f));
    }

    [Fact]
    public void ClampToArena_PreservesInRange()
    {
        Assert.Equal(10f, SwarmCalculator.ClampToArena(10f, 20f));
    }

    [Fact]
    public void NormalizeDeathProgress_ReturnsZeroAtStart()
    {
        Assert.Equal(0f, SwarmCalculator.NormalizeDeathProgress(0f, 0.5f));
    }

    [Fact]
    public void NormalizeDeathProgress_ReturnsOneAtEnd()
    {
        Assert.Equal(1f, SwarmCalculator.NormalizeDeathProgress(0.5f, 0.5f));
    }

    [Fact]
    public void NormalizeDeathProgress_ClampsOverflow()
    {
        Assert.Equal(1f, SwarmCalculator.NormalizeDeathProgress(1f, 0.5f));
    }

    [Fact]
    public void NormalizeDeathProgress_HandlesZeroDuration()
    {
        Assert.Equal(1f, SwarmCalculator.NormalizeDeathProgress(0f, 0f));
    }

    [Fact]
    public void TickFlash_DecrementsTimer()
    {
        Assert.Equal(0.05f, SwarmCalculator.TickFlash(0.15f, 0.1f), 3);
    }

    [Fact]
    public void TickFlash_ClampsToZero()
    {
        Assert.Equal(0f, SwarmCalculator.TickFlash(0.05f, 0.1f));
    }
}
