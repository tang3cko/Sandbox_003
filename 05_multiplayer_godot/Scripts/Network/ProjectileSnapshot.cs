namespace SwarmSurvivor;

using System;
using System.Buffers.Binary;

public static class ProjectileSnapshot
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
        float[] dirX, float[] dirZ,
        float[] lifetime,
        int[] ownerId,
        int[] colorIdx,
        int[] projectileId,
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
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 8, 4), dirX[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 12, 4), dirZ[i]);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.AsSpan(offset + 16, 4), lifetime[i]);
            buffer[offset + 20] = (byte)(ownerId[i] & 0xFF);
            buffer[offset + 21] = (byte)(colorIdx[i] & 0xFF);
            BinaryPrimitives.WriteUInt16LittleEndian(buffer.AsSpan(offset + 22, 2), (ushort)(projectileId[i] & 0xFFFF));
            offset += EntitySize;
        }
        return offset;
    }

    public static int Decode(
        byte[] buffer, int byteCount,
        float[] posX, float[] posZ,
        float[] dirX, float[] dirZ,
        float[] lifetime,
        int[] ownerId,
        int[] colorIdx,
        int[] projectileId)
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
            dirX[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 8, 4));
            dirZ[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 12, 4));
            lifetime[i] = BinaryPrimitives.ReadSingleLittleEndian(buffer.AsSpan(offset + 16, 4));
            ownerId[i] = buffer[offset + 20];
            colorIdx[i] = buffer[offset + 21];
            projectileId[i] = BinaryPrimitives.ReadUInt16LittleEndian(buffer.AsSpan(offset + 22, 2));
            offset += EntitySize;
        }
        return max;
    }
}
