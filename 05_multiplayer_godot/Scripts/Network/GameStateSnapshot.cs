namespace SwarmSurvivor;

using System;
using System.Buffers.Binary;

public static class GameStateSnapshot
{
    public const byte Version = 1;
    public const int HeaderSize = 11;
    public const int VersionOffset = 0;
    public const int WaveOffset = 1;
    public const int EnemiesRemainingOffset = 3;
    public const int TotalKillsOffset = 5;
    public const int IsVictoryOffset = 9;
    public const int IsGameOverOffset = 10;

    public static int CalculateBufferSize() => HeaderSize;

    public static int Encode(byte[] buffer, int wave, int enemiesRemaining, int totalKills, bool isVictory, bool isGameOver)
    {
        if (buffer.Length < HeaderSize)
            throw new ArgumentException("Buffer too small for snapshot", nameof(buffer));

        buffer[VersionOffset] = Version;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(WaveOffset, 2), (ushort)(wave & 0xFFFF));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(EnemiesRemainingOffset, 2), (ushort)(enemiesRemaining & 0xFFFF));
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(TotalKillsOffset, 4), unchecked((uint)totalKills));
        buffer[IsVictoryOffset] = isVictory ? (byte)1 : (byte)0;
        buffer[IsGameOverOffset] = isGameOver ? (byte)1 : (byte)0;
        return HeaderSize;
    }

    public static bool Decode(byte[] buffer, int byteCount,
        out int wave, out int enemiesRemaining, out int totalKills,
        out bool isVictory, out bool isGameOver)
    {
        wave = 0;
        enemiesRemaining = 0;
        totalKills = 0;
        isVictory = false;
        isGameOver = false;

        if (buffer == null || buffer.Length < HeaderSize) return false;
        if (byteCount < HeaderSize) return false;
        if (buffer[VersionOffset] != Version) return false;

        wave = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(WaveOffset, 2));
        enemiesRemaining = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(EnemiesRemainingOffset, 2));
        totalKills = unchecked((int)BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(TotalKillsOffset, 4)));
        isVictory = buffer[IsVictoryOffset] != 0;
        isGameOver = buffer[IsGameOverOffset] != 0;
        return true;
    }
}
