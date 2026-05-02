namespace Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public sealed class SaveSerializer
{
    public enum DeserializeStatus
    {
        Ok,
        Truncated,
        ChecksumMismatch,
        InvalidFormat,
        FromFutureVersion,
        InvalidVersion,
    }

    // File layout (little-endian):
    //   [Magic 4 byte ASCII "SBNX"]
    //   [Version 4 byte int32]
    //   [PayloadLen 4 byte uint32]
    //   [Payload N byte]
    //   [CRC32 4 byte uint32 over Payload]
    //
    // Payload encodes Dictionary<string, object>:
    //   [EntryCount 4 byte int32]
    //   For each entry:
    //     [KeyLen 2 byte uint16][Key UTF-8 bytes]
    //     [TypeTag 1 byte][Value]
    //   TypeTag:
    //     0 = int32  (4 byte)
    //     1 = int64  (8 byte)
    //     2 = double (8 byte IEEE754)
    //     3 = string ([Len 4 byte int32][UTF-8 bytes])
    //     4 = nested-dict (recursive payload, same shape)

    private static readonly byte[] Magic = new byte[] { (byte)'S', (byte)'B', (byte)'N', (byte)'X' };

    private const byte TagInt32 = 0;
    private const byte TagInt64 = 1;
    private const byte TagDouble = 2;
    private const byte TagString = 3;
    private const byte TagDict = 4;

    public void Serialize(Stream stream, IReadOnlyDictionary<string, object> data)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (data == null) throw new ArgumentNullException(nameof(data));

        var payloadStream = new MemoryStream();
        using (var payloadWriter = new BinaryWriter(payloadStream, Encoding.UTF8, leaveOpen: true))
        {
            WriteDict(payloadWriter, data);
        }

        var payload = payloadStream.ToArray();
        uint crc = Crc32.Compute(payload);

        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(SaveDataMigrator.CurrentVersion);
        writer.Write((uint)payload.Length);
        writer.Write(payload);
        writer.Write(crc);
    }

    public (Dictionary<string, object> Data, DeserializeStatus Status) Deserialize(Stream stream)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));

        // Read header into a buffer so we can detect truncation cleanly.
        var header = new byte[4 + 4 + 4];
        if (!ReadFully(stream, header, header.Length))
            return (null, DeserializeStatus.Truncated);

        for (int i = 0; i < Magic.Length; i++)
        {
            if (header[i] != Magic[i])
                return (null, DeserializeStatus.InvalidFormat);
        }

        int version = BitConverter.ToInt32(header, 4);
        uint payloadLen = BitConverter.ToUInt32(header, 8);

        if (version < 0)
            return (null, DeserializeStatus.InvalidVersion);

        if (payloadLen > int.MaxValue)
            return (null, DeserializeStatus.InvalidFormat);

        var payload = new byte[payloadLen];
        if (payloadLen > 0 && !ReadFully(stream, payload, payload.Length))
            return (null, DeserializeStatus.Truncated);

        var crcBytes = new byte[4];
        if (!ReadFully(stream, crcBytes, 4))
            return (null, DeserializeStatus.Truncated);

        uint storedCrc = BitConverter.ToUInt32(crcBytes, 0);
        uint actualCrc = Crc32.Compute(payload);
        if (storedCrc != actualCrc)
            return (null, DeserializeStatus.ChecksumMismatch);

        if (version > SaveDataMigrator.CurrentVersion)
            return (null, DeserializeStatus.FromFutureVersion);

        Dictionary<string, object> data;
        try
        {
            using var ms = new MemoryStream(payload, writable: false);
            using var reader = new BinaryReader(ms, Encoding.UTF8, leaveOpen: true);
            data = ReadDict(reader);

            if (ms.Position != ms.Length)
                return (null, DeserializeStatus.InvalidFormat);
        }
        catch (EndOfStreamException)
        {
            return (null, DeserializeStatus.Truncated);
        }
        catch (InvalidDataException)
        {
            return (null, DeserializeStatus.InvalidFormat);
        }

        return (data, DeserializeStatus.Ok);
    }

    private static void WriteDict(BinaryWriter writer, IReadOnlyDictionary<string, object> data)
    {
        writer.Write(data.Count);
        foreach (var kv in data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(kv.Key ?? string.Empty);
            if (keyBytes.Length > ushort.MaxValue)
                throw new InvalidDataException("Key too long.");
            writer.Write((ushort)keyBytes.Length);
            writer.Write(keyBytes);
            WriteValue(writer, kv.Value);
        }
    }

    private static void WriteValue(BinaryWriter writer, object value)
    {
        switch (value)
        {
            case int i:
                writer.Write(TagInt32);
                writer.Write(i);
                break;
            case long l:
                writer.Write(TagInt64);
                writer.Write(l);
                break;
            case double d:
                writer.Write(TagDouble);
                writer.Write(d);
                break;
            case string s:
                writer.Write(TagString);
                var sb = Encoding.UTF8.GetBytes(s);
                writer.Write(sb.Length);
                writer.Write(sb);
                break;
            case IReadOnlyDictionary<string, object> nested:
                writer.Write(TagDict);
                WriteDict(writer, nested);
                break;
            default:
                throw new InvalidDataException(
                    $"Unsupported value type: {(value == null ? "null" : value.GetType().FullName)}");
        }
    }

    private static Dictionary<string, object> ReadDict(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        if (count < 0)
            throw new InvalidDataException("Negative entry count.");

        var dict = new Dictionary<string, object>(count);
        for (int i = 0; i < count; i++)
        {
            ushort keyLen = reader.ReadUInt16();
            var keyBytes = reader.ReadBytes(keyLen);
            if (keyBytes.Length != keyLen)
                throw new EndOfStreamException();
            var key = Encoding.UTF8.GetString(keyBytes);
            byte tag = reader.ReadByte();
            object value = tag switch
            {
                TagInt32 => reader.ReadInt32(),
                TagInt64 => reader.ReadInt64(),
                TagDouble => reader.ReadDouble(),
                TagString => ReadString(reader),
                TagDict => ReadDict(reader),
                _ => throw new InvalidDataException($"Unknown type tag: {tag}"),
            };
            dict[key] = value;
        }
        return dict;
    }

    private static string ReadString(BinaryReader reader)
    {
        int len = reader.ReadInt32();
        if (len < 0)
            throw new InvalidDataException("Negative string length.");
        var bytes = reader.ReadBytes(len);
        if (bytes.Length != len)
            throw new EndOfStreamException();
        return Encoding.UTF8.GetString(bytes);
    }

    private static bool ReadFully(Stream stream, byte[] buffer, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(buffer, total, count - total);
            if (read <= 0)
                return false;
            total += read;
        }
        return true;
    }
}
