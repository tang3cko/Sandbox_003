namespace VoxelTerrain.Core;

// Deterministic 2D value noise with optional fractal octaves.
// No external dependency (pure System.Math). Output range roughly [-amplitude, +amplitude] for a single octave.
public sealed class HeightNoise
{
    private readonly int _seed;

    public HeightNoise(int seed = 1337) { _seed = seed; }

    public float Sample(float x, float z, float frequency = 0.05f, float amplitude = 1f)
    {
        x *= frequency;
        z *= frequency;
        int x0 = (int)System.Math.Floor(x);
        int z0 = (int)System.Math.Floor(z);
        float fx = Smoothstep(x - x0);
        float fz = Smoothstep(z - z0);

        float v00 = Hash(x0,     z0);
        float v10 = Hash(x0 + 1, z0);
        float v01 = Hash(x0,     z0 + 1);
        float v11 = Hash(x0 + 1, z0 + 1);

        float v0 = Lerp(v00, v10, fx);
        float v1 = Lerp(v01, v11, fx);
        return Lerp(v0, v1, fz) * amplitude;
    }

    public float SampleFractal(float x, float z, float frequency, float amplitude, int octaves, float lacunarity = 2f, float gain = 0.5f)
    {
        float total = 0f;
        float a = amplitude;
        float f = frequency;
        for (int i = 0; i < octaves; i++)
        {
            total += Sample(x, z, f, a);
            f *= lacunarity;
            a *= gain;
        }
        return total;
    }

    private float Hash(int x, int z)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + z * 668265263 + _seed * 1442695040888963407L);
            h = (h ^ (h >> 13)) * 1274126177u;
            h = h ^ (h >> 16);
            return (h / (float)uint.MaxValue) * 2f - 1f;
        }
    }

    private static float Smoothstep(float t) => t * t * (3f - 2f * t);
    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
