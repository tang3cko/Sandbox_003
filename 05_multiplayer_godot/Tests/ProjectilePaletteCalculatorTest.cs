using Xunit;

namespace SwarmSurvivor.Tests;

public class ProjectilePaletteCalculatorTest
{
    [Fact]
    public void WrapPaletteIndex_Zero_ReturnsZero()
    {
        Assert.Equal(0, ProjectilePaletteCalculator.WrapPaletteIndex(0, 8));
    }

    [Fact]
    public void WrapPaletteIndex_PositiveBelowSize_ReturnsAsIs()
    {
        Assert.Equal(3, ProjectilePaletteCalculator.WrapPaletteIndex(3, 8));
    }

    [Fact]
    public void WrapPaletteIndex_AtSize_WrapsToZero()
    {
        Assert.Equal(0, ProjectilePaletteCalculator.WrapPaletteIndex(8, 8));
    }

    [Fact]
    public void WrapPaletteIndex_AboveSize_Wraps()
    {
        Assert.Equal(2, ProjectilePaletteCalculator.WrapPaletteIndex(10, 8));
    }

    [Fact]
    public void WrapPaletteIndex_FarAboveSize_Wraps()
    {
        Assert.Equal(4, ProjectilePaletteCalculator.WrapPaletteIndex(100, 8));
    }

    [Fact]
    public void WrapPaletteIndex_NegativeOne_WrapsToSizeMinusOne()
    {
        Assert.Equal(7, ProjectilePaletteCalculator.WrapPaletteIndex(-1, 8));
    }

    [Fact]
    public void WrapPaletteIndex_NegativeFar_Wraps()
    {
        Assert.Equal(1, ProjectilePaletteCalculator.WrapPaletteIndex(-15, 8));
    }

    [Fact]
    public void WrapPaletteIndex_DefaultSizeOverload_UsesEight()
    {
        Assert.Equal(ProjectilePaletteCalculator.WrapPaletteIndex(10, 8),
            ProjectilePaletteCalculator.WrapPaletteIndex(10));
        Assert.Equal(7, ProjectilePaletteCalculator.WrapPaletteIndex(-1));
        Assert.Equal(8, ProjectilePaletteCalculator.PaletteSize);
    }

    [Fact]
    public void WrapPaletteIndex_PaletteSizeZero_ReturnsZeroDefensive()
    {
        Assert.Equal(0, ProjectilePaletteCalculator.WrapPaletteIndex(5, 0));
    }

    [Fact]
    public void WrapPaletteIndex_PaletteSizeNegative_ReturnsZeroDefensive()
    {
        Assert.Equal(0, ProjectilePaletteCalculator.WrapPaletteIndex(5, -3));
    }

    [Fact]
    public void WrapPaletteIndex_CustomSize_RespectsParameter()
    {
        Assert.Equal(2, ProjectilePaletteCalculator.WrapPaletteIndex(5, 3));
    }
}
