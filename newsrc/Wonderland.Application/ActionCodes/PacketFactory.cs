using System.Buffers.Binary;
using System.Text;

namespace Wonderland.Application.ActionCodes;

/// <summary>
/// Fluent packet builder — creates WLO-format packets.
/// Usage: PacketFactory.Create(actionCode, subAction).WriteUInt32(charId).Build()
///
/// WLO packet format: [0x44F4 header (2)] [length (2)] [payload]
/// All multi-byte values are little-endian.
/// </summary>
public sealed class PacketFactory
{
    private const ushort Header = 0x44F4;
    private const int HeaderSize = 4;

    private readonly MemoryStream _stream;
    private readonly BinaryWriter _writer;

    private PacketFactory(byte actionCode, byte subAction)
    {
        _stream = new MemoryStream(64);
        _writer = new BinaryWriter(_stream);
        _writer.Write(actionCode);
        _writer.Write(subAction);
    }

    /// <summary>Create a new packet builder</summary>
    public static PacketFactory Create(byte ac, byte sub = 0) => new(ac, sub);

    // --- Primitive writers (fluent) ---
    public PacketFactory U8(byte value) { _writer.Write(value); return this; }
    public PacketFactory U16(ushort value) { _writer.Write(value); return this; }
    public PacketFactory U32(uint value) { _writer.Write(value); return this; }
    public PacketFactory I32(int value) { _writer.Write(value); return this; }
    public PacketFactory I64(long value) { _writer.Write(value); return this; }

    /// <summary>Write a fixed-length string (padded with \0)</summary>
    public PacketFactory Str(string value, int fixedLength)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var buffer = new byte[fixedLength];
        Buffer.BlockCopy(bytes, 0, buffer, 0, Math.Min(bytes.Length, fixedLength));
        _writer.Write(buffer);
        return this;
    }

    /// <summary>Write a length-prefixed string (1-byte length prefix)</summary>
    public PacketFactory PStr(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _writer.Write((byte)bytes.Length);
        _writer.Write(bytes);
        return this;
    }

    /// <summary>Write a null-terminated string</summary>
    public PacketFactory NStr(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        _writer.Write(bytes);
        _writer.Write((byte)0);
        return this;
    }

    /// <summary>Write raw bytes</summary>
    public PacketFactory Raw(byte[] data) { _writer.Write(data); return this; }

    /// <summary>Write N zero bytes as padding</summary>
    public PacketFactory Pad(int count)
    {
        for (int i = 0; i < count; i++) _writer.Write((byte)0);
        return this;
    }

    /// <summary>
    /// Build the final packet with WLO header.
    /// Returns the complete byte array ready to send.
    /// </summary>
    public byte[] Build()
    {
        var payload = _stream.ToArray();
        var packet = new byte[HeaderSize + payload.Length];

        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), Header);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), (ushort)payload.Length);
        payload.CopyTo(packet.AsSpan(HeaderSize));

        return packet;
    }

    /// <summary>
    /// Build payload only (without WLO header) — for embedding in larger packets
    /// </summary>
    public byte[] BuildPayload() => _stream.ToArray();
}
