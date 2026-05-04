using System.Numerics;

namespace VoxelTerrain.Core;

// Geological color layering by world Y. Models the Bananza-style "strip grass to reveal soil, dig deeper to hit rock".
public sealed class TerrainPalette
{
    public Vector4 GrassColor { get; init; } = new(0.30f, 0.65f, 0.25f, 1f);
    public Vector4 DirtColor { get; init; } = new(0.45f, 0.30f, 0.18f, 1f);
    public Vector4 StoneColor { get; init; } = new(0.55f, 0.55f, 0.58f, 1f);

    public float GrassDepth { get; init; } = 0.6f;
    public float DirtDepth { get; init; } = 3.5f;

    public Vector4 Sample(Vector3 worldPos, float referenceTopY)
    {
        float depth = referenceTopY - worldPos.Y;
        if (depth <= GrassDepth) return GrassColor;
        if (depth <= DirtDepth) return DirtColor;
        return StoneColor;
    }
}
