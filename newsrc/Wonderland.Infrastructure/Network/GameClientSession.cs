using System.Buffers.Binary;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;

namespace Wonderland.Infrastructure.Network;

/// <summary>
/// Represents a single game client TCP connection.
/// Replaces RCLibrary SocketClient.
/// </summary>
public class GameClientSession : IGameSession, IDisposable
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    public string SessionId { get; } = Guid.NewGuid().ToString("N")[..12];
    public string RemoteAddress { get; }

    /// <summary>
    /// Arbitrary tag for associating a Player object with this session
    /// </summary>
    public object? Tag { get; set; }

    public GameClientSession(TcpClient client, ILogger logger)
    {
        _client = client;
        _stream = client.GetStream();
        _logger = logger;
        RemoteAddress = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Reads WLO packets as an async enumerable stream
    /// </summary>
    public async IAsyncEnumerable<ReceivePacket> ReadPacketsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var headerBuffer = new byte[PacketConstants.HeaderSize];

        while (!ct.IsCancellationRequested && _client.Connected)
        {
            // Read 4-byte header: [0x44F4] [length]
            var bytesRead = await ReadExactAsync(headerBuffer, PacketConstants.HeaderSize, ct);
            if (bytesRead < PacketConstants.HeaderSize)
                yield break; // Client disconnected

            var header = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(0, 2));
            if (header != PacketConstants.Header)
            {
                _logger.LogWarning("Invalid packet header 0x{Header:X4} from {Session}", header, SessionId);
                yield break;
            }

            var length = BinaryPrimitives.ReadUInt16LittleEndian(headerBuffer.AsSpan(2, 2));
            if (length == 0 || length > 8192)
            {
                _logger.LogWarning("Invalid packet length {Length} from {Session}", length, SessionId);
                yield break;
            }

            // Read payload
            var payload = new byte[length];
            bytesRead = await ReadExactAsync(payload, length, ct);
            if (bytesRead < length)
                yield break;

            yield return new ReceivePacket(payload);
        }
    }

    // IGameSession.SendAsync — sends raw bytes
    public async Task SendAsync(byte[] data) => await SendRawAsync(data);

    public async Task SendPacketAsync(SendPacket packet)
    {
        await SendRawAsync(packet.Build());
    }

    public async Task SendRawAsync(byte[] data)
    {
        if (_disposed || !_client.Connected) return;

        await _sendLock.WaitAsync();
        try
        {
            await _stream.WriteAsync(data);
            await _stream.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Send failed for {Session}", SessionId);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task<int> ReadExactAsync(byte[] buffer, int count, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            var read = await _stream.ReadAsync(buffer.AsMemory(totalRead, count - totalRead), ct);
            if (read == 0) return totalRead; // Connection closed
            totalRead += read;
        }
        return totalRead;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sendLock.Dispose();
        _stream.Dispose();
        _client.Dispose();
    }
}
