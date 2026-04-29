using Xunit;

namespace SwarmSurvivor.Tests;

public class TickAccumulatorTest
{
    private const float Step60Hz = 1f / 60f;

    [Fact]
    public void Advance_AccumulatorBelowStep_NoTicks()
    {
        var (acc, ticks) = TickAccumulator.Advance(0.005f, 0.005f, Step60Hz, 5);

        Assert.Equal(0, ticks);
        Assert.Equal(0.01f, acc, precision: 5);
    }

    [Fact]
    public void Advance_AccumulatorAboveStep_OneTick()
    {
        var (acc, ticks) = TickAccumulator.Advance(0.02f, 0.005f, Step60Hz, 5);

        Assert.Equal(1, ticks);
        Assert.Equal(0.025f - Step60Hz, acc, precision: 5);
    }

    [Fact]
    public void Advance_AccumulatorMuchGreater_MultipleTicks()
    {
        var (acc, ticks) = TickAccumulator.Advance(0f, 0.05f, 0.01f, 10);

        Assert.Equal(5, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_AtExactStep_OneTick()
    {
        var (acc, ticks) = TickAccumulator.Advance(0f, Step60Hz, Step60Hz, 5);

        Assert.Equal(1, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_DefaultStep_60Hz()
    {
        var (acc, ticks) = TickAccumulator.Advance(0f, Step60Hz);

        Assert.Equal(1, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_LongPause_CappedAtMaxTicks()
    {
        var (acc, ticks) = TickAccumulator.Advance(0f, 10f, Step60Hz, 5);

        Assert.Equal(5, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_ZeroDt_NoChange()
    {
        var (acc, ticks) = TickAccumulator.Advance(0.005f, 0f, Step60Hz, 5);

        Assert.Equal(0, ticks);
        Assert.Equal(0.005f, acc, precision: 5);
    }

    [Fact]
    public void Advance_NegativeFixedStep_Defensive_NoTicks()
    {
        var (acc, ticks) = TickAccumulator.Advance(0.5f, 0.1f, -0.01f, 5);

        Assert.Equal(0, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_ZeroFixedStep_Defensive_NoTicks()
    {
        var (acc, ticks) = TickAccumulator.Advance(0.5f, 0.1f, 0f, 5);

        Assert.Equal(0, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }

    [Fact]
    public void Advance_AccumulatesAcrossCalls_PreservesResidual()
    {
        var (acc1, ticks1) = TickAccumulator.Advance(0f, 0.01f, Step60Hz, 5);
        Assert.Equal(0, ticks1);
        Assert.Equal(0.01f, acc1, precision: 5);

        var (acc2, ticks2) = TickAccumulator.Advance(acc1, 0.01f, Step60Hz, 5);
        Assert.Equal(1, ticks2);
        Assert.Equal(0.02f - Step60Hz, acc2, precision: 5);
    }

    [Fact]
    public void Advance_DefaultMaxTicksFive_Capped()
    {
        var (acc, ticks) = TickAccumulator.Advance(0f, 10f);

        Assert.Equal(5, ticks);
        Assert.Equal(0f, acc, precision: 5);
    }
}
