using Microsoft.Extensions.Logging;
using Wonderland.Application.Interfaces;
using Wonderland.Domain.Entities;
using Wonderland.Domain.Enums;

namespace Wonderland.GameServer.Systems;

/// <summary>
/// Routes GM command strings to registered IGmCommand handlers.
/// Commands are auto-discovered via DI.
/// </summary>
public class GmCommandService : IGmCommandService
{
    private readonly Dictionary<string, IGmCommand> _commands;
    private readonly ILogger<GmCommandService> _logger;

    public GmCommandService(IEnumerable<IGmCommand> commands, ILogger<GmCommandService> logger)
    {
        _logger = logger;
        _commands = commands.ToDictionary(c => c.Name.ToLowerInvariant(), c => c);
        _logger.LogInformation("Registered {Count} GM commands: {Names}",
            _commands.Count, string.Join(", ", _commands.Keys));
    }

    public async Task<GmCommandResult> ExecuteAsync(
        PlayerRuntime player, string commandText, CancellationToken ct = default)
    {
        // Parse: ":commandName arg1 arg2 ..."
        var text = commandText.TrimStart(':').Trim();
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
            return new GmCommandResult(false, "Empty command");

        var cmdName = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1..] : [];

        if (!_commands.TryGetValue(cmdName, out var command))
            return new GmCommandResult(false, $"Unknown command: {cmdName}");

        // Permission check
        if (player.GmLevel < command.RequiredLevel)
            return new GmCommandResult(false, "Insufficient permissions");

        _logger.LogInformation("GM command by {Player}: {Command}",
            player.CharName, commandText);

        return await command.ExecuteAsync(player, args, ct);
    }

    public IReadOnlyList<string> GetAvailableCommands()
        => _commands.Values.Select(c => $":{c.Name} — {c.Usage}").ToList().AsReadOnly();
}
