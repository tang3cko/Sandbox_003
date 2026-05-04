using System.Numerics;
using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class MarchingCubesMesherTests
{
    [Fact]
    public void EmptyField_ProducesEmptyMesh()
    {
        var field = new DensityField(8);
        var mesh = MarchingCubesMesher.Build(field);
        Assert.True(mesh.IsEmpty);
    }

    [Fact]
    public void FullySolidField_ProducesBoundaryWallsOnly()
    {
        const int size = 8;
        var field = new DensityField(size);
        for (int z = 0; z < size; z++)
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            field.Set(x, y, z, 1f);

        var mesh = MarchingCubesMesher.Build(field);
        Assert.False(mesh.IsEmpty);

        foreach (var v in mesh.Vertices)
        {
            bool atBoundary =
                v.X < 0.5f || v.X > size - 1.5f ||
                v.Y < 0.5f || v.Y > size - 1.5f ||
                v.Z < 0.5f || v.Z > size - 1.5f;
            Assert.True(atBoundary, $"vertex {v} should sit on the chunk boundary");
        }
    }

    [Fact]
    public void FlatGround_ProducesNonEmptyMesh()
    {
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY: 8f);
        var mesh = MarchingCubesMesher.Build(field);
        Assert.False(mesh.IsEmpty);
        Assert.True(mesh.TriangleCount > 0);
    }

    [Fact]
    public void FlatGround_TopSurfaceVerticesLieNearGroundY()
    {
        const float groundY = 8f;
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY);
        var mesh = MarchingCubesMesher.Build(field);

        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            if (mesh.Normals[i].Y > 0.5f)
                Assert.InRange(mesh.Vertices[i].Y, groundY - 1f, groundY + 1f);
        }
    }

    // For Godot CW = front-facing, the player viewing from the air side must see the triangle as CW.
    // The triangle's natural cross product (v1-v0) × (v2-v0) should therefore point INTO the solid
    // (opposite to outward), and the per-vertex normal should still be outward.
    // Equivalently: (v2-v0) × (v1-v0) should align with the outward normal.
    [Fact]
    public void FlatGround_TrianglesWindCwFromAirSide()
    {
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY: 8f);
        var mesh = MarchingCubesMesher.Build(field);

        int aligned = 0;
        int total = 0;
        for (int i = 0; i < mesh.Indices.Count; i += 3)
        {
            var v0 = mesh.Vertices[mesh.Indices[i]];
            var v1 = mesh.Vertices[mesh.Indices[i + 1]];
            var v2 = mesh.Vertices[mesh.Indices[i + 2]];
            var avgNormal = (mesh.Normals[mesh.Indices[i]]
                           + mesh.Normals[mesh.Indices[i + 1]]
                           + mesh.Normals[mesh.Indices[i + 2]]) / 3f;
            var faceFront = Vector3.Cross(v2 - v0, v1 - v0);
            if (Vector3.Dot(faceFront, avgNormal) > 0f) aligned++;
            total++;
        }
        Assert.Equal(total, aligned);
    }

    [Fact]
    public void FlatGround_HasUpwardFacingTopSurface()
    {
        const float groundY = 8f;
        var field = new DensityField(16);
        SdfEdits.FillFlatGround(field, groundY);
        var mesh = MarchingCubesMesher.Build(field);

        int upFacing = 0;
        for (int i = 0; i < mesh.Vertices.Count; i++)
        {
            if (mesh.Normals[i].Y > 0.5f && System.Math.Abs(mesh.Vertices[i].Y - groundY) < 1f)
                upFacing++;
        }
        Assert.True(upFacing > 0, "expected at least some up-facing vertices on the top surface");
    }

    [Fact]
    public void DigSphere_ReducesMeshArea()
    {
        var field = new DensityField(20);
        SdfEdits.FillFlatGround(field, groundY: 10f);
        var before = MarchingCubesMesher.Build(field);

        SdfEdits.DigSphere(field, 10f, 10f, 10f, 3f);
        var after = MarchingCubesMesher.Build(field);

        Assert.NotEqual(before.TriangleCount, after.TriangleCount);
        Assert.True(after.TriangleCount > before.TriangleCount,
            "digging exposes new surface so triangle count should grow");
    }
}
