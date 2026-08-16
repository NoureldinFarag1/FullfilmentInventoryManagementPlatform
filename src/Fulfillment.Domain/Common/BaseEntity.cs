namespace Fulfillment.Domain.Common;

public abstract class BaseEntity
{
    protected BaseEntity () => Id = Guid.NewGuid();
    
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }
}