namespace Persistence.Tests;
using System.Collections.Generic;
using System.IO;
using Xunit;

public class SaveSerializerTests
{
    private static byte[] SerializeToBytes(IReadOnlyDictionary<string, object> data)
    {
        var ms = new MemoryStream();
        new SaveSerializer().Serialize(ms, data);
        return ms.ToArray();
    }

    [Fact]
    public void Roundtrip_EmptyDict()
    {
        var input = new Dictionary<string, object>();
        var bytes = SerializeToBytes(input);
        var ms = new MemoryStream(bytes);
        var (data, status) = new SaveSerializer().Deserialize(ms);
        Assert.Equal(SaveSerializer.DeserializeStatus.Ok, status);
        Assert.NotNull(data);
        Assert.Empty(data);
    }

    [Theory]
    [MemberData(nameof(RoundtripCases))]
    public void Roundtrip_PreservesTypedValues(string label, Dictionary<string, object> input)
    {
        Assert.NotNull(label);
        var bytes = SerializeToBytes(input);
        var ms = new MemoryStream(bytes);
        var (data, status) = new SaveSerializer().Deserialize(ms);

        Assert.Equal(SaveSerializer.DeserializeStatus.Ok, status);
        AssertDictEquals(input, data);
    }

    public static IEnumerable<object[]> RoundtripCases()
    {
        yield return new object[]
        {
            "int32",
            new Dictionary<string, object> { ["money"] = 1234, ["day"] = 7 }
        };
        yield return new object[]
        {
            "int64",
            new Dictionary<string, object> { ["seq"] = 9_000_000_000L }
        };
        yield return new object[]
        {
            "double",
            new Dictionary<string, object> { ["saved_at"] = 1714521600.5d }
        };
        yield return new object[]
        {
            "string",
            new Dictionary<string, object> { ["player_name"] = "テスト Player" }
        };
        yield return new object[]
        {
            "mixed",
            new Dictionary<string, object>
            {
                ["save_version"] = 1,
                ["seq"] = 42L,
                ["saved_at"] = 1.5d,
                ["name"] = "alice",
            }
        };
        yield return new object[]
        {
            "nested",
            new Dictionary<string, object>
            {
                ["save_version"] = 1,
                ["inventory"] = new Dictionary<string, object>
                {
                    ["apple"] = 3,
                    ["potion"] = 1,
                },
            }
        };
    }

    [Fact]
    public void Truncated_ReturnsTruncatedStatus()
    {
        var input = new Dictionary<string, object> { ["money"] = 100, ["day"] = 1 };
        var bytes = SerializeToBytes(input);
        // Cut off the trailing CRC (and a byte of payload).
        var truncated = new byte[bytes.Length - 5];
        System.Array.Copy(bytes, truncated, truncated.Length);

        var ms = new MemoryStream(truncated);
        var (data, status) = new SaveSerializer().Deserialize(ms);

        Assert.Equal(SaveSerializer.DeserializeStatus.Truncated, status);
        Assert.Null(data);
    }

    [Fact]
    public void Truncated_EmptyStream()
    {
        var ms = new MemoryStream(new byte[0]);
        var (data, status) = new SaveSerializer().Deserialize(ms);
        Assert.Equal(SaveSerializer.DeserializeStatus.Truncated, status);
        Assert.Null(data);
    }

    [Fact]
    public void ChecksumMismatch_FlippedPayloadByte()
    {
        var input = new Dictionary<string, object> { ["money"] = 1234 };
        var bytes = SerializeToBytes(input);

        // Header is 12 bytes; flip a byte in the payload region (offset 12).
        bytes[12] ^= 0xFF;

        var ms = new MemoryStream(bytes);
        var (data, status) = new SaveSerializer().Deserialize(ms);

        Assert.Equal(SaveSerializer.DeserializeStatus.ChecksumMismatch, status);
        Assert.Null(data);
    }

    [Fact]
    public void InvalidMagic_ReturnsInvalidFormat()
    {
        var input = new Dictionary<string, object> { ["money"] = 1 };
        var bytes = SerializeToBytes(input);
        bytes[0] = (byte)'X';
        bytes[1] = (byte)'X';
        bytes[2] = (byte)'X';
        bytes[3] = (byte)'X';

        var ms = new MemoryStream(bytes);
        var (data, status) = new SaveSerializer().Deserialize(ms);

        Assert.Equal(SaveSerializer.DeserializeStatus.InvalidFormat, status);
        Assert.Null(data);
    }

    [Fact]
    public void FromFutureVersion_ReturnsFromFutureStatus()
    {
        var input = new Dictionary<string, object> { ["money"] = 1 };
        var bytes = SerializeToBytes(input);

        // Overwrite version (bytes [4..8]) with CurrentVersion + 999.
        int futureVersion = SaveDataMigrator.CurrentVersion + 999;
        var futureBytes = System.BitConverter.GetBytes(futureVersion);
        bytes[4] = futureBytes[0];
        bytes[5] = futureBytes[1];
        bytes[6] = futureBytes[2];
        bytes[7] = futureBytes[3];

        var ms = new MemoryStream(bytes);
        var (data, status) = new SaveSerializer().Deserialize(ms);

        Assert.Equal(SaveSerializer.DeserializeStatus.FromFutureVersion, status);
        Assert.Null(data);
    }

    private static void AssertDictEquals(
        IReadOnlyDictionary<string, object> expected,
        IReadOnlyDictionary<string, object> actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Count, actual.Count);
        foreach (var kv in expected)
        {
            Assert.True(actual.ContainsKey(kv.Key), $"missing key: {kv.Key}");
            var actualValue = actual[kv.Key];
            if (kv.Value is IReadOnlyDictionary<string, object> nestedExpected)
            {
                Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(actualValue);
                AssertDictEquals(nestedExpected, (IReadOnlyDictionary<string, object>)actualValue);
            }
            else
            {
                Assert.Equal(kv.Value, actualValue);
                Assert.Equal(kv.Value.GetType(), actualValue.GetType());
            }
        }
    }
}
