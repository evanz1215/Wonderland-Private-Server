using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Database.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.ToTable("characters");
        builder.HasKey(c => c.CharId);
        builder.Property(c => c.CharId).HasColumnName("char_id");
        builder.Property(c => c.UserId).HasColumnName("user_id");
        builder.Property(c => c.Slot).HasColumnName("slot");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(20).IsRequired();

        // Appearance
        builder.Property(c => c.Body).HasColumnName("body");
        builder.Property(c => c.Head).HasColumnName("head");
        builder.Property(c => c.Hair).HasColumnName("hair");
        builder.Property(c => c.Eyes).HasColumnName("eyes");
        builder.Property(c => c.Skin).HasColumnName("skin");
        builder.Property(c => c.Clothing).HasColumnName("clothing");

        // Location
        builder.Property(c => c.MapId).HasColumnName("map_id");
        builder.Property(c => c.X).HasColumnName("x");
        builder.Property(c => c.Y).HasColumnName("y");

        // Stats
        builder.Property(c => c.Level).HasColumnName("level");
        builder.Property(c => c.TotalExp).HasColumnName("total_exp");
        builder.Property(c => c.Gold).HasColumnName("gold");
        builder.Property(c => c.MaxHp).HasColumnName("max_hp");
        builder.Property(c => c.MaxSp).HasColumnName("max_sp");
        builder.Property(c => c.CurrentHp).HasColumnName("current_hp");
        builder.Property(c => c.CurrentSp).HasColumnName("current_sp");
        builder.Property(c => c.Str).HasColumnName("str");
        builder.Property(c => c.Con).HasColumnName("con");
        builder.Property(c => c.Agi).HasColumnName("agi");
        builder.Property(c => c.Int).HasColumnName("int");
        builder.Property(c => c.Wis).HasColumnName("wis");
        builder.Property(c => c.PotentialPoints).HasColumnName("potential_points");
        builder.Property(c => c.SkillPoints).HasColumnName("skill_points");

        // Reborn
        builder.Property(c => c.RebornJob).HasColumnName("reborn_job");
        builder.Property(c => c.IsReborn).HasColumnName("is_reborn");
        builder.Property(c => c.Affinity).HasColumnName("affinity");

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasMany(c => c.Inventory).WithOne(i => i.Character).HasForeignKey(i => i.CharId);
        builder.HasMany(c => c.Pets).WithOne(p => p.Character).HasForeignKey(p => p.CharId);
    }
}
