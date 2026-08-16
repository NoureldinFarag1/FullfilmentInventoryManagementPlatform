using Fulfillment.Domain.Common;

namespace Fulfillment.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Product> Products { get; set; } = new  HashSet<Product>();
}