using System;
using Xunit;

namespace SwarmSurvivor.Tests;

public class SpatialSeparationGridTest
{
    private const float Alive = SwarmCalculator.AliveMarker;
    private const float Dying = 0f;
    private const float Radius = 2f;
    private const float Force = 10f;

    private static (float X, float Z) Naive(
        int selfIndex, float[] posX, float[] posZ, float[] deathTimer,
        int activeCount, float radius, float force)
    {
        return SeparationForceCalculator.ComputeAccumulatedSeparation(
            selfIndex, posX, posZ, deathTimer, activeCount, radius, force);
    }

    private static (float X, float Z) Grid(
        SpatialSeparationGrid grid,
        int selfIndex, float[] posX, float[] posZ, float[] deathTimer,
        int activeCount, float radius, float force)
    {
        grid.Build(posX, posZ, deathTimer, activeCount, radius);
        return grid.ComputeAccumulatedSeparation(
            selfIndex, posX, posZ, deathTimer, activeCount, radius, force);
    }

    [Fact]
    public void Empty_ReturnsZero()
    {
        var grid = new SpatialSeparationGrid();
        var posX = Array.Empty<float>();
        var posZ = Array.Empty<float>();
        var deathTimer = Array.Empty<float>();

        // activeCount = 0; selfIndex doesn't matter as we won't query.
        grid.Build(posX, posZ, deathTimer, 0, Radius);
        // No entities to query; but if we ask with a fake larger array:
        var fakeX = new float[1] { 0f };
        var fakeZ = new float[1] { 0f };
        var fakeDeath = new float[1] { Alive };
        // Build with active=0 means even querying selfIndex=0 yields zero contribution.
        var res = grid.ComputeAccumulatedSeparation(0, fakeX, fakeZ, fakeDeath, 0, Radius, Force);
        Assert.Equal(0f, res.X, precision: 4);
        Assert.Equal(0f, res.Z, precision: 4);
    }

    [Fact]
    public void SingleEntity_NoNeighbors_ReturnsZero()
    {
        var grid = new SpatialSeparationGrid();
        var posX = new float[1] { 0f };
        var posZ = new float[1] { 0f };
        var death = new float[1] { Alive };

        var result = Grid(grid, 0, posX, posZ, death, 1, Radius, Force);
        Assert.Equal(0f, result.X, precision: 4);
        Assert.Equal(0f, result.Z, precision: 4);
    }

    [Fact]
    public void TwoEntitiesWithinRadius_MatchesNaive()
    {
        var grid = new SpatialSeparationGrid();
        var posX = new float[2] { 0f, 1f };
        var posZ = new float[2] { 0f, 0f };
        var death = new float[2] { Alive, Alive };

        var expected = Naive(0, posX, posZ, death, 2, Radius, Force);
        var actual = Grid(grid, 0, posX, posZ, death, 2, Radius, Force);

        Assert.Equal(expected.X, actual.X, precision: 4);
        Assert.Equal(expected.Z, actual.Z, precision: 4);
    }

    [Fact]
    public void TwoEntitiesOutsideRadius_ReturnsZero()
    {
        var grid = new SpatialSeparationGrid();
        // Place far apart; even if cells overlap, the SwarmCalculator distance check
        // must reject them. With cellSize=Radius=2, (0,0) is in cell (0,0); (10,0) in (5,0).
        var posX = new float[2] { 0f, 10f };
        var posZ = new float[2] { 0f, 0f };
        var death = new float[2] { Alive, Alive };

        var expected = Naive(0, posX, posZ, death, 2, Radius, Force);
        var actual = Grid(grid, 0, posX, posZ, death, 2, Radius, Force);

        Assert.Equal(0f, expected.X, precision: 4);
        Assert.Equal(expected.X, actual.X, precision: 4);
        Assert.Equal(expected.Z, actual.Z, precision: 4);
    }

    [Fact]
    public void ClusterOf10_MatchesNaive()
    {
        var grid = new SpatialSeparationGrid();
        const int N = 10;
        var posX = new float[N];
        var posZ = new float[N];
        var death = new float[N];

        // Tight cluster; all within radius 2 from origin.
        for (int i = 0; i < N; i++)
        {
            posX[i] = 0.1f * i;
            posZ[i] = 0.05f * i;
            death[i] = Alive;
        }

        for (int i = 0; i < N; i++)
        {
            var expected = Naive(i, posX, posZ, death, N, Radius, Force);
            var actual = Grid(grid, i, posX, posZ, death, N, Radius, Force);
            Assert.Equal(expected.X, actual.X, precision: 4);
            Assert.Equal(expected.Z, actual.Z, precision: 4);
        }
    }

    [Fact]
    public void EntitiesOnCellBoundary_MatchesNaive()
    {
        // Place entities exactly on cell boundaries (multiples of cellSize=Radius=2).
        var grid = new SpatialSeparationGrid();
        var posX = new float[5] { 0f, 2f, -2f, 0f, 0f };
        var posZ = new float[5] { 0f, 0f, 0f, 2f, -2f };
        var death = new float[5] { Alive, Alive, Alive, Alive, Alive };

        for (int i = 0; i < 5; i++)
        {
            var expected = Naive(i, posX, posZ, death, 5, Radius, Force);
            var actual = Grid(grid, i, posX, posZ, death, 5, Radius, Force);
            Assert.Equal(expected.X, actual.X, precision: 4);
            Assert.Equal(expected.Z, actual.Z, precision: 4);
        }
    }

