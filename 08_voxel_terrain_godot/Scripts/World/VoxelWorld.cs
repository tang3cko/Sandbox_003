using Godot;
using VoxelTerrain.Core;

namespace VoxelTerrain.World;

public partial class VoxelWorld : Node3D
{
    [Export] public int SampleSize { get; set; } = 48;
    [Export] public float GroundLevel { get; set; } = 18f;
    [Export] public float HillAmplitude { get; set; } = 5f;
    [Export] public float HillFrequency { get; set; } = 0.06f;
    [Export] public int NoiseSeed { get; set; } = 1337;

    private VoxelChunk _chunk;
    private TerrainPalette _palette;
    private float[,] _heightMap;

    public VoxelChunk Chunk => _chunk;

    public override void _Ready()
    {
        _palette = new TerrainPalette();
        var noise = new HeightNoise(NoiseSeed);

        // Precompute the heightmap once. Used both by terrain initialization and per-vertex color
        // sampling so that ColorSampler does not invoke noise.SampleFractal on every rebuild's vertices.
        _heightMap = new float[SampleSize, SampleSize];
        for (int z = 0; z < SampleSize; z++)
        for (int x = 0; x < SampleSize; x++)
            _heightMap[x, z] = GroundLevel + noise.SampleFractal(x, z, HillFrequency, HillAmplitude, octaves: 3);

        _chunk = new VoxelChunk
        {
            Name = "Chunk_0_0_0",
            SampleSize = SampleSize,
            ColorSampler = pos =>
            {
                // Bilinear interpolation of the precomputed heightmap so that topY is
                // continuous across cell boundaries. Nearest-neighbor rounding would cause
                // step-like grass/dirt colour transitions on slopes.
                float fx = System.Math.Clamp(pos.X, 0f, SampleSize - 1.001f);
                float fz = System.Math.Clamp(pos.Z, 0f, SampleSize - 1.001f);
                int x0 = (int)fx, x1 = x0 + 1;
                int z0 = (int)fz, z1 = z0 + 1;
                float tx = fx - x0, tz = fz - z0;
                float h = _heightMap[x0, z0] * (1 - tx) * (1 - tz)
                        + _heightMap[x1, z0] * tx        * (1 - tz)
                        + _heightMap[x0, z1] * (1 - tx)  * tz
                        + _heightMap[x1, z1] * tx        * tz;
                return _palette.Sample(pos, h);
            },
        };
        AddChild(_chunk);

        SdfEdits.FillFromHeightMap(_chunk.Field, _heightMap);
        _chunk.Rebuild();
    }

    public bool DigSphere(Vector3 worldPos, float radius)
    {
        var local = worldPos - _chunk.GlobalPosition;
        int changed = SdfEdits.DigSphere(_chunk.Field, local.X, local.Y, local.Z, radius);
        if (changed == 0) return false;
        _chunk.Rebuild();
        return true;
    }

    public bool PlaceSphere(Vector3 worldPos, float radius)
    {
        var local = worldPos - _chunk.GlobalPosition;
        int changed = SdfEdits.PlaceSphere(_chunk.Field, local.X, local.Y, local.Z, radius);
        if (changed == 0) return false;
        _chunk.Rebuild();
        return true;
    }
}
