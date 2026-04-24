using Xunit;

namespace SwarmSurvivor.Tests;

public class UpgradeCalculatorTest
{
    [Fact]
    public void CreateInitial_SetsLevel1()
    {
        var state = UpgradeCalculator.CreateInitial(10);

        Assert.Equal(1, state.Level);
        Assert.Equal(0, state.CurrentXP);
        Assert.Equal(10, state.XPToNextLevel);
    }

    [Fact]
    public void CreateInitial_ClampsMinimumXP()
    {
        var state = UpgradeCalculator.CreateInitial(0);

        Assert.True(state.XPToNextLevel >= 1);
    }

    [Fact]
    public void GainXP_AccumulatesXP()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, 30, 100, 1.2f);

        Assert.Equal(30, result.State.CurrentXP);
        Assert.False(result.DidLevelUp);
    }

    [Fact]
    public void GainXP_TriggersLevelUp()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, 100, 100, 1.2f);

        Assert.Equal(2, result.State.Level);
        Assert.True(result.DidLevelUp);
    }

    [Fact]
    public void GainXP_OverflowCarriesToNextLevel()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, 130, 100, 1.2f);

        Assert.Equal(2, result.State.Level);
        Assert.Equal(30, result.State.CurrentXP);
        Assert.True(result.DidLevelUp);
    }

    [Fact]
    public void GainXP_UpdatesXPThresholdOnLevelUp()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, 100, 100, 1.5f);

        Assert.True(result.State.XPToNextLevel > 100);
    }

    [Fact]
    public void GainXP_IgnoresZeroAmount()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, 0, 100, 1.2f);

        Assert.Equal(0, result.State.CurrentXP);
        Assert.False(result.DidLevelUp);
    }

    [Fact]
    public void GainXP_IgnoresNegativeAmount()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        var result = UpgradeCalculator.GainXP(state, -10, 100, 1.2f);

        Assert.Equal(0, result.State.CurrentXP);
        Assert.False(result.DidLevelUp);
    }

    [Fact]
    public void CalculateXPForLevel_ReturnsBaseForLevel1()
    {
        int xp = UpgradeCalculator.CalculateXPForLevel(1, 100, 1.5f);

        Assert.Equal(100, xp);
    }

    [Fact]
    public void CalculateXPForLevel_ScalesWithGrowthFactor()
    {
        int level2 = UpgradeCalculator.CalculateXPForLevel(2, 100, 1.5f);
        int level3 = UpgradeCalculator.CalculateXPForLevel(3, 100, 1.5f);

        Assert.Equal(150, level2);
        Assert.Equal(225, level3);
    }

    [Fact]
    public void CalculateXPForLevel_MinimumOne()
    {
        int xp = UpgradeCalculator.CalculateXPForLevel(1, 0, 0.5f);

        Assert.True(xp >= 1);
    }

    [Fact]
    public void GetRandomChoices_ReturnsRequestedCount()
    {
        var available = new UpgradeChoice[]
        {
            new(1, "A", "a"),
            new(2, "B", "b"),
            new(3, "C", "c"),
            new(4, "D", "d"),
            new(5, "E", "e"),
        };

        var result = UpgradeCalculator.GetRandomChoices(available, 3, 42);

        Assert.Equal(3, result.Length);
    }

    [Fact]
    public void GetRandomChoices_NoDuplicates()
    {
        var available = new UpgradeChoice[]
        {
            new(1, "A", "a"),
            new(2, "B", "b"),
            new(3, "C", "c"),
            new(4, "D", "d"),
            new(5, "E", "e"),
        };

        var result = UpgradeCalculator.GetRandomChoices(available, 3, 42);
        var ids = new HashSet<int>();
        foreach (var choice in result)
        {
            Assert.True(ids.Add(choice.Id));
        }
    }

    [Fact]
    public void GetRandomChoices_DeterministicWithSeed()
    {
        var available = new UpgradeChoice[]
        {
            new(1, "A", "a"),
            new(2, "B", "b"),
            new(3, "C", "c"),
            new(4, "D", "d"),
            new(5, "E", "e"),
        };

        var result1 = UpgradeCalculator.GetRandomChoices(available, 3, 42);
        var result2 = UpgradeCalculator.GetRandomChoices(available, 3, 42);

        for (int i = 0; i < result1.Length; i++)
        {
            Assert.Equal(result1[i].Id, result2[i].Id);
        }
    }

    [Fact]
    public void GetRandomChoices_ClampsToAvailableCount()
    {
        var available = new UpgradeChoice[]
        {
            new(1, "A", "a"),
            new(2, "B", "b"),
        };

        var result = UpgradeCalculator.GetRandomChoices(available, 5, 42);

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void GetRandomChoices_HandlesEmptyArray()
    {
        var result = UpgradeCalculator.GetRandomChoices(Array.Empty<UpgradeChoice>(), 3, 42);

        Assert.Empty(result);
    }

    [Fact]
    public void GetRandomChoices_HandlesNull()
    {
        var result = UpgradeCalculator.GetRandomChoices(null, 3, 42);

        Assert.Empty(result);
    }

    [Fact]
    public void GetXPProgress_ReturnsZeroAtStart()
    {
        var state = UpgradeCalculator.CreateInitial(100);

        Assert.Equal(0f, UpgradeCalculator.GetXPProgress(state));
    }

    [Fact]
    public void GetXPProgress_ReturnsHalfAtMidpoint()
    {
        var state = UpgradeCalculator.CreateInitial(100);
        state.CurrentXP = 50;

        Assert.Equal(0.5f, UpgradeCalculator.GetXPProgress(state), 2);
    }

    [Fact]
    public void GetXPProgress_ClampsToOne()
    {
        var state = new PlayerProgression { Level = 1, CurrentXP = 200, XPToNextLevel = 100 };

        Assert.Equal(1f, UpgradeCalculator.GetXPProgress(state));
    }
}
