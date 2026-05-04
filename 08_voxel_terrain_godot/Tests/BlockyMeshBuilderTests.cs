using System.Numerics;
using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class BlockyMeshBuilderTests
{
    [Fact]
    public void EmptyChunk_ProducesEmptyMesh()
    {
        var mesh = BlockyMeshBuilder.Build(new ChunkData(4));
        Assert.True(mesh.IsEmpty);
        Assert.Empty(mesh.Vertices);
        Assert.Empty(mesh.Indices);
    }

    [Fact]
    public void SingleVoxel_Produces6Faces()
    {
        var data = new ChunkData(4);
        data.Set(1, 1, 1, MaterialId.Stone);
        var mesh = BlockyMeshBuilder.Build(data);

        Assert.Equal(24, mesh.Vertices.Count);
        Assert.Equal(36, mesh.Indices.Count);
        Assert.Equal(12, mesh.TriangleCount);
    }

    [Fact]
    public void TwoAdjacentVoxels_HiddenFacesAreCulled()
    {
        var data = new ChunkData(4);
        data.Set(1, 1, 1, MaterialId.Stone);
        data.Set(2, 1, 1, MaterialId.Stone);
        var mesh = BlockyMeshBuilder.Build(data);

        Assert.Equal(10 * 4, mesh.Vertices.Count);
        Assert.Equal(10 * 6, mesh.Indices.Count);
    }

    [Fact]
    public void FullyEnclosedVoxel_ProducesNoFaces()
    {
        var data = new ChunkData(4);
        for (int z = 0; z < 4; z++)
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
            data.Set(x, y, z, MaterialId.Stone);

        var mesh = BlockyMeshBuilder.Build(data);

        int expectedFacesPerExposed = 0;
        for (int z = 0; z < 4; z++)
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            int onBoundary = 0;
            if (x == 0) onBoundary++;
            if (x == 3) onBoundary++;
            if (y == 0) onBoundary++;
            if (y == 3) onBoundary++;
            if (z == 0) onBoundary++;
            if (z == 3) onBoundary++;
            expectedFacesPerExposed += onBoundary;
        }

        Assert.Equal(expectedFacesPerExposed * 6, mesh.Indices.Count);
    }

    // Godot uses CW winding for front-facing triangles.
    // For a face to be visible from outside (front-facing toward outward normal),
    // the index order must be such that (v2 - v0) × (v1 - v0) equals the outward normal.
    [Fact]
    public void AllFaces_AreFrontFacingOutward()
    {
        var data = new ChunkData(3);
        data.Set(1, 1, 1, MaterialId.Stone);
        var mesh = BlockyMeshBuilder.Build(data);

        Assert.Equal(12, mesh.TriangleCount);

        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            int i0 = mesh.Indices[i];
            int i1 = mesh.Indices[i + 1];
            int i2 = mesh.Indices[i + 2];
            var v0 = mesh.Vertices[i0];
            var v1 = mesh.Vertices[i1];
            var v2 = mesh.Vertices[i2];
            var declaredNormal = mesh.Normals[i0];
            var computed = Vector3.Normalize(Vector3.Cross(v2 - v0, v1 - v0));
            float diff = (computed - declaredNormal).Length();
            Assert.True(diff < 0.001f,
                $"Triangle {i / 3} not front-facing outward: declared normal {declaredNormal}, computed {computed}");
        }
    }
}
