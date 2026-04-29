using Xunit;

namespace SwarmSurvivor.Tests;

public class PeerIdParserTest
{
    [Fact]
    public void TryParseFromName_ValidNumeric_ReturnsTrueAndId()
    {
        Assert.True(PeerIdParser.TryParseFromName("1", out var id));
        Assert.Equal(1, id);
    }

    [Fact]
    public void TryParseFromName_LargePositive_ReturnsTrueAndId()
    {
        Assert.True(PeerIdParser.TryParseFromName("217212806", out var id));
        Assert.Equal(217212806, id);
    }

    [Fact]
    public void TryParseFromName_Empty_ReturnsFalseAndZero()
    {
        Assert.False(PeerIdParser.TryParseFromName("", out var id));
        Assert.Equal(0, id);
    }

    [Fact]
    public void TryParseFromName_Null_ReturnsFalseAndZero()
    {
        Assert.False(PeerIdParser.TryParseFromName(null, out var id));
        Assert.Equal(0, id);
    }

    [Fact]
    public void TryParseFromName_NonNumeric_ReturnsFalseAndZero()
    {
        Assert.False(PeerIdParser.TryParseFromName("abc", out var id));
        Assert.Equal(0, id);
    }

    [Fact]
    public void TryParseFromName_LeadingSpace_ReturnsFalseAndZero()
    {
        Assert.False(PeerIdParser.TryParseFromName(" 1", out var id));
        Assert.Equal(0, id);
    }

    [Fact]
    public void TryParseFromName_Negative_ReturnsTrueAndId()
    {
        Assert.True(PeerIdParser.TryParseFromName("-5", out var id));
        Assert.Equal(-5, id);
    }

    [Fact]
    public void TryParseFromName_Overflow_ReturnsFalseAndZero()
    {
        Assert.False(PeerIdParser.TryParseFromName("99999999999999999999", out var id));
        Assert.Equal(0, id);
    }
}
