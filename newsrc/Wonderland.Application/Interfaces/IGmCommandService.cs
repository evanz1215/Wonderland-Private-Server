using Wonderland.Domain.Entities;

namespace Wonderland.Application.Interfaces;

/// <summary>
/// Parses and executes GM commands from chat or web panel.
/// Extensible via IGmCommand registration.
/// </summary>
public interface IGmCommandService
{
    /// <summary>Execute a GM command string (e.g., ":item add 1001 5")</summary>
    Task<GmCommandResult> ExecuteAsync(PlayerRuntime player, string commandText, CancellationToken ct = default);

    /// <summary>Get list of available commands</summary>
    IReadOnlyList<string> GetAvailableCommands();
}

/// <summary>
/// Individual GM command handler — register via DI to extend the system.
/// </summary>
public interface IGmCommand
{
    /// <summary>Command name without prefix (e.g., "item", "warp", "kick")</summary>
    string Name { get; }

    /// <summary>Usage description</summary>
    string Usage { get; }

    /// <summary>Minimum GM level required</summary>
    Domain.Enums.GmStatus RequiredLevel { get; }

    /// <summary>Execute the command</summary>
    Task<GmCommandResult> ExecuteAsync(PlayerRuntime player, string[] args, CancellationToken ct = default);
}

public record GmCommandResult(bool Success, string Message);
