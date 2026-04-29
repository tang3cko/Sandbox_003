using Xunit;

namespace SwarmSurvivor.Tests;

public class PoolIndexManagerTest
{
    [Fact]
    public void Allocate_EmptyPool_ReturnsZero()
    {
        var inUse = new bool[4];

        int index = PoolIndexManager.Allocate(inUse);

        Assert.Equal(0, index);
        Assert.True(inUse[0]);
        Assert.False(inUse[1]);
        Assert.False(inUse[2]);
        Assert.False(inUse[3]);
    }

    [Fact]
    public void Allocate_FirstSlotInUse_ReturnsOne()
    {
        var inUse = new bool[] { true, false, false, false };

        int index = PoolIndexManager.Allocate(inUse);

        Assert.Equal(1, index);
        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
    }

    [Fact]
    public void Allocate_FullPool_ReturnsUnallocatedIndex()
    {
        var inUse = new bool[] { true, true, true, true };

        int index = PoolIndexManager.Allocate(inUse);

        Assert.Equal(PoolIndexManager.UnallocatedIndex, index);
        Assert.Equal(-1, index);
        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
        Assert.True(inUse[2]);
        Assert.True(inUse[3]);
    }

    [Fact]
    public void Allocate_AllocatesFirstFreeSlot()
    {
        var inUse = new bool[] { true, false, true, false };

        int index = PoolIndexManager.Allocate(inUse);

        Assert.Equal(1, index);
        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
        Assert.True(inUse[2]);
        Assert.False(inUse[3]);
    }

    [Fact]
    public void Allocate_NullArray_ReturnsUnallocatedIndex()
    {
        int index = PoolIndexManager.Allocate(null);

        Assert.Equal(PoolIndexManager.UnallocatedIndex, index);
    }

    [Fact]
    public void Allocate_ZeroLengthArray_ReturnsUnallocatedIndex()
    {
        var inUse = new bool[0];

        int index = PoolIndexManager.Allocate(inUse);

        Assert.Equal(PoolIndexManager.UnallocatedIndex, index);
    }

    [Fact]
    public void Release_ValidIndex_FreesSlot()
    {
        var inUse = new bool[] { true, true, true, true };

        PoolIndexManager.Release(inUse, 2);

        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
        Assert.False(inUse[2]);
        Assert.True(inUse[3]);
    }

    [Fact]
    public void Release_OutOfRangeIndex_NoOp()
    {
        var inUse = new bool[] { true, true, true, true };

        PoolIndexManager.Release(inUse, 10);

        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
        Assert.True(inUse[2]);
        Assert.True(inUse[3]);
    }

    [Fact]
    public void Release_NegativeIndex_NoOp()
    {
        var inUse = new bool[] { true, true, true, true };

        PoolIndexManager.Release(inUse, -1);

        Assert.True(inUse[0]);
        Assert.True(inUse[1]);
        Assert.True(inUse[2]);
        Assert.True(inUse[3]);
    }

    [Fact]
    public void Release_AlreadyFreeSlot_NoOp()
    {
        var inUse = new bool[] { true, false, true, true };

        PoolIndexManager.Release(inUse, 1);

        Assert.True(inUse[0]);
        Assert.False(inUse[1]);
        Assert.True(inUse[2]);
        Assert.True(inUse[3]);
    }

    [Fact]
    public void Release_NullArray_NoOp()
    {
        // Should not throw.
        PoolIndexManager.Release(null, 0);
    }

    [Fact]
    public void CountInUse_VariousStates_ReturnsCorrectCount()
    {
        Assert.Equal(0, PoolIndexManager.CountInUse(new bool[4]));
        Assert.Equal(4, PoolIndexManager.CountInUse(new bool[] { true, true, true, true }));
        Assert.Equal(2, PoolIndexManager.CountInUse(new bool[] { true, false, true, false }));
        Assert.Equal(0, PoolIndexManager.CountInUse(null));
        Assert.Equal(0, PoolIndexManager.CountInUse(new bool[0]));
    }

    [Fact]
    public void CountFree_VariousStates_ReturnsCorrectCount()
    {
        Assert.Equal(4, PoolIndexManager.CountFree(new bool[4]));
        Assert.Equal(0, PoolIndexManager.CountFree(new bool[] { true, true, true, true }));
        Assert.Equal(2, PoolIndexManager.CountFree(new bool[] { true, false, true, false }));
        Assert.Equal(0, PoolIndexManager.CountFree(null));
        Assert.Equal(0, PoolIndexManager.CountFree(new bool[0]));
    }

    [Fact]
    public void AllocateThenRelease_RestoresFreeState()
    {
        var inUse = new bool[4];

        int a = PoolIndexManager.Allocate(inUse);
        int b = PoolIndexManager.Allocate(inUse);
        int c = PoolIndexManager.Allocate(inUse);

        Assert.Equal(0, a);
        Assert.Equal(1, b);
        Assert.Equal(2, c);
        Assert.Equal(3, PoolIndexManager.CountInUse(inUse));
        Assert.Equal(1, PoolIndexManager.CountFree(inUse));

        PoolIndexManager.Release(inUse, b);
        Assert.Equal(2, PoolIndexManager.CountInUse(inUse));

        // Next allocate should reuse slot 1 (the freed slot, lowest free index).
        int d = PoolIndexManager.Allocate(inUse);
        Assert.Equal(1, d);

        PoolIndexManager.Release(inUse, a);
        PoolIndexManager.Release(inUse, c);
        PoolIndexManager.Release(inUse, d);

        Assert.Equal(0, PoolIndexManager.CountInUse(inUse));
        Assert.Equal(4, PoolIndexManager.CountFree(inUse));
        Assert.False(inUse[0]);
        Assert.False(inUse[1]);
        Assert.False(inUse[2]);
        Assert.False(inUse[3]);
    }
}
