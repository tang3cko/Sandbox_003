using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class ChunkDataTests
{
    [Fact]
    public void NewChunk_AllVoxelsAreAir()
    {
        var data = new ChunkData(8);
        for (int z = 0; z < 8; z++)
        for (int y = 0; y < 8; y++)
        for (int x = 0; x < 8; x++)
            Assert.Equal(MaterialId.Air, data.Get(x, y, z));
    }

    [Fact]
    public void Set_WithinBounds_PersistsValue()
    {
        var data = new ChunkData(4);
        data.Set(1, 2, 3, MaterialId.Stone);
        Assert.Equal(MaterialId.Stone, data.Get(1, 2, 3));
    }

    [Fact]
    public void Set_OutOfBounds_IsNoOp()
    {
        var data = new ChunkData(4);
        data.Set(-1, 0, 0, MaterialId.Stone);
        data.Set(0, 4, 0, MaterialId.Stone);
        Assert.Equal(MaterialId.Air, data.Get(-1, 0, 0));
        Assert.Equal(MaterialId.Air, data.Get(0, 4, 0));
    }

    [Fact]
    public void IsSolid_ReturnsFalseForAirAndOutOfBounds()
    {
        var data = new ChunkData(2);
        data.Set(0, 0, 0, MaterialId.Stone);
        Assert.True(data.IsSolid(0, 0, 0));
        Assert.False(data.IsSolid(1, 0, 0));
        Assert.False(data.IsSolid(-1, -1, -1));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveSize()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChunkData(0));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new ChunkData(-1));
    }
}
