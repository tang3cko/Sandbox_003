using System.Collections.Generic;
using Xunit;

namespace SwarmSurvivor.Tests;

public class InterpolationIdMatcherTest
{
    [Fact]
    public void BuildIdToIndex_EmptyPrevious_DictionaryEmpty()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 99, 42 }; // values present but ignored due to count=0

        InterpolationIdMatcher.BuildIdToIndex(ids, 0, dict);

        Assert.Empty(dict);
    }

    [Fact]
    public void BuildIdToIndex_FillsDictionaryCorrectly()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 10, 20, 30, 40 };

        InterpolationIdMatcher.BuildIdToIndex(ids, 4, dict);

        Assert.Equal(4, dict.Count);
        Assert.Equal(0, dict[10]);
        Assert.Equal(1, dict[20]);
        Assert.Equal(2, dict[30]);
        Assert.Equal(3, dict[40]);
    }

    [Fact]
    public void BuildIdToIndex_ResetsExistingEntries()
    {
        var dict = new Dictionary<int, int>();
        var first = new int[] { 1, 2, 3 };
        var second = new int[] { 100, 200 };

        InterpolationIdMatcher.BuildIdToIndex(first, 3, dict);
        InterpolationIdMatcher.BuildIdToIndex(second, 2, dict);

        Assert.Equal(2, dict.Count);
        Assert.False(dict.ContainsKey(1));
        Assert.False(dict.ContainsKey(2));
        Assert.False(dict.ContainsKey(3));
        Assert.Equal(0, dict[100]);
        Assert.Equal(1, dict[200]);
    }

    [Fact]
    public void BuildIdToIndex_DuplicateIds_LastWins()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 7, 7, 7 };

        InterpolationIdMatcher.BuildIdToIndex(ids, 3, dict);

        Assert.Single(dict);
        Assert.Equal(2, dict[7]);
    }

    [Fact]
    public void FindOldIndex_PresentId_ReturnsIndex()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 5, 6, 7 };
        InterpolationIdMatcher.BuildIdToIndex(ids, 3, dict);

        Assert.Equal(0, InterpolationIdMatcher.FindOldIndex(5, dict));
        Assert.Equal(1, InterpolationIdMatcher.FindOldIndex(6, dict));
        Assert.Equal(2, InterpolationIdMatcher.FindOldIndex(7, dict));
    }

    [Fact]
    public void FindOldIndex_AbsentId_ReturnsMinusOne()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 1, 2, 3 };
        InterpolationIdMatcher.BuildIdToIndex(ids, 3, dict);

        Assert.Equal(-1, InterpolationIdMatcher.FindOldIndex(999, dict));
        Assert.Equal(-1, InterpolationIdMatcher.FindOldIndex(0, dict));
    }

    [Fact]
    public void FindOldIndex_AfterRebuild_NewMapping()
    {
        var dict = new Dictionary<int, int>();
        var initial = new int[] { 11, 22, 33 };
        InterpolationIdMatcher.BuildIdToIndex(initial, 3, dict);

        Assert.Equal(0, InterpolationIdMatcher.FindOldIndex(11, dict));

        var next = new int[] { 44, 55 };
        InterpolationIdMatcher.BuildIdToIndex(next, 2, dict);

        Assert.Equal(-1, InterpolationIdMatcher.FindOldIndex(11, dict));
        Assert.Equal(-1, InterpolationIdMatcher.FindOldIndex(22, dict));
        Assert.Equal(-1, InterpolationIdMatcher.FindOldIndex(33, dict));
        Assert.Equal(0, InterpolationIdMatcher.FindOldIndex(44, dict));
        Assert.Equal(1, InterpolationIdMatcher.FindOldIndex(55, dict));
    }

    [Fact]
    public void BuildIdToIndex_PreservesOrderForUniqueIds()
    {
        var dict = new Dictionary<int, int>();
        var ids = new int[] { 1, 2, 3 };

        InterpolationIdMatcher.BuildIdToIndex(ids, 3, dict);

        Assert.Equal(0, InterpolationIdMatcher.FindOldIndex(1, dict));
        Assert.Equal(1, InterpolationIdMatcher.FindOldIndex(2, dict));
        Assert.Equal(2, InterpolationIdMatcher.FindOldIndex(3, dict));
    }
}
