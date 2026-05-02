namespace Persistence.Tests;
using System.Text;
using Xunit;

public class Crc32Tests
{
    [Fact]
    public void EmptyInput_ProducesZero()
    {
        // Standard CRC-32/ISO-HDLC: empty buffer -> 0x00000000
        Assert.Equal(0u, Crc32.Compute(System.ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void StandardTestVector_123456789()
    {
        // CRC-32/ISO-HDLC standard test vector ("123456789") -> 0xCBF43926
        var data = Encoding.ASCII.GetBytes("123456789");
        Assert.Equal(0xCBF43926u, Crc32.Compute(data));
    }

    [Fact]
    public void SingleByte_a()
    {
        // CRC-32/ISO-HDLC of ASCII "a" -> 0xE8B7BE43 (well-known fixed value)
        var data = Encoding.ASCII.GetBytes("a");
        Assert.Equal(0xE8B7BE43u, Crc32.Compute(data));
    }

    [Fact]
    public void IsDeterministic()
    {
        var data = Encoding.ASCII.GetBytes("hello world");
        var first = Crc32.Compute(data);
        var second = Crc32.Compute(data);
        Assert.Equal(first, second);
    }
}
