using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.Inventory.Commands.AdjustStock;

public class AdjustStockCommandHandler
    : IRequestHandler<AdjustStockCommand, StockAdjustmentResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<AdjustStockCommandHandler> _logger;

    public AdjustStockCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, ILogger<AdjustStockCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<StockAdjustmentResult> Handle(
        AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
                     ?? throw new UnauthorizedAccessException("No authenticated user.");

        var item = await _context.InventoryItems
                       .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken)
                   ?? throw new NotFoundException(nameof(InventoryItem), request.InventoryItemId);

        // Domain enforces every rule; the entity is the only thing that can change Quantity.
        var movement = item.ApplyAdjustment(request.Delta, request.Type, request.Reason, userId);

        _context.StockMovements.Add(movement);

        // One SaveChanges: quantity change and movement row share a single transaction.
        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation(
            "Stock adjusted on item {InventoryItemId} by {UserId}: {Delta} ({MovementType}), resulting quantity {QuantityAfter}.",
            item.Id, userId, request.Delta, request.Type, movement.QuantityAfter);

        return new StockAdjustmentResult(
            movement.Id,
            movement.InventoryItemId,
            movement.Delta,
            movement.QuantityAfter,
            movement.Type,
            movement.Reason,
            movement.PerformedByUserId,
            movement.OccuredAt);
    }
}