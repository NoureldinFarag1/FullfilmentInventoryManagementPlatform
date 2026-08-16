using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", t => 
            t.HasCheckConstraint("CK_InventoryItems_Quantity_NotNegative", "[Quantity] >= 0"));

        builder.HasKey(i => i.Id);
        
        builder.HasIndex(i => new {i.ProductId, i.WarehouseId}).IsUnique();
        builder.HasIndex(i => i.WarehouseId);
        
        builder.Property(i=>i.Quantity).IsRequired();
        builder.Property(i=>i.RowVersion).IsRowVersion();
        
        builder.HasOne(i=>i.Product)
            .WithMany(p=>p.InventoryItems)
            .HasForeignKey(i=>i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasOne(i => i.Warehouse)
            .WithMany(w => w.InventoryItems)
            .HasForeignKey(i => i.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(i => i.Movements)
            .WithOne(m => m.InventoryItem)
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.Metadata
            .FindNavigation(nameof(InventoryItem.Movements))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}