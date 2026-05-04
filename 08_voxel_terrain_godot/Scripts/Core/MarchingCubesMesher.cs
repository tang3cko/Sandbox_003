using System.Numerics;

namespace VoxelTerrain.Core;

// Marching Cubes mesher operating on a DensityField (positive = solid, negative = air, surface at 0).
//
// Triangle winding:
//   The MC table generates triangles wound CCW when viewed from the solid side.
//   That means CCW when viewed from the air side as well? No — a triangle wound CCW from one side
//   is CW from the opposite side. The natural triangle's normal (B-A) × (C-A) points OUT of the
//   solid (verified for case 1, the single-corner case).
//   The player views from the air side (where the normal points to), so naturally sees CCW.
//   Godot uses CW for front faces, so we REVERSE each triangle's winding by emitting (a, c, b).
public static class MarchingCubesMesher
{
    public static MeshData Build(DensityField field) => Build(field, _ => MaterialPalette.ColorOf(MaterialId.Stone));

    public static MeshData Build(DensityField field, Vector4 color) => Build(field, _ => color);

    public static MeshData Build(DensityField field, System.Func<Vector3, Vector4> colorSampler)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        if (colorSampler == null) throw new System.ArgumentNullException(nameof(colorSampler));

        int size = field.Size;
        // Estimate: a flat surface produces ~size² × 6 vertices. Allocate enough headroom up front
        // so List<>.Add() does not trigger geometric growth-and-copy during the hot loop.
        var mesh = new MeshData(estimatedVertexCount: size * size * 6);

        var cornerValues = new float[8];
        var cornerNormals = new Vector3[8];
        var edgeVerts = new Vector3[12];
        var edgeNormals = new Vector3[12];

        // Iterate including boundary cubes whose corners reach outside the field.
        // Out-of-bounds samples return air (negative) so that boundary walls (sides + bottom of the chunk)
        // are also generated, not only the interior surface.
        for (int z = -1; z < size; z++)
        for (int y = -1; y < size; y++)
        for (int x = -1; x < size; x++)
        {
            int caseIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                var off = MarchingCubesTables.CornerOffsets[i];
                float v = field.Get(x + off.x, y + off.y, z + off.z);
                cornerValues[i] = v;
                if (v > 0f) caseIndex |= 1 << i;
            }

            if (caseIndex == 0 || caseIndex == 255) continue;

            int edgeMask = MarchingCubesTables.EdgeMasks[caseIndex];
            if (edgeMask == 0) continue;

            // Compute corner normals once per cube (was up to 24× redundant within the same cube before).
            // Each normal needs only 6 field reads, so 8 corners = 48 reads / cube vs previous up-to-144.
            for (int i = 0; i < 8; i++)
            {
                var off = MarchingCubesTables.CornerOffsets[i];
                cornerNormals[i] = OutwardNormal(field, x + off.x, y + off.y, z + off.z);
            }

            for (int e = 0; e < 12; e++)
            {
                if ((edgeMask & (1 << e)) == 0) continue;

                var (ai, bi) = MarchingCubesTables.EdgeVertexIndices[e];
                var aOff = MarchingCubesTables.CornerOffsets[ai];
                var bOff = MarchingCubesTables.CornerOffsets[bi];
                float vA = cornerValues[ai];
                float vB = cornerValues[bi];

                float t = SafeInterp(vA, vB);
                var aPos = new Vector3(x + aOff.x, y + aOff.y, z + aOff.z);
                var bPos = new Vector3(x + bOff.x, y + bOff.y, z + bOff.z);
                edgeVerts[e] = Lerp(aPos, bPos, t);

                edgeNormals[e] = SafeNormalize(Lerp(cornerNormals[ai], cornerNormals[bi], t));
            }

            int[] tris = MarchingCubesTables.TriangleTable[caseIndex];
            for (int i = 0; i < tris.Length - 2; i += 3)
            {
                int e0 = tris[i];
                if (e0 < 0) break;
                int e1 = tris[i + 1];
                int e2 = tris[i + 2];

                int baseIndex = mesh.Vertices.Count;
                mesh.Vertices.Add(edgeVerts[e0]);
                mesh.Vertices.Add(edgeVerts[e1]);
                mesh.Vertices.Add(edgeVerts[e2]);
                mesh.Normals.Add(edgeNormals[e0]);
                mesh.Normals.Add(edgeNormals[e1]);
                mesh.Normals.Add(edgeNormals[e2]);
                mesh.Colors.Add(colorSampler(edgeVerts[e0]));
                mesh.Colors.Add(colorSampler(edgeVerts[e1]));
                mesh.Colors.Add(colorSampler(edgeVerts[e2]));

                // Reverse winding for Godot CW = front-facing convention.
                mesh.Indices.Add(baseIndex + 0);
                mesh.Indices.Add(baseIndex + 2);
                mesh.Indices.Add(baseIndex + 1);
            }
        }
        return mesh;
    }

    private static float SafeInterp(float vA, float vB)
    {
        float diff = vA - vB;
        if (System.Math.Abs(diff) < 1e-6f) return 0.5f;
        float t = vA / diff;
        if (t < 0f) return 0f;
        if (t > 1f) return 1f;
        return t;
    }

    // Outward normal at a sample = -gradient(density), normalized.
    // Gradient computed via central differences. Outward = direction of decreasing density (toward air).
    private static Vector3 OutwardNormal(DensityField field, int x, int y, int z)
    {
        float gx = field.Get(x + 1, y, z) - field.Get(x - 1, y, z);
        float gy = field.Get(x, y + 1, z) - field.Get(x, y - 1, z);
        float gz = field.Get(x, y, z + 1) - field.Get(x, y, z - 1);
        return SafeNormalize(new Vector3(-gx, -gy, -gz));
    }

    private static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;

    private static Vector3 SafeNormalize(Vector3 v)
    {
        float lenSq = v.LengthSquared();
        if (lenSq < 1e-12f) return new Vector3(0f, 1f, 0f);
        return v / (float)System.Math.Sqrt(lenSq);
    }
}
