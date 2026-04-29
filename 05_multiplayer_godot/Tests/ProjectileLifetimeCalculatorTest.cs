using Xunit;

namespace SwarmSurvivor.Tests;

public class ProjectileLifetimeCalculatorTest
{
    [Fact]
    public void Step_ZeroDelta_NoMovement()
    {
        var result = ProjectileLifetimeCalculator.Step(5f, 10f, 1f, 0f, 20f, 0f);

        Assert.Equal(5f, result.NewX);
        Assert.Equal(10f, result.NewZ);
    }

    [Fact]
    public void Step_PositiveDelta_AdvancesAlongDirection()
    {
        var result = ProjectileLifetimeCalculator.Step(0f, 0f, 1f, 0f, 5f, 1f);

        Assert.Equal(5f, result.NewX, 3);
        Assert.Equal(0f, result.NewZ, 3);
    }

    [Fact]
    public void Step_NegativeDirection_MovesBackward()
    {
        var result = ProjectileLifetimeCalculator.Step(10f, 10f, -1f, -1f, 2f, 0.5f);

        Assert.Equal(9f, result.NewX, 3);
        Assert.Equal(9f, result.NewZ, 3);
    }

    [Fact]
    public void Step_HighSpeed_ScalesMovement()
    {
        var result = ProjectileLifetimeCalculator.Step(0f, 0f, 0f, 1f, 100f, 0.1f);

        Assert.Equal(0f, result.NewX, 3);
        Assert.Equal(10f, result.NewZ, 3);
    }

    [Fact]
    public void Step_DiagonalDirection_AppliesBothComponents()
    {
        var result = ProjectileLifetimeCalculator.Step(1f, 2f, 0.5f, 0.5f, 4f, 0.5f);

        Assert.Equal(2f, result.NewX, 3);
        Assert.Equal(3f, result.NewZ, 3);
    }

    [Fact]
    public void TickLifetime_AboveZero_Decrements()
    {
        Assert.Equal(0.4f, ProjectileLifetimeCalculator.TickLifetime(0.5f, 0.1f), 3);
    }

    [Fact]
    public void TickLifetime_AtZero_ReturnsZero()
    {
        Assert.Equal(0f, ProjectileLifetimeCalculator.TickLifetime(0f, 0.1f));
    }

    [Fact]
    public void TickLifetime_BelowDt_ClampsToZero()
    {
        Assert.Equal(0f, ProjectileLifetimeCalculator.TickLifetime(0.05f, 0.2f));
    }

    [Fact]
    public void TickLifetime_ExactlyDt_ReturnsZero()
    {
        Assert.Equal(0f, ProjectileLifetimeCalculator.TickLifetime(0.1f, 0.1f), 3);
    }

    [Fact]
    public void IsLifetimeExpired_Positive_ReturnsFalse()
    {
        Assert.False(ProjectileLifetimeCalculator.IsLifetimeExpired(0.01f));
        Assert.False(ProjectileLifetimeCalculator.IsLifetimeExpired(5f));
    }

    [Fact]
    public void IsLifetimeExpired_Zero_ReturnsTrue()
    {
        Assert.True(ProjectileLifetimeCalculator.IsLifetimeExpired(0f));
    }

    [Fact]
    public void IsLifetimeExpired_Negative_ReturnsTrue()
    {
        Assert.True(ProjectileLifetimeCalculator.IsLifetimeExpired(-0.001f));
        Assert.True(ProjectileLifetimeCalculator.IsLifetimeExpired(-10f));
    }

    [Fact]
    public void ComputeInitialLifetime_ZeroSpeed_ReturnsZero()
    {
        Assert.Equal(0f, ProjectileLifetimeCalculator.ComputeInitialLifetime(20f, 0f));
    }

    [Fact]
    public void ComputeInitialLifetime_PositiveSpeedAndRange_ReturnsRangeOverSpeed()
    {
        Assert.Equal(4f, ProjectileLifetimeCalculator.ComputeInitialLifetime(20f, 5f), 3);
    }

    [Fact]
    public void ComputeInitialLifetime_NegativeSpeed_ReturnsZero()
    {
        Assert.Equal(0f, ProjectileLifetimeCalculator.ComputeInitialLifetime(20f, -5f));
    }

    [Fact]
    public void DefaultHitRadius_IsExpectedValue()
    {
        Assert.Equal(0.8f, ProjectileLifetimeCalculator.DefaultHitRadius);
    }
}
