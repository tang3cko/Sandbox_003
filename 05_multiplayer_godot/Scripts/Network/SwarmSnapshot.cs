namespace SwarmSurvivor;

using System;
using System.Buffers.Binary;

public static class SwarmSnapshot
{
    public const byte Version = 1;
    public const int HeaderSize = 3;
    public const int VersionOffset = 0;
    public const int CountOffset = 1;
    public const int EntitySize = 24;

    public const int MaxCount = ushort.MaxValue;

    public static int CalculateBufferSize(int count) => HeaderSize + EntitySize * count;

    public static int Encode(
        byte[] buffer,
        float[] posX, float[] posZ,
        float[] velX, float[] velZ,
        int[] typeIndex,
        float[] flashTimer,
        float[] deathTimer,
        int[] entityId,
        int count)
    {
        if (count < 0 || count > MaxCount)
            throw new ArgumentOutOfRangeException(nameof(count));
        int needed = CalculateBufferSize(count);
        if (buffer.Length < needed)
            throw new ArgumentException("Buffer too small for snapshot", nameof(buffer));

        buffer[VersionOffset] = Version;
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(CountOffset, 2), (ushort)count);

        int offset = HeaderSize;
        for (int i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 0, 4), posX[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 4, 4), posZ[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 8, 4), velX[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 12, 4), velZ[i]);
            buffer[offset + 16] = (byte)(typeIndex[i] & 0xFF);
            buffer[offset + 17] = EncodeFlashTimer(flashTimer[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 18, 4), deathTimer[i]);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset + 22, 2), (ushort)(entityId[i] & 0xFFFF));
            offset += EntitySize;
        }
        return offset;
    }

    public static int Decode(
        byte[] buffer, int byteCount,
        float[] posX, float[] posZ,
        float[] velX, float[] velZ,
        int[] typeIndex,
        float[] flashTimer,
        float[] deathTimer,
        int[] entityId)
    {
        if (byteCount < HeaderSize) return 0;
        if (buffer[VersionOffset] != Version) return 0;

        int count = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(CountOffset, 2));
        int expected = CalculateBufferSize(count);
        if (byteCount < expected) return 0;

        int max = Math.Min(count, posX.Length);
        int offset = HeaderSize;
        for (int i = 0; i < max; i++)
        {
            posX[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 0, 4));
            posZ[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 4, 4));
            velX[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 8, 4));
            velZ[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 12, 4));
            typeIndex[i] = buffer[offset + 16];
            flashTimer[i] = DecodeFlashTimer(buffer[offset + 17]);
            deathTimer[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 18, 4));
            entityId[i] = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 22, 2));
            offset += EntitySize;
        }
        return max;
    }

    public static byte EncodeFlashTimer(float timer)
    {
        float normalized = timer / SwarmCalculator.DefaultFlashDuration;
        if (normalized <= 0f) return 0;
        if (normalized >= 1f) return 255;
        return (byte)(normalized * 255f + 0.5f);
    }

    public static float DecodeFlashTimer(byte value)
    {
        return (value / 255f) * SwarmCalculator.DefaultFlashDuration;
    }
}
