using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fulfillment.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p=>p.Id);
        
        builder.Property(p=>p.Sku).HasMaxLength(100).IsRequired();
        builder.Property(p=>p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p=>p.Description).HasMaxLength(1000);
        builder.Property(p=>p.Price).HasPrecision(18,2).IsRequired();
        builder.Property(p => p.LowStockThreshold);
        
        builder.HasIndex(p=>p.Sku).IsUnique();
        builder.HasIndex(p=>p.Name);
        
        builder.HasOne(p=>p.Category)
            .WithMany(c=>c.Products)
            .HasForeignKey(p=>p.CategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}