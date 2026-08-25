using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements", t =>
        {
            t.HasCheckConstraint("CK_StockMovements_Delta_NonZero", "[Delta] <> 0");
            t.HasCheckConstraint("CK_StockMovements_QuantityAfter_NonNegative", "[QuantityAfter] >= 0");
        });
        
        builder.HasKey(m => m.Id);
        
        builder.Property(m=>m.Reason).HasMaxLength(500);
        builder.Property(m=>m.Type).HasConversion<int>().IsRequired();
        builder.Property(m => m.OccuredAt).IsRequired();
        builder.Property(m => m.OrderId);

        builder.HasIndex(m=> new {m.InventoryItemId, m.OccuredAt});
        builder.HasIndex(m => m.OccuredAt);
        builder.HasIndex(m => m.PerformedByUserId);
        builder.HasIndex(m => m.OrderId);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m=>m.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // No navigation back from Order: the movement log is read on its own
        // terms, same reasoning as PerformedByUserId above.
        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(m => m.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}