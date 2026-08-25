using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using Fulfillment.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.Orders.Commands.ConfirmOrder;

public class ConfirmOrderCommandHandler : IRequestHandler<ConfirmOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ConfirmOrderCommandHandler> _logger;

    public ConfirmOrderCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, ILogger<ConfirmOrderCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }
    public async Task Handle(ConfirmOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedAccessException("No authenticated user.");

        var order = await _context.Orders
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Transition first: an invalid state change must not touch stock.
        order.Confirm();

        var productIds = order.Items.Select(i => i.ProductId).ToList();

        var inventoryItems = await _context.InventoryItems
            .Where(i => i.WarehouseId == order.WarehouseId && productIds.Contains(i.ProductId))
            .ToDictionaryAsync(i => i.ProductId, cancellationToken);

        foreach (var line in order.Items)
        {
            if (!inventoryItems.TryGetValue(line.ProductId, out var inventoryItem))
                throw new BusinessRuleViolationException(
                    $"Product '{line.ProductSku}' is not stocked in this warehouse.");

            if (inventoryItem.Quantity < line.Quantity)
                throw new BusinessRuleViolationException(
                    $"Insufficient stock for '{line.ProductSku}': {inventoryItem.Quantity} available, {line.Quantity} required.");

            // Throws if the resulting quantity would be negative.
            var movement = inventoryItem.ApplyAdjustment(
                -line.Quantity,
                MovementType.OrderAllocation,
                $"Order {order.Id}",
                userId);

            _context.StockMovements.Add(movement);
        }

        // One save: the status change, every quantity change and every movement
        // row share a single transaction. Any failure rolls all of it back.
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Order {OrderId} confirmed by {UserId}: {LineCount} lines allocated from warehouse {WarehouseId}, total {TotalAmount}.",
            order.Id, userId, order.Items.Count, order.WarehouseId, order.TotalAmount);
    }
}