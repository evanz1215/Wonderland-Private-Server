using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wonderland.Domain.Entities;

namespace Wonderland.Infrastructure.Database.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.CharId).HasColumnName("char_id");
        builder.Property(i => i.Slot).HasColumnName("slot");
        builder.Property(i => i.ItemId).HasColumnName("item_id");
        builder.Property(i => i.Quantity).HasColumnName("quantity");
        builder.Property(i => i.Forge).HasColumnName("forge");
        builder.Property(i => i.SocketId).HasColumnName("socket_id");
        builder.Property(i => i.BombId).HasColumnName("bomb_id");
        builder.Property(i => i.SewId).HasColumnName("sew_id");
        builder.Property(i => i.IsEquipped).HasColumnName("is_equipped");
        builder.Property(i => i.EquipSlot).HasColumnName("equip_slot");

        builder.HasIndex(i => new { i.CharId, i.Slot }).IsUnique();
    }
}
