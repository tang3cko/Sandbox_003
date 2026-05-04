namespace VoxelTerrain.Core;

public static class VoxelEdits
{
    public static int FillSphere(ChunkData data, float cx, float cy, float cz, float radius, byte id)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        if (radius <= 0f) return 0;

        int minX = (int)System.Math.Floor(cx - radius);
        int minY = (int)System.Math.Floor(cy - radius);
        int minZ = (int)System.Math.Floor(cz - radius);
        int maxX = (int)System.Math.Ceiling(cx + radius);
        int maxY = (int)System.Math.Ceiling(cy + radius);
        int maxZ = (int)System.Math.Ceiling(cz + radius);

        float r2 = radius * radius;
        int changed = 0;

        for (int z = minZ; z <= maxZ; z++)
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            if (!data.InBounds(x, y, z)) continue;
            float dx = x + 0.5f - cx;
            float dy = y + 0.5f - cy;
            float dz = z + 0.5f - cz;
            if (dx * dx + dy * dy + dz * dz > r2) continue;
            if (data.Get(x, y, z) == id) continue;
            data.Set(x, y, z, id);
            changed++;
        }
        return changed;
    }

    public static int DigSphere(ChunkData data, float cx, float cy, float cz, float radius)
        => FillSphere(data, cx, cy, cz, radius, MaterialId.Air);

    public static bool SetVoxelAt(ChunkData data, float fx, float fy, float fz, byte id)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        int x = (int)System.Math.Floor(fx);
        int y = (int)System.Math.Floor(fy);
        int z = (int)System.Math.Floor(fz);
        if (!data.InBounds(x, y, z)) return false;
        if (data.Get(x, y, z) == id) return false;
        data.Set(x, y, z, id);
        return true;
    }

    public static void FillFlatGround(ChunkData data, int groundY, byte stoneId, byte dirtId, byte grassId, int dirtThickness = 3)
    {
        if (data == null) throw new System.ArgumentNullException(nameof(data));
        for (int z = 0; z < data.Size; z++)
        for (int x = 0; x < data.Size; x++)
        {
            for (int y = 0; y < data.Size; y++)
            {
                if (y < groundY - dirtThickness) data.Set(x, y, z, stoneId);
                else if (y < groundY) data.Set(x, y, z, dirtId);
                else if (y == groundY) data.Set(x, y, z, grassId);
                else data.Set(x, y, z, MaterialId.Air);
            }
        }
    }
}
