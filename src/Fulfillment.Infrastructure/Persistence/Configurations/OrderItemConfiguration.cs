using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", t =>
            t.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0"));

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductSku).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ProductName).IsRequired().HasMaxLength(100);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.Ignore(i => i.LineTotal);

        builder.HasIndex(i => new { i.OrderId, i.ProductId }).IsUnique();

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}