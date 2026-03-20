using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
        builder.Property(u => u.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
        builder.Property(u => u.CipherPassword).HasColumnName("cipher_password").HasMaxLength(50);
        builder.Property(u => u.GmLevel).HasColumnName("gm_level");
        builder.Property(u => u.ImPoints).HasColumnName("im_points");
        builder.Property(u => u.IsBanned).HasColumnName("is_banned");
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");

        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasMany(u => u.Characters).WithOne(c => c.User).HasForeignKey(c => c.UserId);
    }
}
