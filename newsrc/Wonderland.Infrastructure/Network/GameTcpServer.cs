using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Wonderland.Infrastructure.Network;

/// <summary>
/// Async TCP server that replaces RCLibrary.Core.Networking.TcpServer.
/// Listens on port 6414 for WLO game client connections.
/// </summary>
public class GameTcpServer
{
    private readonly ILogger<GameTcpServer> _logger;
    private readonly ConcurrentDictionary<string, GameClientSession> _sessions = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public int Port { get; }
    public int ConnectedClients => _sessions.Count;

    public event Func<GameClientSession, ReceivePacket, Task>? OnPacketReceived;
    public event Func<GameClientSession, Task>? OnClientConnected;
    public event Func<GameClientSession, Task>? OnClientDisconnected;

    public GameTcpServer(int port, ILogger<GameTcpServer> logger)
    {
        Port = port;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _listener = new TcpListener(IPAddress.Any, Port);
        _listener.Start(40);

        _logger.LogInformation("Game TCP server started on port {Port}", Port);

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(_cts.Token);
                var session = new GameClientSession(tcpClient, _logger);
                _sessions.TryAdd(session.SessionId, session);

                _logger.LogInformation("Client connected: {SessionId} from {RemoteEndPoint}",
                    session.SessionId, tcpClient.Client.RemoteEndPoint);

                if (OnClientConnected is not null)
                    await OnClientConnected(session);

                _ = HandleClientAsync(session, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Game TCP server stopping...");
        }
    }

    private async Task HandleClientAsync(GameClientSession session, CancellationToken ct)
    {
        try
        {
            await foreach (var packet in session.ReadPacketsAsync(ct))
            {
                if (OnPacketReceived is not null)
                    await OnPacketReceived(session, packet);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Client {SessionId} error", session.SessionId);
        }
        finally
        {
            _sessions.TryRemove(session.SessionId, out _);
            session.Dispose();

            if (OnClientDisconnected is not null)
                await OnClientDisconnected(session);

            _logger.LogInformation("Client disconnected: {SessionId}", session.SessionId);
        }
    }

    public async Task SendToAsync(string sessionId, SendPacket packet)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
            await session.SendPacketAsync(packet);
    }

    public async Task BroadcastAsync(byte[] data, Func<GameClientSession, bool>? filter = null)
    {
        foreach (var session in _sessions.Values)
        {
            if (filter is null || filter(session))
                await session.SendRawAsync(data);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();

        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();

        _logger.LogInformation("Game TCP server stopped");
    }
}
