namespace SwarmSurvivor.Tests;

using System;
using SwarmSurvivor;
using Xunit;

public class SpawnPositionCalculatorTest
{
    private const long ServerPeerId = 1;

    [Fact]
    public void ComputePlayerSpawnPosition_ServerPeer_AtSlotZero()
    {
        var (x, z) = SpawnPositionCalculator.ComputePlayerSpawnPosition(1, ServerPeerId);
        Assert.Equal(4f, x, precision: 4);
        Assert.Equal(0f, z, precision: 4);
    }

    [Fact]
    public void ComputePlayerSpawnPosition_DifferentPeer_AtComputedSlot()
    {
        // peerId=2, server=1 → slot=2 → angle = 2 * Tau / 8 = PI/2 → (cos=0, sin=1) → (0, 4)
        var (x, z) = SpawnPositionCalculator.ComputePlayerSpawnPosition(2, ServerPeerId);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(4f, z, precision: 4);
    }

    [Fact]
    public void ComputePlayerSpawnPosition_PeerModEightHigh_WrapsToSlot()
    {
        // peerId=10 → slot = 10 % 8 = 2 → same as peerId=2
        var (x10, z10) = SpawnPositionCalculator.ComputePlayerSpawnPosition(10, ServerPeerId);
        var (x2, z2) = SpawnPositionCalculator.ComputePlayerSpawnPosition(2, ServerPeerId);
        Assert.Equal(x2, x10, precision: 4);
        Assert.Equal(z2, z10, precision: 4);
    }

    [Fact]
    public void ComputePlayerSpawnPosition_LargePeer_HandlesCorrectly()
    {
        // peerId=217212806 → slot = 217212806 % 8 = 6 → angle = 6 * Tau / 8 = 3*PI/2
        // (cos(3π/2)=0, sin(3π/2)=-1) → (0, -4)
        var (x, z) = SpawnPositionCalculator.ComputePlayerSpawnPosition(217212806L, ServerPeerId);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(-4f, z, precision: 4);
    }

    [Fact]
    public void ComputeRandomSpawnPosition_AngleZeroDistMin_OnPositiveX()
    {
        var (x, z) = SpawnPositionCalculator.ComputeRandomSpawnPosition(0.0, 0.0, 2f, 10f);
        Assert.Equal(2f, x, precision: 4);
        Assert.Equal(0f, z, precision: 4);
    }

    [Fact]
    public void ComputeRandomSpawnPosition_AngleHalfDistMax_OnNegativeX()
    {
        // angle = 0.5 * Tau = PI → (cos=-1, sin=0); dist = 10 → (-10, 0)
        var (x, z) = SpawnPositionCalculator.ComputeRandomSpawnPosition(0.5, 1.0, 2f, 10f);
        Assert.Equal(-10f, x, precision: 4);
        Assert.Equal(0f, z, precision: 4);
    }

    [Fact]
    public void ComputeRandomSpawnPosition_AngleQuarterDistHalf_OnPositiveZ()
    {
        // angle = 0.25 * Tau = PI/2 → (cos=0, sin=1); dist = 2 + 0.5*8 = 6 → (0, 6)
        var (x, z) = SpawnPositionCalculator.ComputeRandomSpawnPosition(0.25, 0.5, 2f, 10f);
        Assert.Equal(0f, x, precision: 4);
        Assert.Equal(6f, z, precision: 4);
    }

    [Fact]
    public void ComputeRandomSpawnPosition_DistMinEqualMax_FixedRadius()
    {
        var (x, z) = SpawnPositionCalculator.ComputeRandomSpawnPosition(0.37, 0.83, 5f, 5f);
        float magnitude = MathF.Sqrt(x * x + z * z);
        Assert.Equal(5f, magnitude, precision: 4);
    }

    [Fact]
    public void ComputePlayerSpawnPosition_RadiusIsConstant()
    {
        for (long peerId = 1; peerId <= 16; peerId++)
        {
            var (x, z) = SpawnPositionCalculator.ComputePlayerSpawnPosition(peerId, ServerPeerId);
            float magnitude = MathF.Sqrt(x * x + z * z);
            Assert.Equal(SpawnPositionCalculator.PlayerSpawnRadius, magnitude, precision: 4);
        }
    }
}
