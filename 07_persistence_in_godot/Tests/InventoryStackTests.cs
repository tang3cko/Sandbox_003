namespace Persistence.Tests;
using System.Collections.Generic;
using Xunit;

public class InventoryStackTests
{
    [Fact]
    public void Add_NewItem_CreatesEntry()
    {
        var src = new Dictionary<string, int>();
        var result = InventoryStack.Add(src, "apple", 3);
        Assert.Equal(3, result["apple"]);
    }

    [Fact]
    public void Add_ExistingItem_AccumulatesCount()
    {
        var src = new Dictionary<string, int> { ["apple"] = 2 };
        var result = InventoryStack.Add(src, "apple", 5);
        Assert.Equal(7, result["apple"]);
    }

    [Fact]
    public void Add_DoesNotMutateInput()
    {
        var src = new Dictionary<string, int> { ["apple"] = 2 };
        InventoryStack.Add(src, "apple", 5);
        Assert.Equal(2, src["apple"]);
    }

    [Fact]
    public void Add_NonPositiveAmount_LeavesCountUnchanged()
    {
        var src = new Dictionary<string, int> { ["apple"] = 2 };
        var zero = InventoryStack.Add(src, "apple", 0);
        var neg = InventoryStack.Add(src, "apple", -3);
        Assert.Equal(2, zero["apple"]);
        Assert.Equal(2, neg["apple"]);
    }

    [Fact]
    public void Add_EmptyOrNullItemId_NoOp()
    {
        var src = new Dictionary<string, int> { ["apple"] = 2 };
        var emptyResult = InventoryStack.Add(src, "", 5);
        var nullResult = InventoryStack.Add(src, null, 5);
        Assert.Single(emptyResult);
        Assert.Single(nullResult);
    }

    [Fact]
    public void Remove_DecrementsCount()
    {
        var src = new Dictionary<string, int> { ["apple"] = 5 };
        var result = InventoryStack.Remove(src, "apple", 2);
        Assert.Equal(3, result["apple"]);
    }

    [Fact]
    public void Remove_ToZero_RemovesKey()
    {
        var src = new Dictionary<string, int> { ["apple"] = 3 };
        var result = InventoryStack.Remove(src, "apple", 3);
        Assert.False(result.ContainsKey("apple"));
    }

    [Fact]
    public void Remove_ExceedingCount_RemovesKey()
    {
        var src = new Dictionary<string, int> { ["apple"] = 3 };
        var result = InventoryStack.Remove(src, "apple", 100);
        Assert.False(result.ContainsKey("apple"));
    }

    [Fact]
    public void Remove_DoesNotMutateInput()
    {
        var src = new Dictionary<string, int> { ["apple"] = 5 };
        InventoryStack.Remove(src, "apple", 2);
        Assert.Equal(5, src["apple"]);
    }

    [Fact]
    public void Remove_MissingItem_ReturnsCopy()
    {
        var src = new Dictionary<string, int> { ["apple"] = 5 };
        var result = InventoryStack.Remove(src, "potion", 1);
        Assert.Equal(5, result["apple"]);
    }

    [Fact]
    public void GetCount_ReturnsZeroForMissingItem()
    {
        var src = new Dictionary<string, int>();
        Assert.Equal(0, InventoryStack.GetCount(src, "apple"));
    }

    [Fact]
    public void GetCount_ReturnsZeroForNullSource()
    {
        Assert.Equal(0, InventoryStack.GetCount(null, "apple"));
    }
}
