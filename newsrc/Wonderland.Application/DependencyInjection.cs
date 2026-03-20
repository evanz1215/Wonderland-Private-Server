using Microsoft.Extensions.DependencyInjection;
using Wonderland.Application.Interfaces;
using Wonderland.Application.ActionCodes;

namespace Wonderland.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Core services
        services.AddSingleton<ActionCodes.ActionCodeRouter>();

        // Auto-register all IActionCodeHandler implementations from this assembly
        var handlerType = typeof(IActionCodeHandler);
        var handlers = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && handlerType.IsAssignableFrom(t));

        foreach (var handler in handlers)
            services.AddSingleton(handlerType, handler);

        // Auto-register all IGmCommand implementations from this assembly
        var gmCommandType = typeof(IGmCommand);
        var gmCommands = typeof(DependencyInjection).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && gmCommandType.IsAssignableFrom(t));

        foreach (var cmd in gmCommands)
            services.AddSingleton(gmCommandType, cmd);

        return services;
    }
}
