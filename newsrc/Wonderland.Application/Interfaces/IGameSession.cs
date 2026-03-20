namespace Wonderland.Application.Interfaces;

/// <summary>
/// Abstract game client session — decouples Application from Infrastructure's TCP implementation.
/// Infrastructure's GameClientSession implements this.
/// </summary>
public interface IGameSession
{
    string SessionId { get; }
    string RemoteAddress { get; }
    object? Tag { get; set; }
    Task SendAsync(byte[] data);
}

/// <summary>
/// Abstract incoming packet — decouples Application from Infrastructure's packet format.
/// </summary>
public interface IReceivePacket
{
    byte ActionCode { get; }
    byte SubAction { get; }
    int Length { get; }
    int Remaining { get; }

    byte ReadByte();
    ushort ReadUInt16();
    uint ReadUInt32();
    string ReadString(int length);
    byte[] ReadBytes(int count);
    void Skip(int count);
}
