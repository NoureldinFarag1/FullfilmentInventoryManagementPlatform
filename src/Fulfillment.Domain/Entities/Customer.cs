using Fulfillment.Domain.Common;

namespace Fulfillment.Domain.Entities;

public class Customer : BaseEntity
{
    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
}