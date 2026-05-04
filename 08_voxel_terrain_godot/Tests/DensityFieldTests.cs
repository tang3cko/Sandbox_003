using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class DensityFieldTests
{
    [Fact]
    public void NewField_AllSamplesAreZero()
    {
        var field = new DensityField(8);
        for (int z = 0; z < 8; z++)
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            Assert.Equal(0f, field.Get(x, y, z));
    }

    [Fact]
    public void Set_WithinBounds_PersistsValue()
    {
        var field = new DensityField(4);
        field.Set(1, 2, 3, 1.5f);
        Assert.Equal(1.5f, field.Get(1, 2, 3));
    }

    [Fact]
    public void Get_OutOfBounds_ReturnsAirSentinel()
    {
        var field = new DensityField(4);
        Assert.True(field.Get(-1, 0, 0) < 0f);
        Assert.True(field.Get(0, 4, 0) < 0f);
    }

    [Fact]
    public void IsSolid_OnlyTrueForPositiveValues()
    {
        var field = new DensityField(2);
        field.Set(0, 0, 0, 1f);
        field.Set(1, 0, 0, -1f);
        field.Set(0, 1, 0, 0f);
        Assert.True(field.IsSolid(0, 0, 0));
        Assert.False(field.IsSolid(1, 0, 0));
        Assert.False(field.IsSolid(0, 1, 0));
    }

    [Fact]
    public void Constructor_RejectsTooSmallSize()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DensityField(1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new DensityField(0));
    }
}
