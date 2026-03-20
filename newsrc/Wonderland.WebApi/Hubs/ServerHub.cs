using Microsoft.AspNetCore.SignalR;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;

namespace Wonderland.WebApi.Hubs;

/// <summary>
/// SignalR hub for real-time server management.
/// Web panel connects here for live updates (player events, logs, stats).
/// </summary>
public class ServerHub : Hub
{
    private readonly IGameServerManager _serverManager;
    private readonly IPlayerRuntimeManager _playerManager;

    public ServerHub(IGameServerManager serverManager, IPlayerRuntimeManager playerManager)
    {
        _serverManager = serverManager;
        _playerManager = playerManager;
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("ServerStatus", _serverManager.GetStatus());
        await base.OnConnectedAsync();
    }

    public ServerStatus GetServerStatus() => _serverManager.GetStatus();

    public IReadOnlyCollection<PlayerRuntimeDto> GetOnlinePlayers()
        => _playerManager.GetAll().Select(p => new PlayerRuntimeDto(
            p.CharId, p.CharName, p.Level, p.MapId, p.X, p.Y, p.IsInBattle
        )).ToList().AsReadOnly();

    public async Task ExecuteGmCommand(string command)
    {
        // TODO: Execute via IGmCommandService with admin-level permissions
        await Clients.Caller.SendAsync("GmCommandResult", new { Command = command, Success = true, Message = "Command executed" });
    }

    public async Task SendAnnouncement(string message)
    {
        await Clients.All.SendAsync("Announcement", message);
    }
}

public record PlayerRuntimeDto(int CharId, string Name, byte Level, ushort MapId, ushort X, ushort Y, bool IsInBattle);
public record LogEntryDto(DateTime Timestamp, string Level, string Message, string? Source);
