using Xunit;

namespace SwarmSurvivor.Tests;

public class SeparationForceCalculatorTest
{
    private const float Alive = SwarmCalculator.AliveMarker;
    private const float Dying = 0f;
    private const float Radius = 2f;
    private const float Force = 10f;

    [Fact]
    public void ComputeAccumulated_NoOthers_ReturnsZero()
    {
        var posX = new float[1] { 0f };
        var posZ = new float[1] { 0f };
        var deathTimer = new float[1] { Alive };

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 1, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_AllDying_ReturnsZero()
    {
        var posX = new float[3] { 0f, 0.5f, -0.5f };
        var posZ = new float[3] { 0f, 0.5f, -0.5f };
        var deathTimer = new float[3] { Alive, Dying, 0.1f };

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 3, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_OneNeighborInRange_AppliesForce()
    {
        // Self at (0,0), neighbor at (1,0) within radius 2 → push self in +X direction.
        var posX = new float[2] { 0f, 1f };
        var posZ = new float[2] { 0f, 0f };
        var deathTimer = new float[2] { Alive, Alive };

        var expected = SwarmCalculator.CalculateSeparation(0f, 0f, 1f, 0f, Radius, Force);
        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 2, Radius, Force);

        Assert.Equal(expected.VelX, result.X, precision: 4);
        Assert.Equal(expected.VelZ, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_NeighborOutOfRange_AppliesZero()
    {
        // Neighbor at (5,0) with radius 2 → outside; CalculateSeparation returns 0.
        var posX = new float[2] { 0f, 5f };
        var posZ = new float[2] { 0f, 0f };
        var deathTimer = new float[2] { Alive, Alive };

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 2, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_TwoNeighbors_SumsForces()
    {
        // Self at (0,0); neighbors at (1,0) and (0,1) within radius.
        var posX = new float[3] { 0f, 1f, 0f };
        var posZ = new float[3] { 0f, 0f, 1f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        var sep1 = SwarmCalculator.CalculateSeparation(0f, 0f, 1f, 0f, Radius, Force);
        var sep2 = SwarmCalculator.CalculateSeparation(0f, 0f, 0f, 1f, Radius, Force);

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 3, Radius, Force);

        Assert.Equal(sep1.VelX + sep2.VelX, result.X, precision: 4);
        Assert.Equal(sep1.VelZ + sep2.VelZ, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_SkipsSelfIndex()
    {
        // Self index is 1; the only "neighbor" is itself → must skip → zero.
        var posX = new float[2] { 100f, 0f };
        var posZ = new float[2] { 100f, 0f };
        var deathTimer = new float[2] { Alive, Alive };

        // activeCount = 1 means only j=0 considered, but self is index 1 (out of activeCount).
        // Use activeCount = 2 with self=1; only j=0 (far away) considered.
        // Far entity at (100,100) is outside radius → still 0.
        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            1, posX, posZ, deathTimer, 2, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);

        // Also verify with self adjacent to a near entity: self at index 0, neighbor at index 0 mustn't be counted.
        var posX2 = new float[2] { 0f, 1f };
        var posZ2 = new float[2] { 0f, 0f };
        var deathTimer2 = new float[2] { Alive, Alive };
        var resultSelf = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX2, posZ2, deathTimer2, 2, Radius, Force);
        var expected = SwarmCalculator.CalculateSeparation(0f, 0f, 1f, 0f, Radius, Force);
        // If self were not skipped, dx=0/dz=0 → distSq < 0.0001 → returns zero anyway,
        // so the visible effect equals only the neighbor's contribution.
        Assert.Equal(expected.VelX, resultSelf.X, precision: 4);
        Assert.Equal(expected.VelZ, resultSelf.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_SkipsDyingNeighbor()
    {
        // Self at (0,0); neighbor[1] at (1,0) DYING; neighbor[2] at (0,1) alive.
        var posX = new float[3] { 0f, 1f, 0f };
        var posZ = new float[3] { 0f, 0f, 1f };
        var deathTimer = new float[3] { Alive, Dying, Alive };

        // Only neighbor[2] should contribute.
        var expected = SwarmCalculator.CalculateSeparation(0f, 0f, 0f, 1f, Radius, Force);

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 3, Radius, Force);

        Assert.Equal(expected.VelX, result.X, precision: 4);
        Assert.Equal(expected.VelZ, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_OppositeSidesSymmetric_CancelOut()
    {
        // Self at (0,0); neighbors at (+1,0) and (-1,0) at the same distance.
        // Forces should cancel along X, total Z = 0.
        var posX = new float[3] { 0f, 1f, -1f };
        var posZ = new float[3] { 0f, 0f, 0f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 3, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_ZeroActiveCount_ReturnsZero()
    {
        var posX = new float[3] { 0f, 1f, -1f };
        var posZ = new float[3] { 0f, 0f, 0f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, 0, Radius, Force);

        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void ComputeAccumulated_LargeSwarm_HandlesAll()
    {
        // 11 entities: self at index 0 (0,0); 10 neighbors clustered in +X half.
        const int N = 11;
        var posX = new float[N];
        var posZ = new float[N];
        var deathTimer = new float[N];
        posX[0] = 0f;
        posZ[0] = 0f;
        deathTimer[0] = Alive;

        float expectedX = 0f, expectedZ = 0f;
        for (int j = 1; j < N; j++)
        {
            posX[j] = 0.5f + 0.05f * j; // all within radius 2 from origin
            posZ[j] = 0.05f * j;
            deathTimer[j] = Alive;
            var sep = SwarmCalculator.CalculateSeparation(
                0f, 0f, posX[j], posZ[j], Radius, Force);
            expectedX += sep.VelX;
            expectedZ += sep.VelZ;
        }

        var result = SeparationForceCalculator.ComputeAccumulatedSeparation(
            0, posX, posZ, deathTimer, N, Radius, Force);

        Assert.Equal(expectedX, result.X, precision: 4);
        Assert.Equal(expectedZ, result.Z, precision: 4);
    }
}
