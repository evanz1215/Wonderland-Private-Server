using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wonderland.Domain.Interfaces;
using Wonderland.Infrastructure.Database;
using Wonderland.Infrastructure.Database.Repositories;
using Wonderland.Infrastructure.Network;

namespace Wonderland.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL via EF Core
        services.AddDbContext<WonderlandDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsAssembly(typeof(WonderlandDbContext).Assembly.FullName)));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICharacterRepository, CharacterRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();

        // TCP Game Server (singleton — one instance for the application lifetime)
        services.AddSingleton<GameTcpServer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GameTcpServer>>();
            var port = int.TryParse(configuration["GameServer:TcpPort"], out var p) ? p : 6414;
            return new GameTcpServer(port, logger);
        });

        return services;
    }
}
