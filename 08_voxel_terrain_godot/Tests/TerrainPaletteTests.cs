using System.Numerics;
using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class TerrainPaletteTests
{
    [Fact]
    public void TopVertex_ReturnsGrassColor()
    {
        var palette = new TerrainPalette();
        var color = palette.Sample(new Vector3(0, 10f, 0), referenceTopY: 10f);
        Assert.Equal(palette.GrassColor, color);
    }

    [Fact]
    public void MidDepth_ReturnsDirtColor()
    {
        var palette = new TerrainPalette();
        var color = palette.Sample(new Vector3(0, 8f, 0), referenceTopY: 10f);
        Assert.Equal(palette.DirtColor, color);
    }

    [Fact]
    public void DeepVertex_ReturnsStoneColor()
    {
        var palette = new TerrainPalette();
        var color = palette.Sample(new Vector3(0, 0f, 0), referenceTopY: 10f);
        Assert.Equal(palette.StoneColor, color);
    }
}
