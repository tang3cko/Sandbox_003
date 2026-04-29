using System.Collections.Generic;
using Xunit;

namespace SwarmSurvivor.Tests;

public class PlayerLifecycleCalculatorTest
{
    [Fact]
    public void AreAllPlayersDead_NoPlayers_ReturnsFalse()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>();
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_OneAlive_ReturnsFalse()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (false, false),
        };
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_OneDead_ReturnsTrue()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (true, false),
        };
        Assert.True(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_AllDead_ReturnsTrue()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (true, false),
            (true, false),
            (true, false),
        };
        Assert.True(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_MixedDeadAlive_ReturnsFalse()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (true, false),
            (true, false),
            (false, false),
        };
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_AllQueued_ReturnsFalse()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (true, true),
            (false, true),
        };
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_QueuedAreIgnored_PicksOnlyNonQueued()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (false, true),  // queued alive — ignored
            (true, false),  // non-queued dead
        };
        Assert.True(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_QueuedDeadIgnored()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>
        {
            (true, true),   // queued dead — ignored
            (false, true),  // queued alive — ignored
            (false, false), // non-queued alive
        };
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }

    [Fact]
    public void AreAllPlayersDead_NullEnumerable_ReturnsFalse()
    {
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(null));
    }

    [Fact]
    public void AreAllPlayersDead_EmptySequence_ReturnsFalse()
    {
        var states = new List<(bool IsDead, bool IsQueuedForDeletion)>();
        Assert.False(PlayerLifecycleCalculator.AreAllPlayersDead(states));
    }
}
