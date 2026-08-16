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
        
        builder.HasIndex(m=> new {m.InventoryItemId, m.OccuredAt});
        builder.HasIndex(m => m.OccuredAt);
        builder.HasIndex(m => m.PerformedByUserId);
        
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(m=>m.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}