using Microsoft.EntityFrameworkCore;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Database;

public class WonderlandDbContext : DbContext
{
    public WonderlandDbContext(DbContextOptions<WonderlandDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<GuildMember> GuildMembers => Set<GuildMember>();
    public DbSet<Mail> Mails => Set<Mail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WonderlandDbContext).Assembly);
    }
}
