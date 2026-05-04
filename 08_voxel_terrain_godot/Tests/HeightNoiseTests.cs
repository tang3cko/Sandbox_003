using VoxelTerrain.Core;
using Xunit;

namespace VoxelTerrain.Tests;

public class HeightNoiseTests
{
    [Fact]
    public void SameSeed_SameInput_ProducesSameValue()
    {
        var a = new HeightNoise(42);
        var b = new HeightNoise(42);
        Assert.Equal(a.Sample(3.7f, 5.2f), b.Sample(3.7f, 5.2f));
    }

    [Fact]
    public void DifferentSeed_ProducesDifferentValues()
    {
        var a = new HeightNoise(1);
        var b = new HeightNoise(2);
        Assert.NotEqual(a.Sample(3.7f, 5.2f), b.Sample(3.7f, 5.2f));
    }

    [Fact]
    public void Sample_StaysWithinAmplitude()
    {
        var noise = new HeightNoise(99);
        for (int i = 0; i < 200; i++)
        for (int j = 0; j < 200; j++)
        {
            float v = noise.Sample(i * 0.1f, j * 0.1f, frequency: 0.5f, amplitude: 3f);
            Assert.InRange(v, -3.001f, 3.001f);
        }
    }

    [Fact]
    public void Fractal_SumsOctaves()
    {
        var noise = new HeightNoise(7);
        float octave1 = noise.Sample(2f, 3f, 0.1f, 1f);
        float fractal1 = noise.SampleFractal(2f, 3f, 0.1f, 1f, octaves: 1);
        Assert.Equal(octave1, fractal1);
    }
}
