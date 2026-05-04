namespace VoxelTerrain.Core;

public static class MaterialId
{
    public const byte Air = 0;
    public const byte Stone = 1;
    public const byte Dirt = 2;
    public const byte Grass = 3;

    public static bool IsSolid(byte id) => id != Air;
}
