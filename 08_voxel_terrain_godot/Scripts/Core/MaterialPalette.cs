using System.Numerics;

namespace VoxelTerrain.Core;

public static class MaterialPalette
{
    public static Vector4 ColorOf(byte id) => id switch
    {
        MaterialId.Stone => new Vector4(0.55f, 0.55f, 0.58f, 1f),
        MaterialId.Dirt => new Vector4(0.45f, 0.30f, 0.18f, 1f),
        MaterialId.Grass => new Vector4(0.30f, 0.65f, 0.25f, 1f),
        _ => new Vector4(1f, 0f, 1f, 1f),
    };
}
