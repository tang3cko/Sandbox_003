using System.Numerics;

namespace VoxelTerrain.Core;

public static class BlockyMeshBuilder
{
    private static readonly (int dx, int dy, int dz)[] FaceNeighbors =
    {
        ( 1,  0,  0), // +X
        (-1,  0,  0), // -X
        ( 0,  1,  0), // +Y
        ( 0, -1,  0), // -Y
        ( 0,  0,  1), // +Z
        ( 0,  0, -1), // -Z
    };

    private static readonly Vector3[] FaceNormals =
    {
        new( 1,  0,  0),
        new(-1,  0,  0),
        new( 0,  1,  0),
        new( 0, -1,  0),
        new( 0,  0,  1),
        new( 0,  0, -1),
    };

    // Corners listed CCW when viewed from outside (geometric/intuitive order).
    // Index emission below reverses winding to CW so Godot's CW = front-facing convention renders them outward.
    // Verified by (v2-v0) × (v1-v0) == outward normal in BlockyMeshBuilderTests.AllFaces_AreFrontFacingOutward.
    private static readonly Vector3[][] FaceCorners =
    {
        new[] { new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(1, 1, 1), new Vector3(1, 0, 1) }, // +X
        new[] { new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 1, 1), new Vector3(0, 1, 0) }, // -X
        new[] { new Vector3(0, 1, 0), new Vector3(0, 1, 1), new Vector3(1, 1, 1), new Vector3(1, 1, 0) }, // +Y
        new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 0, 1), new Vector3(0, 0, 1) }, // -Y
        new[] { new Vector3(0, 0, 1), new Vector3(1, 0, 1), new Vector3(1, 1, 1), new Vector3(0, 1, 1) }, // +Z
        new[] { new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 1, 0), new Vector3(1, 0, 0) }, // -Z
    };

    public static MeshData Build(ChunkData data)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        var mesh = new MeshData();
        int size = data.Size;

        for (int z = 0; z < size; z++)
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            byte id = data.Get(x, y, z);
            if (!MaterialId.IsSolid(id)) continue;

            for (int f = 0; f < 6; f++)
            {
                var n = FaceNeighbors[f];
                if (data.IsSolid(x + n.dx, y + n.dy, z + n.dz)) continue;
                EmitFace(mesh, x, y, z, f, id);
            }
        }
        return mesh;
    }

    private static void EmitFace(MeshData mesh, int x, int y, int z, int faceIndex, byte materialId)
    {
        var origin = new Vector3(x, y, z);
        var normal = FaceNormals[faceIndex];
        var color = MaterialPalette.ColorOf(materialId);
        int baseIndex = mesh.Vertices.Count;

        var corners = FaceCorners[faceIndex];
        for (int i = 0; i < 4; i++)
        {
            mesh.Vertices.Add(origin + corners[i]);
            mesh.Normals.Add(normal);
            mesh.Colors.Add(color);
        }

        mesh.Indices.Add(baseIndex + 0);
        mesh.Indices.Add(baseIndex + 2);
        mesh.Indices.Add(baseIndex + 1);
        mesh.Indices.Add(baseIndex + 0);
        mesh.Indices.Add(baseIndex + 3);
        mesh.Indices.Add(baseIndex + 2);
    }
}
