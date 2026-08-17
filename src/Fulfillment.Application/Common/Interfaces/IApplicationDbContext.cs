using Fulfillment.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<StockMovement> StockMovements { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}