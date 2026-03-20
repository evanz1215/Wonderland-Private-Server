using Microsoft.Extensions.DependencyInjection;
using Wonderland.Application.Interfaces;
using Wonderland.GameServer.Systems;

namespace Wonderland.GameServer;

public static class DependencyInjection
{
    public static IServiceCollection AddGameServer(this IServiceCollection services)
    {
        // Player runtime manager (singleton — shared state)
        services.AddSingleton<IPlayerRuntimeManager, PlayerRuntimeManager>();

        // Broadcast service
        services.AddSingleton<IBroadcastService, BroadcastService>();

        // Map manager
        services.AddSingleton<IMapManager, MapManager>();

        // Auth service
        services.AddScoped<IAuthService, AuthService>();

        // Character creation service
        services.AddScoped<ICharacterCreationService, CharacterCreationService>();

        // Inventory service
        services.AddScoped<IInventoryService, InventoryService>();

        // GM command service
        services.AddSingleton<IGmCommandService, GmCommandService>();

        // World event system
        services.AddSingleton<IWorldEventService, WorldEventService>();

        // Game server as hosted background service
        services.AddSingleton<GameServerHostedService>();
        services.AddSingleton<IGameServerManager>(sp => sp.GetRequiredService<GameServerHostedService>());
        services.AddHostedService(sp => sp.GetRequiredService<GameServerHostedService>());

        return services;
    }
}
