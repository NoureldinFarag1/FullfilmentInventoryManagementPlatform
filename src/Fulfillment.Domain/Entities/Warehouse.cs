using Fulfillment.Domain.Common;

namespace Fulfillment.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<InventoryItem> InventoryItems { get; set; } = new HashSet<InventoryItem>();
}