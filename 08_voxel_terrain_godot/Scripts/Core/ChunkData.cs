namespace VoxelTerrain.Core;

public sealed class ChunkData
{
    public int Size { get; }
    private readonly byte[] _voxels;

    public ChunkData(int size)
    {
        if (size <= 0) throw new System.ArgumentOutOfRangeException(nameof(size));
        Size = size;
        _voxels = new byte[size * size * size];
    }

    public bool InBounds(int x, int y, int z) =>
        x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size;

    public byte Get(int x, int y, int z)
    {
        if (!InBounds(x, y, z)) return MaterialId.Air;
        return _voxels[Index(x, y, z)];
    }

    public void Set(int x, int y, int z, byte id)
    {
        if (!InBounds(x, y, z)) return;
        _voxels[Index(x, y, z)] = id;
    }

    public bool IsSolid(int x, int y, int z) => MaterialId.IsSolid(Get(x, y, z));

    private int Index(int x, int y, int z) => x + Size * (y + Size * z);
}
