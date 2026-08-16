using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        
        builder.HasKey(w=>w.Id);
        
        builder.Property(w=>w.Code).HasMaxLength(20).IsRequired();
        builder.Property(w=>w.Name).HasMaxLength(200).IsRequired();
        builder.Property(w=>w.Address).HasMaxLength(500);
        
        builder.HasIndex(w=>w.Code).IsUnique();
    }
}