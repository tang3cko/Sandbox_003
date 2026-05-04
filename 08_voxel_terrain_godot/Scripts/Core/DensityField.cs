namespace VoxelTerrain.Core;

// 3D scalar field for Marching Cubes / smooth voxel terrain.
// Convention: density > 0 = SOLID, density < 0 = AIR, surface at density == 0.
// Size is sample count per axis. Marching Cubes processes (Size-1)^3 cells.
public sealed class DensityField
{
    public int Size { get; }
    private readonly float[] _values;

    public DensityField(int size)
    {
        if (size < 2) throw new System.ArgumentOutOfRangeException(nameof(size));
        Size = size;
        _values = new float[size * size * size];
    }

    public bool InBounds(int x, int y, int z) =>
        x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;

    public float Get(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) return -1f; // out of bounds = air (no surface at boundary)
        return _values[Index(x, y, z)];
    }

    public void Set(int x, int y, int z, float value)
    {
        if (!InBounds(x, y, z)) return;
        _values[Index(x, y, z)] = value;
    }

    public bool IsSolid(int x, int y, int z) => Get(x, y, z) > 0f;

    private int Index(int x, int y, int z) => x + Size * (y + Size * z);
}
