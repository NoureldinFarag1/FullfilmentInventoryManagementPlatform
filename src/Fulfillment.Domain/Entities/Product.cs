using Fulfillment.Domain.Common;

namespace Fulfillment.Domain.Entities;

public class Product : BaseEntity
{
    public string Sku { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public ICollection<InventoryItem> InventoryItems { get; set; } = new HashSet<InventoryItem>();
    public decimal Price { get; set; }
    public int? LowStockThreshold { get; set; }
}