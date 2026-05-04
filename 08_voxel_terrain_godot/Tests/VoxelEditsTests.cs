using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class VoxelEditsTests
{
    [Fact]
    public void FillFlatGround_LayersStoneDirtGrassAirCorrectly()
    {
        var data = new ChunkData(8);
        VoxelEdits.FillFlatGround(data, groundY: 5, MaterialId.Stone, MaterialId.Dirt, MaterialId.Grass, dirtThickness: 2);

        Assert.Equal(MaterialId.Stone, data.Get(0, 0, 0));
        Assert.Equal(MaterialId.Stone, data.Get(0, 2, 0));
        Assert.Equal(MaterialId.Dirt, data.Get(0, 3, 0));
        Assert.Equal(MaterialId.Dirt, data.Get(0, 4, 0));
        Assert.Equal(MaterialId.Grass, data.Get(0, 5, 0));
        Assert.Equal(MaterialId.Air, data.Get(0, 6, 0));
        Assert.Equal(MaterialId.Air, data.Get(0, 7, 0));
    }

    [Fact]
    public void DigSphere_RemovesVoxelsInsideRadius()
    {
        var data = new ChunkData(8);
        for (int z = 0; z < 8; z++)
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            data.Set(x, y, z, MaterialId.Stone);

        int changed = VoxelEdits.DigSphere(data, 4f, 4f, 4f, 1.5f);

        Assert.True(changed > 0);
        Assert.Equal(MaterialId.Air, data.Get(4, 4, 4));
        Assert.Equal(MaterialId.Stone, data.Get(0, 0, 0));
    }

    [Fact]
    public void DigSphere_OnAlreadyAir_ReturnsZero()
    {
        var data = new ChunkData(8);
        int changed = VoxelEdits.DigSphere(data, 4f, 4f, 4f, 2f);
        Assert.Equal(0, changed);
    }

    [Fact]
    public void FillSphere_PlacesMaterialInsideRadius()
    {
        var data = new ChunkData(8);
        int changed = VoxelEdits.FillSphere(data, 4f, 4f, 4f, 1f, MaterialId.Stone);
        Assert.True(changed > 0);
        Assert.Equal(MaterialId.Stone, data.Get(4, 4, 4));
    }

    [Fact]
    public void FillSphere_NegativeRadius_IsNoOp()
    {
        var data = new ChunkData(4);
        Assert.Equal(0, VoxelEdits.FillSphere(data, 2f, 2f, 2f, -1f, MaterialId.Stone));
    }

    [Fact]
    public void SetVoxelAt_FloorsCoordinates()
    {
        var data = new ChunkData(8);
        Assert.True(VoxelEdits.SetVoxelAt(data, 2.7f, 3.1f, 4.9f, MaterialId.Stone));
        Assert.Equal(MaterialId.Stone, data.Get(2, 3, 4));
        Assert.Equal(MaterialId.Air, data.Get(3, 3, 4));
    }

    [Fact]
    public void SetVoxelAt_OutOfBounds_ReturnsFalse()
    {
        var data = new ChunkData(4);
        Assert.False(VoxelEdits.SetVoxelAt(data, -0.5f, 0f, 0f, MaterialId.Stone));
        Assert.False(VoxelEdits.SetVoxelAt(data, 4f, 0f, 0f, MaterialId.Stone));
    }

    [Fact]
    public void SetVoxelAt_AlreadySameMaterial_ReturnsFalse()
    {
        var data = new ChunkData(4);
        VoxelEdits.SetVoxelAt(data, 1.5f, 1.5f, 1.5f, MaterialId.Stone);
        Assert.False(VoxelEdits.SetVoxelAt(data, 1.5f, 1.5f, 1.5f, MaterialId.Stone));
    }
}
