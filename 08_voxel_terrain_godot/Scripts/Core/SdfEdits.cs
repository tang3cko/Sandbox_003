namespace VoxelTerrain.Core;

// Field operations on DensityField using SDF-style boolean composition.
// Convention: positive = solid, negative = air, surface at 0.
// Union (place):     new = max(old, brush)
// Subtract (dig):    new = min(old, -brush)
public static class SdfEdits
{
    public static void FillFlatGround(DensityField field, float groundY)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        for (int z = 0; z < field.Size; z++)
        for (int y = 0; y < field.Size; y++)
        for (int x = 0; x < field.Size; x++)
            field.Set(x, y, z, groundY - y);
    }

    public static void FillHillyTerrain(
        DensityField field,
        float baseGroundY,
        HeightNoise noise,
        float frequency = 0.06f,
        float amplitude = 4f,
        int octaves = 3)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        if (noise == null) throw new System.ArgumentNullException(nameof(noise));
        for (int z = 0; z < field.Size; z++)
        for (int x = 0; x < field.Size; x++)
        {
            float h = baseGroundY + noise.SampleFractal(x, z, frequency, amplitude, octaves);
            for (int y = 0; y < field.Size; y++)
                field.Set(x, y, z, h - y);
        }
    }

    public static void FillFromHeightMap(DensityField field, float[,] heightMap)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        if (heightMap == null) throw new System.ArgumentNullException(nameof(heightMap));
        if (heightMap.GetLength(0) != field.Size || heightMap.GetLength(1) != field.Size)
            throw new System.ArgumentException("heightMap dimensions must match field size", nameof(heightMap));

        for (int z = 0; z < field.Size; z++)
        for (int x = 0; x < field.Size; x++)
        {
            float h = heightMap[x, z];
            for (int y = 0; y < field.Size; y++)
                field.Set(x, y, z, h - y);
        }
    }

    public static int DigSphere(DensityField field, float cx, float cy, float cz, float radius)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        if (radius <= 0f) return 0;
        return ApplyBrush(field, cx, cy, cz, radius, subtract: true);
    }

    public static int PlaceSphere(DensityField field, float cx, float cy, float cz, float radius)
    {
        if (field == null) throw new System.ArgumentNullException(nameof(field));
        if (radius <= 0f) return 0;
        return ApplyBrush(field, cx, cy, cz, radius, subtract: false);
    }

    private static int ApplyBrush(DensityField field, float cx, float cy, float cz, float radius, bool subtract)
    {
        // Tighten AABB to brush radius; only cells within (radius + skin) actually need a sqrt + read+write.
        const float skin = 0.5f;
        int minX = (int)System.Math.Floor(cx - radius - skin);
        int minY = (int)System.Math.Floor(cy - radius - skin);
        int minZ = (int)System.Math.Floor(cz - radius - skin);
        int maxX = (int)System.Math.Ceiling(cx + radius + skin);
        int maxY = (int)System.Math.Ceiling(cy + radius + skin);
        int maxZ = (int)System.Math.Ceiling(cz + radius + skin);

        float padR = radius + skin;
        float padR2 = padR * padR;
        int changed = 0;

        for (int z = minZ; z <= maxZ; z++)
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            if (!field.InBounds(x, y, z)) continue;

            float dx = x - cx;
            float dy = y - cy;
            float dz = z - cz;
            float d2 = dx * dx + dy * dy + dz * dz;
            if (d2 > padR2) continue; // outside influence sphere — skip sqrt + read

            float dist = (float)System.Math.Sqrt(d2);
            float brush = radius - dist;

            float old = field.Get(x, y, z);
            float updated = subtract ? System.Math.Min(old, -brush) : System.Math.Max(old, brush);

            if (updated != old)
            {
                field.Set(x, y, z, updated);
                changed++;
            }
        }
        return changed;
    }
}
