using Fulfillment.Domain.Enums;

namespace Fulfillment.Domain.Entities;

public class StockMovement
{
    private StockMovement() { }

    internal StockMovement(Guid inventoryItemId, int delta,
        int quantityAfter, MovementType type, string? reason,
        Guid performedByUserId, Guid? orderId = null)
    {
        Id =  Guid.NewGuid();
        InventoryItemId = inventoryItemId;
        Delta = delta;
        QuantityAfter = quantityAfter;
        Type = type;
        Reason = reason;
        PerformedByUserId = performedByUserId;
        OccuredAt = DateTime.UtcNow;
        OrderId = orderId;
    }
    
    public Guid Id { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public int Delta { get; private set; }
    public int QuantityAfter { get; private set; }
    public MovementType Type { get; private set; }
    public string? Reason { get; private set; }
    public Guid PerformedByUserId { get; private set; }
    public DateTime OccuredAt { get; private set; }

    /// <summary>
    /// Set when the movement was caused by an order, so order history can be
    /// read by key instead of matching text in <see cref="Reason"/>.
    /// </summary>
    public Guid? OrderId { get; private set; }

    public InventoryItem InventoryItem { get; private set; } = null!;
}