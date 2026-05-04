using System.Collections.Generic;
using System.Numerics;

namespace VoxelTerrain.Core;

public sealed class MeshData
{
    public List<Vector3> Vertices { get; }
    public List<Vector3> Normals { get; }
    public List<Vector4> Colors { get; }
    public List<int> Indices { get; }

    public MeshData(int estimatedVertexCount = 0)
    {
        Vertices = new List<Vector3>(estimatedVertexCount);
        Normals = new List<Vector3>(estimatedVertexCount);
        Colors = new List<Vector4>(estimatedVertexCount);
        Indices = new List<int>(estimatedVertexCount * 3 / 2);
    }

    public bool IsEmpty => Indices.Count == 0;

    public int TriangleCount => Indices.Count / 3;
}
