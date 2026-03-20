using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Database.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("pets");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.CharId).HasColumnName("char_id");
        builder.Property(p => p.Slot).HasColumnName("slot");
        builder.Property(p => p.NpcId).HasColumnName("npc_id");
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(20);
        builder.Property(p => p.Level).HasColumnName("level");
        builder.Property(p => p.TotalExp).HasColumnName("total_exp");
        builder.Property(p => p.Element).HasColumnName("element");
        builder.Property(p => p.MaxHp).HasColumnName("max_hp");
        builder.Property(p => p.MaxSp).HasColumnName("max_sp");
        builder.Property(p => p.CurrentHp).HasColumnName("current_hp");
        builder.Property(p => p.CurrentSp).HasColumnName("current_sp");
        builder.Property(p => p.Str).HasColumnName("str");
        builder.Property(p => p.Con).HasColumnName("con");
        builder.Property(p => p.Agi).HasColumnName("agi");
        builder.Property(p => p.Int).HasColumnName("int");
        builder.Property(p => p.Wis).HasColumnName("wis");
        builder.Property(p => p.Intimacy).HasColumnName("intimacy");
        builder.Property(p => p.IsSummoned).HasColumnName("is_summoned");
    }
}
