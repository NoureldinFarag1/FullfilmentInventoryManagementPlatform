using System.Reflection;
using Fulfillment.Domain.Entities;
using Fulfillment.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Infrastructure.Persistence;

public class FulfillmentDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public FulfillmentDbContext(DbContextOptions<FulfillmentDbContext> options)
        : base(options) { }
    
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}