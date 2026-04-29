using Xunit;

namespace SwarmSurvivor.Tests;

public class InputDirectionCalculatorTest
{
    [Fact]
    public void NormalizeWithDeadzone_ZeroInput_ReturnsNoInput()
    {
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(0f, 0f);

        Assert.Equal(0f, dirX, precision: 5);
        Assert.Equal(0f, dirZ, precision: 5);
        Assert.False(hasInput);
    }

    [Fact]
    public void NormalizeWithDeadzone_BelowDeadzone_ReturnsNoInput()
    {
        // lenSq = 0.05^2 + 0.05^2 = 0.005 < 0.01
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(0.05f, 0.05f);

        Assert.Equal(0f, dirX, precision: 5);
        Assert.Equal(0f, dirZ, precision: 5);
        Assert.False(hasInput);
    }

    [Fact]
    public void NormalizeWithDeadzone_AboveDeadzone_NormalizesToUnitLength()
    {
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(1f, 0f);

        Assert.Equal(1f, dirX, precision: 5);
        Assert.Equal(0f, dirZ, precision: 5);
        Assert.True(hasInput);
    }

    [Fact]
    public void NormalizeWithDeadzone_DiagonalInput_NormalizesCorrectly()
    {
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(1f, 1f);

        Assert.Equal(0.70711f, dirX, precision: 5);
        Assert.Equal(0.70711f, dirZ, precision: 5);
        Assert.True(hasInput);
    }

    [Fact]
    public void NormalizeWithDeadzone_LargeInput_NormalizesToUnit()
    {
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(3f, 4f);

        Assert.Equal(0.6f, dirX, precision: 5);
        Assert.Equal(0.8f, dirZ, precision: 5);
        Assert.True(hasInput);
    }

    [Fact]
    public void NormalizeWithDeadzone_NegativeInput_NormalizesCorrectly()
    {
        var (dirX, dirZ, hasInput) = InputDirectionCalculator.NormalizeWithDeadzone(-1f, 0f);

        Assert.Equal(-1f, dirX, precision: 5);
        Assert.Equal(0f, dirZ, precision: 5);
        Assert.True(hasInput);
    }

    [Fact]
    public void ComputeVelocity_HasInput_AppliesSpeed()
    {
        var (velX, velZ) = InputDirectionCalculator.ComputeVelocity(1f, 0f, 6f, hasInput: true);

        Assert.Equal(6f, velX, precision: 5);
        Assert.Equal(0f, velZ, precision: 5);
    }

    [Fact]
    public void ComputeVelocity_NoInput_ReturnsZero()
    {
        var (velX, velZ) = InputDirectionCalculator.ComputeVelocity(1f, 0f, 6f, hasInput: false);

        Assert.Equal(0f, velX, precision: 5);
        Assert.Equal(0f, velZ, precision: 5);
    }

    [Fact]
    public void ComputeVelocity_ZeroSpeed_ReturnsZero()
    {
        var (velX, velZ) = InputDirectionCalculator.ComputeVelocity(1f, 0f, 0f, hasInput: true);

        Assert.Equal(0f, velX, precision: 5);
        Assert.Equal(0f, velZ, precision: 5);
    }

    [Fact]
    public void ComputeLookTarget_AddsDirectionToPlayer()
    {
        var (targetX, targetZ) = InputDirectionCalculator.ComputeLookTarget(5f, 3f, 1f, 0f);

        Assert.Equal(6f, targetX, precision: 5);
        Assert.Equal(3f, targetZ, precision: 5);
    }

    [Fact]
    public void ComputeLookTarget_ZeroDirection_ReturnsPlayer()
    {
        var (targetX, targetZ) = InputDirectionCalculator.ComputeLookTarget(5f, 3f, 0f, 0f);

        Assert.Equal(5f, targetX, precision: 5);
        Assert.Equal(3f, targetZ, precision: 5);
    }
}
