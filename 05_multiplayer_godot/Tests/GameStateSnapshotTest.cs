using Xunit;

namespace SwarmSurvivor.Tests;

public class GameStateSnapshotTest
{
    [Fact]
    public void CalculateBufferSize_ReturnsHeaderSize()
    {
        // Layout: Version(1) + Wave(2) + EnemiesRemaining(2) + TotalKills(4) + IsVictory(1) + IsGameOver(1) = 11
        Assert.Equal(11, GameStateSnapshot.CalculateBufferSize());
    }

    [Fact]
    public void RoundTrip_NormalValues_PreservesAllFields()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        int written = GameStateSnapshot.Encode(buf, wave: 5, enemiesRemaining: 12, totalKills: 200, isVictory: false, isGameOver: false);
        Assert.Equal(GameStateSnapshot.CalculateBufferSize(), written);

        bool ok = GameStateSnapshot.Decode(buf, written,
            out int wave, out int enemiesRemaining, out int totalKills,
            out bool isVictory, out bool isGameOver);

        Assert.True(ok);
        Assert.Equal(5, wave);
        Assert.Equal(12, enemiesRemaining);
        Assert.Equal(200, totalKills);
        Assert.False(isVictory);
        Assert.False(isGameOver);
    }

    [Fact]
    public void RoundTrip_ZeroValues_PreservesAllFields()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf, 0, 0, 0, false, false);

        bool ok = GameStateSnapshot.Decode(buf, buf.Length,
            out int wave, out int enemiesRemaining, out int totalKills,
            out bool isVictory, out bool isGameOver);

        Assert.True(ok);
        Assert.Equal(0, wave);
        Assert.Equal(0, enemiesRemaining);
        Assert.Equal(0, totalKills);
        Assert.False(isVictory);
        Assert.False(isGameOver);
    }

    [Fact]
    public void RoundTrip_MaxValues_PreservesAllFields()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf,
            wave: ushort.MaxValue,
            enemiesRemaining: ushort.MaxValue,
            totalKills: unchecked((int)uint.MaxValue),
            isVictory: true,
            isGameOver: true);

        bool ok = GameStateSnapshot.Decode(buf, buf.Length,
            out int wave, out int enemiesRemaining, out int totalKills,
            out bool isVictory, out bool isGameOver);

        Assert.True(ok);
        Assert.Equal(ushort.MaxValue, wave);
        Assert.Equal(ushort.MaxValue, enemiesRemaining);
        Assert.Equal(unchecked((int)uint.MaxValue), totalKills);
        Assert.True(isVictory);
        Assert.True(isGameOver);
    }

    [Fact]
    public void Decode_WrongVersion_ReturnsFalse()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf, 3, 7, 50, true, false);
        buf[0] = (byte)(GameStateSnapshot.Version + 1);

        bool ok = GameStateSnapshot.Decode(buf, buf.Length,
            out _, out _, out _, out _, out _);

        Assert.False(ok);
    }

    [Fact]
    public void Decode_BufferTooSmall_ReturnsFalse()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize() - 1];
        bool ok = GameStateSnapshot.Decode(buf, buf.Length,
            out _, out _, out _, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void Decode_ByteCountTooSmall_ReturnsFalse()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        bool ok = GameStateSnapshot.Decode(buf, GameStateSnapshot.CalculateBufferSize() - 1,
            out _, out _, out _, out _, out _);
        Assert.False(ok);
    }

    [Fact]
    public void RoundTrip_VictoryFlag_Preserved()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf, 10, 0, 999, isVictory: true, isGameOver: false);

        GameStateSnapshot.Decode(buf, buf.Length,
            out _, out _, out _, out bool isVictory, out bool isGameOver);

        Assert.True(isVictory);
        Assert.False(isGameOver);
    }

    [Fact]
    public void RoundTrip_GameOverFlag_Preserved()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf, 4, 8, 75, isVictory: false, isGameOver: true);

        GameStateSnapshot.Decode(buf, buf.Length,
            out _, out _, out _, out bool isVictory, out bool isGameOver);

        Assert.False(isVictory);
        Assert.True(isGameOver);
    }

    [Fact]
    public void Encode_AlwaysWritesCurrentVersion()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize()];
        GameStateSnapshot.Encode(buf, 1, 2, 3, false, false);
        Assert.Equal(GameStateSnapshot.Version, buf[0]);
    }

    [Fact]
    public void Encode_BufferTooSmall_Throws()
    {
        var buf = new byte[GameStateSnapshot.CalculateBufferSize() - 1];
        Assert.Throws<System.ArgumentException>(() =>
            GameStateSnapshot.Encode(buf, 1, 2, 3, false, false));
    }
}
