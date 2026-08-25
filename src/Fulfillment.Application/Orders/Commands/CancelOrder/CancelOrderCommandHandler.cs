using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.Orders.Commands.CancelOrder;

public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, ILogger<CancelOrderCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedAccessException("No authenticated user.");

        var order = await _context.Orders
                        .Include(o => o.Items)
                        .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Returns true only when the order was Confirmed, i.e. stock had been taken.
        // Cancelled is terminal, so this can never fire twice for one order.
        var stockMustBeRestored = order.Cancel();

        if (stockMustBeRestored)
        {
            var productIds = order.Items.Select(i => i.ProductId).ToList();

            var inventoryItems = await _context.InventoryItems
                .Where(i => i.WarehouseId == order.WarehouseId && productIds.Contains(i.ProductId))
                .ToDictionaryAsync(i => i.ProductId, cancellationToken);

            foreach (var line in order.Items)
            {
                if (!inventoryItems.TryGetValue(line.ProductId, out var inventoryItem))
                    continue;

                var movement = inventoryItem.ApplyAdjustment(
                    line.Quantity,
                    MovementType.OrderCancellation,
                    request.Reason ?? $"Cancellation of order {order.Id}",
                    userId);

                _context.StockMovements.Add(movement);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Order {OrderId} cancelled by {UserId}. Stock restored: {StockRestored}.",
            order.Id, userId, stockMustBeRestored);
    }
}