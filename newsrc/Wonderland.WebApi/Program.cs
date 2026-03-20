using Microsoft.EntityFrameworkCore;
using Serilog;
using Wonderland.Application;
using Wonderland.GameServer;
using Wonderland.Infrastructure;
using Wonderland.Infrastructure.Database;
using Wonderland.WebApi.Hubs;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/server-.log", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog();

    // Layer registration
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddGameServer();

    // ASP.NET Core services
    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Wonderland Server API", Version = "v1" });
    });

    // CORS — allow web management panel from any origin in development
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowWebPanel", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        options.AddPolicy("AllowSignalR", policy =>
            policy.SetIsOriginAllowed(_ => true).AllowAnyMethod().AllowAnyHeader().AllowCredentials());
    });

    var app = builder.Build();

    // Auto-migrate database in development
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WonderlandDbContext>();
        await db.Database.MigrateAsync();
    }

    // Middleware pipeline
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowWebPanel");

    app.MapControllers();
    app.MapHub<ServerHub>("/hubs/server", options =>
    {
        options.AllowStatefulReconnects = true;
    }).RequireCors("AllowSignalR");

    Log.Information("Wonderland Server starting...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
