using Fulfillment.Domain.Common;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

namespace Fulfillment.Domain.Entities;

public class InventoryItem : BaseEntity
{
    private readonly List<StockMovement> _movements = new();

    private InventoryItem() { }

    public InventoryItem(Guid productId, Guid warehouseId)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        Quantity = 0;
    }

    public Guid ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public int Quantity { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<StockMovement> Movements => _movements.AsReadOnly();

    public StockMovement ApplyAdjustment(int delta, MovementType type, string? reason, string performedByUserId)
    {
        if (delta == 0)
            throw new BusinessRuleViolationException("A stock adjustment must change the quantity.");

        if (string.IsNullOrWhiteSpace(performedByUserId))
            throw new BusinessRuleViolationException("A stock adjustment must record the user who performed it.");

        reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

        if (type == MovementType.Other && reason is null)
            throw new BusinessRuleViolationException("A reason is required when the movement type is Other.");

        if (!IsDirectionAllowed(type, delta))
            throw new BusinessRuleViolationException(
                $"A {type} movement must {(delta > 0 ? "decrease" : "increase")} stock, but the requested change was {delta}.");

        var newQuantity = Quantity + delta;

        if (newQuantity < 0)
            throw new BusinessRuleViolationException(
                $"Stock cannot go below zero. Current quantity is {Quantity} and the requested change is {delta}.");

        Quantity = newQuantity;

        var movement = new StockMovement(Id, delta, newQuantity, type, reason, performedByUserId);
        _movements.Add(movement);

        return movement;
    }

    private static bool IsDirectionAllowed(MovementType type, int delta) => type switch
    {
        MovementType.Receipt => delta > 0,
        MovementType.Issue => delta < 0,
        MovementType.Damage => delta < 0,
        MovementType.Loss => delta < 0,
        MovementType.CountCorrection => true,
        MovementType.Other => true,
        _ => false
    };
}