    [Fact]
    public void NegativeCoordinates_MatchesNaive()
    {
        var grid = new SpatialSeparationGrid();
        var posX = new float[4] { -5.3f, -4.1f, -5.0f, -6.2f };
        var posZ = new float[4] { -5.3f, -4.1f, -6.0f, -5.5f };
        var death = new float[4] { Alive, Alive, Alive, Alive };

        for (int i = 0; i < 4; i++)
        {
            var expected = Naive(i, posX, posZ, death, 4, Radius, Force);
            var actual = Grid(grid, i, posX, posZ, death, 4, Radius, Force);
            Assert.Equal(expected.X, actual.X, precision: 4);
            Assert.Equal(expected.Z, actual.Z, precision: 4);
        }
    }

    [Fact]
    public void DyingEntities_AreSkipped()
    {
        var grid = new SpatialSeparationGrid();
        var posX = new float[3] { 0f, 1f, 0f };
        var posZ = new float[3] { 0f, 0f, 1f };
        var death = new float[3] { Alive, Dying, Alive };

        var expected = Naive(0, posX, posZ, death, 3, Radius, Force);
        var actual = Grid(grid, 0, posX, posZ, death, 3, Radius, Force);

        Assert.Equal(expected.X, actual.X, precision: 4);
        Assert.Equal(expected.Z, actual.Z, precision: 4);
    }

    [Fact]
    public void DyingSelf_StillProducesSeparationFromAliveNeighbors()
    {
        // Calling Compute on a self that is dying should match naive (which doesn't gate on self).
        var grid = new SpatialSeparationGrid();
        var posX = new float[2] { 0f, 1f };
        var posZ = new float[2] { 0f, 0f };
        var death = new float[2] { Dying, Alive };

        var expected = Naive(0, posX, posZ, death, 2, Radius, Force);
        var actual = Grid(grid, 0, posX, posZ, death, 2, Radius, Force);

        Assert.Equal(expected.X, actual.X, precision: 4);
        Assert.Equal(expected.Z, actual.Z, precision: 4);
    }

    [Fact]
    public void Random100_MatchesNaive()
    {
        // RED-pinning cross-validation against the naive O(N^2) implementation.
        var grid = new SpatialSeparationGrid();
        const int N = 100;
        var rng = new Random(42);
        var posX = new float[N];
        var posZ = new float[N];
        var death = new float[N];
        for (int i = 0; i < N; i++)
        {
            posX[i] = (float)(rng.NextDouble() * 20.0 - 10.0);
            posZ[i] = (float)(rng.NextDouble() * 20.0 - 10.0);
            // Mark ~15% as dying.
            death[i] = rng.NextDouble() < 0.15 ? (float)(rng.NextDouble() * 0.4) : Alive;
        }

        for (int i = 0; i < N; i++)
        {
            var expected = Naive(i, posX, posZ, death, N, Radius, Force);
            var actual = Grid(grid, i, posX, posZ, death, N, Radius, Force);
            Assert.Equal(expected.X, actual.X, precision: 4);
            Assert.Equal(expected.Z, actual.Z, precision: 4);
        }
    }

    [Fact]
    public void ReusedGrid_NoAllocationBeyondInitial()
    {
        // Build twice and ensure correctness is preserved across rebuilds (buffers reused).
        var grid = new SpatialSeparationGrid();
        var posX = new float[3] { 0f, 1f, 0f };
        var posZ = new float[3] { 0f, 0f, 1f };
        var death = new float[3] { Alive, Alive, Alive };

        // First build/query.
        grid.Build(posX, posZ, death, 3, Radius);
        var first = grid.ComputeAccumulatedSeparation(0, posX, posZ, death, 3, Radius, Force);

        // Mutate positions and rebuild.
        posX[1] = 5f; // move entity 1 far away
        grid.Build(posX, posZ, death, 3, Radius);
        var second = grid.ComputeAccumulatedSeparation(0, posX, posZ, death, 3, Radius, Force);

        var firstExpected = SwarmCalculator.CalculateSeparation(0f, 0f, 1f, 0f, Radius, Force);
        var firstExpected2 = SwarmCalculator.CalculateSeparation(0f, 0f, 0f, 1f, Radius, Force);
        Assert.Equal(firstExpected.VelX + firstExpected2.VelX, first.X, precision: 4);
        Assert.Equal(firstExpected.VelZ + firstExpected2.VelZ, first.Z, precision: 4);

        // After moving entity 1 to (5,0) outside radius 2 → only entity 2 contributes.
        Assert.Equal(firstExpected2.VelX, second.X, precision: 4);
        Assert.Equal(firstExpected2.VelZ, second.Z, precision: 4);
    }
}
