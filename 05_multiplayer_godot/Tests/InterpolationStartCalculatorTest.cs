using Xunit;

namespace SwarmSurvivor.Tests;

public class InterpolationStartCalculatorTest
{
    [Fact]
    public void ComputeAlpha_ElapsedZero_ReturnsZero()
    {
        Assert.Equal(0f, InterpolationStartCalculator.ComputeAlpha(0f, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_ElapsedHalfInterval_ReturnsHalf()
    {
        Assert.Equal(0.5f, InterpolationStartCalculator.ComputeAlpha(0.025f, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_ElapsedEqualsInterval_ReturnsOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(0.05f, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_ElapsedGreaterThanInterval_ClampedToOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(0.5f, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_NegativeElapsed_ClampedToZero()
    {
        Assert.Equal(0f, InterpolationStartCalculator.ComputeAlpha(-0.01f, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_ZeroInterval_ReturnsOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(0.025f, 0f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_NegativeInterval_Defensive_ReturnsOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(0.025f, -0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_NaNElapsed_ReturnsOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(float.NaN, 0.05f), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_NaNInterval_ReturnsOne()
    {
        Assert.Equal(1f, InterpolationStartCalculator.ComputeAlpha(0.025f, float.NaN), precision: 5);
    }

    [Fact]
    public void ComputeAlpha_ResultAlwaysWithinUnitRange()
    {
        for (float e = -0.5f; e <= 1.5f; e += 0.1f)
        {
            float a = InterpolationStartCalculator.ComputeAlpha(e, 0.05f);
            Assert.InRange(a, 0f, 1f);
        }
    }

    [Fact]
    public void Lerp_AlphaZero_ReturnsStart()
    {
        Assert.Equal(10f, InterpolationStartCalculator.Lerp(10f, 20f, 0f), precision: 5);
    }

    [Fact]
    public void Lerp_AlphaOne_ReturnsEnd()
    {
        Assert.Equal(20f, InterpolationStartCalculator.Lerp(10f, 20f, 1f), precision: 5);
    }

    [Fact]
    public void Lerp_AlphaHalf_ReturnsMidpoint()
    {
        Assert.Equal(15f, InterpolationStartCalculator.Lerp(10f, 20f, 0.5f), precision: 5);
    }

    [Fact]
    public void Lerp_NegativeRange_Works()
    {
        Assert.Equal(-5f, InterpolationStartCalculator.Lerp(-10f, 0f, 0.5f), precision: 5);
    }
}
