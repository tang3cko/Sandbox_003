namespace SwarmSurvivor.Tests;

using SwarmSurvivor;
using Xunit;

public class ScreenShakeCalculatorTest
{
    [Fact]
    public void CalculateIntensity_RemainingEqualsDuration_ReturnsFullIntensity()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(1f, 0.5f, 0.5f);
        Assert.Equal(1f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_RemainingZero_ReturnsZero()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(1f, 0f, 0.5f);
        Assert.Equal(0f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_RemainingHalfDuration_ReturnsHalfIntensity()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(1f, 0.25f, 0.5f);
        Assert.Equal(0.5f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_DurationOneSecond_LinearDecay()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(2f, 0.7f, 1.0f);
        Assert.Equal(1.4f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_DurationTwoSeconds_RespectsDuration()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(1f, 1.0f, 2.0f);
        Assert.Equal(0.5f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_ZeroInitial_ReturnsZero()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(0f, 0.5f, 1.0f);
        Assert.Equal(0f, result, precision: 5);
    }

    [Fact]
    public void CalculateIntensity_DurationZero_ReturnsZero()
    {
        float result = ScreenShakeCalculator.CalculateIntensity(1f, 0.5f, 0f);
        Assert.Equal(0f, result, precision: 5);
    }
}
