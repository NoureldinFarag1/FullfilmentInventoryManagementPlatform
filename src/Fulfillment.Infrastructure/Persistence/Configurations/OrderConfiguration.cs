using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", t=>
            t.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative","[TotalAmount] >= 0"));
        
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Status).HasConversion<int>().IsRequired();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(o => o.OrderedAt).IsRequired();
        builder.Property(o => o.IdempotencyKey).HasMaxLength(100);
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.OrderedAt);
        builder.HasIndex(o => o.CustomerId);
        builder.HasIndex(o => o.WarehouseId);

        // Filtered unique index: only non-null keys must be unique.
        builder.HasIndex(o => o.IdempotencyKey)
            .IsUnique()
            .HasFilter("[IdempotencyKey] IS NOT NULL");

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata
            .FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}