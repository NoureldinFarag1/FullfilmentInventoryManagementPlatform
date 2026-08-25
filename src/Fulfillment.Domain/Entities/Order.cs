using Fulfillment.Domain.Common;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;

namespace Fulfillment.Domain.Entities;

public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();

    private Order() { }

    public Order(Guid customerId, Guid warehouseId, string referenceNumber, string? notes = null, string? idempotencyKey = null)
    {
        CustomerId = customerId;
        WarehouseId = warehouseId;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        IdempotencyKey = idempotencyKey;
        Status = OrderStatus.Draft;
        OrderedAt = DateTime.UtcNow;
        TotalAmount = 0m;
    }
    
    public string ReferenceNumber { get; private set; } = null!;
    
    public string? Notes { get; set; }
    public Guid CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;

    public Guid WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;

    public OrderStatus Status { get; private set; }
    public DateTime OrderedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public decimal TotalAmount { get; private set; }
    public string? IdempotencyKey { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public OrderItem AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOrderStateException(
                $"Items can only be added while an order is in Draft. This order is {Status}.");

        if (quantity <= 0)
            throw new BusinessRuleViolationException("Order item quantity must be greater than zero.");

        if (!product.IsActive)
            throw new BusinessRuleViolationException(
                $"Product '{product.Sku}' is deactivated and cannot be ordered.");

        var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);

        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            Recalculate();
            return existing;
        }

        var item = new OrderItem(Id, product, quantity);
        _items.Add(item);
        Recalculate();

        return item;
    }

    public void Confirm()
    {
        EnsureTransitionAllowed(OrderStatus.Confirmed);

        if (_items.Count == 0)
            throw new BusinessRuleViolationException("An order cannot be confirmed with no items.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        EnsureTransitionAllowed(OrderStatus.Completed);

        Status = OrderStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <returns>true when stock must be restored, i.e. the order had already
    /// been confirmed and so had taken stock.</returns>
    public bool Cancel()
    {
        EnsureTransitionAllowed(OrderStatus.Cancelled);

        var stockWasTaken = Status == OrderStatus.Confirmed;

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;

        return stockWasTaken;
    }

    private void Recalculate() => TotalAmount = _items.Sum(i => i.LineTotal);

    private void EnsureTransitionAllowed(OrderStatus target)
    {
        var allowed = (Status, target) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Completed) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOrderStateException(
                $"An order cannot move from {Status} to {target}.");
    }
}