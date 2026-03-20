using System.Buffers.Binary;
using Wonderland.Application.Interfaces;

namespace Wonderland.Infrastructure.Network;

/// <summary>
/// WLO packet format: [0x44F4 header (2 bytes)] [length (2 bytes)] [payload]
/// Replaces RCLibrary.Core.Networking.IPacket
/// </summary>
public static class PacketConstants
{
    public const ushort Header = 0x44F4;
    public const int HeaderSize = 4; // 2 bytes header + 2 bytes length
}

/// <summary>
/// Incoming packet reader — replaces RecievePacket from RCLibrary
/// </summary>
public class ReceivePacket : IReceivePacket
{
    private readonly byte[] _data;
    private int _position;

    public byte ActionCode { get; }
    public byte SubAction { get; }
    public int Length => _data.Length;

    public ReceivePacket(byte[] rawPayload)
    {
        _data = rawPayload;
        _position = 0;

        if (_data.Length >= 1)
            ActionCode = ReadByte();
        if (_data.Length >= 2)
            SubAction = ReadByte();
    }

    public byte ReadByte()
    {
        if (_position >= _data.Length) return 0;
        return _data[_position++];
    }

    public ushort ReadUInt16()
    {
        if (_position + 2 > _data.Length) return 0;
        var val = BinaryPrimitives.ReadUInt16LittleEndian(_data.AsSpan(_position, 2));
        _position += 2;
        return val;
    }

    public uint ReadUInt32()
    {
        if (_position + 4 > _data.Length) return 0;
        var val = BinaryPrimitives.ReadUInt32LittleEndian(_data.AsSpan(_position, 4));
        _position += 4;
        return val;
    }

    public string ReadString(int length)
    {
        if (_position + length > _data.Length)
            length = _data.Length - _position;
        var str = System.Text.Encoding.UTF8.GetString(_data, _position, length);
        _position += length;
        return str.TrimEnd('\0');
    }

    public byte[] ReadBytes(int count)
    {
        if (_position + count > _data.Length)
            count = _data.Length - _position;
        var result = new byte[count];
        Array.Copy(_data, _position, result, 0, count);
        _position += count;
        return result;
    }

    public void Skip(int count) => _position += count;
    public int Remaining => _data.Length - _position;
}

/// <summary>
/// Outgoing packet builder — replaces SendPacket from RCLibrary
/// </summary>
public class SendPacket
{
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;

    public SendPacket(byte actionCode, byte subAction = 0)
    {
        _writer = new BinaryWriter(_stream);
        _writer.Write(actionCode);
        _writer.Write(subAction);
    }

    public SendPacket WriteByte(byte value) { _writer.Write(value); return this; }
    public SendPacket WriteUInt16(ushort value) { _writer.Write(value); return this; }
    public SendPacket WriteUInt32(uint value) { _writer.Write(value); return this; }
    public SendPacket WriteInt32(int value) { _writer.Write(value); return this; }

    public SendPacket WriteString(string value, int fixedLength)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        var buffer = new byte[fixedLength];
        Array.Copy(bytes, buffer, Math.Min(bytes.Length, fixedLength));
        _writer.Write(buffer);
        return this;
    }

    public SendPacket WriteBytes(byte[] data) { _writer.Write(data); return this; }

    /// <summary>
    /// Build the final packet with WLO header format
    /// </summary>
    public byte[] Build()
    {
        var payload = _stream.ToArray();
        var packet = new byte[PacketConstants.HeaderSize + payload.Length];

        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(0, 2), PacketConstants.Header);
        BinaryPrimitives.WriteUInt16LittleEndian(packet.AsSpan(2, 2), (ushort)payload.Length);
        payload.CopyTo(packet.AsSpan(PacketConstants.HeaderSize));

        return packet;
    }
}
