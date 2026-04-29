using Xunit;

namespace SwarmSurvivor.Tests;

public class NearestEntityFinderTest
{
    private const float Alive = SwarmCalculator.AliveMarker;
    private const float Dying = 0f;

    [Fact]
    public void FindNearestAliveIndex_NoEntities_ReturnsMinusOne()
    {
        var posX = new float[4];
        var posZ = new float[4];
        var deathTimer = new float[4] { Alive, Alive, Alive, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 0);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindNearestAliveIndex_AllDying_ReturnsMinusOne()
    {
        var posX = new float[3] { 1f, 2f, 3f };
        var posZ = new float[3] { 1f, 2f, 3f };
        var deathTimer = new float[3] { Dying, 0.1f, 0.3f };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindNearestAliveIndex_OneAlive_ReturnsItsIndex()
    {
        var posX = new float[3] { 10f, 20f, 5f };
        var posZ = new float[3] { 10f, 20f, 5f };
        var deathTimer = new float[3] { Dying, Dying, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(2, result);
    }

    [Fact]
    public void FindNearestAliveIndex_ClosestAlive_AmongMixed()
    {
        // Distances from (0,0): idx0=sqrt(50), idx1=sqrt(8), idx2=sqrt(200)
        var posX = new float[3] { 5f, 2f, 10f };
        var posZ = new float[3] { 5f, 2f, 10f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindNearestAliveIndex_DyingSkipped_PicksNonDying()
    {
        // idx0 is closest but dying; idx1 is alive and second-closest
        var posX = new float[3] { 1f, 3f, 10f };
        var posZ = new float[3] { 1f, 3f, 10f };
        var deathTimer = new float[3] { Dying, Alive, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(1, result);
    }

    [Fact]
    public void FindNearestAliveIndex_TiedDistance_ReturnsFirst()
    {
        // Both at distance sqrt(2); first encountered should win
        var posX = new float[2] { 1f, 1f };
        var posZ = new float[2] { 1f, 1f };
        var deathTimer = new float[2] { Alive, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 2);

        Assert.Equal(0, result);
    }

    [Fact]
    public void FindNearestAlivePosition_NoneAlive_ReturnsFallback()
    {
        var posX = new float[2] { 5f, 6f };
        var posZ = new float[2] { 5f, 6f };
        var deathTimer = new float[2] { Dying, Dying };

        var result = NearestEntityFinder.FindNearestAlivePosition(
            7f, 8f, posX, posZ, deathTimer, 2);

        Assert.Equal(7f, result.X);
        Assert.Equal(8f, result.Z);
    }

    [Fact]
    public void FindNearestAlivePosition_OneAlive_ReturnsItsPosition()
    {
        var posX = new float[3] { 1f, 2f, 4.5f };
        var posZ = new float[3] { 1f, 2f, 9.5f };
        var deathTimer = new float[3] { Dying, Dying, Alive };

        var result = NearestEntityFinder.FindNearestAlivePosition(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(4.5f, result.X);
        Assert.Equal(9.5f, result.Z);
    }

    [Fact]
    public void FindNearestAlivePosition_FromOrigin_NearestIsClosestToOrigin()
    {
        var posX = new float[3] { 10f, 1f, 5f };
        var posZ = new float[3] { 10f, 1f, 5f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        var result = NearestEntityFinder.FindNearestAlivePosition(
            0f, 0f, posX, posZ, deathTimer, 3);

        Assert.Equal(1f, result.X);
        Assert.Equal(1f, result.Z);
    }

    [Fact]
    public void FindNearestAlivePosition_NegativeCoords_HandledCorrectly()
    {
        // From (-5, -5): idx0=(-6,-6) distSq=2, idx1=(0,0) distSq=50, idx2=(-10,-10) distSq=50
        var posX = new float[3] { -6f, 0f, -10f };
        var posZ = new float[3] { -6f, 0f, -10f };
        var deathTimer = new float[3] { Alive, Alive, Alive };

        var result = NearestEntityFinder.FindNearestAlivePosition(
            -5f, -5f, posX, posZ, deathTimer, 3);

        Assert.Equal(-6f, result.X);
        Assert.Equal(-6f, result.Z);
    }

    [Fact]
    public void FindNearestAliveIndex_ActiveCountBoundsRespected()
    {
        // A closer alive entity beyond activeCount must be ignored
        var posX = new float[4] { 10f, 20f, 0.5f, 0.5f };
        var posZ = new float[4] { 10f, 20f, 0.5f, 0.5f };
        var deathTimer = new float[4] { Alive, Alive, Alive, Alive };

        int result = NearestEntityFinder.FindNearestAliveIndex(
            0f, 0f, posX, posZ, deathTimer, 2);

        Assert.Equal(0, result);
    }
}
