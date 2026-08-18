using Fulfillment.Domain.Enums;
using MediatR;

namespace Fulfillment.Application.Inventory.Commands.AdjustStock;

public record AdjustStockCommand(
    Guid InventoryItemId,
    int Delta,
    MovementType Type,
    string? Reason) : IRequest<StockAdjustmentResult>;

public record StockAdjustmentResult(
    Guid MovementId,
    Guid InventoryItemId,
    int Delta,
    int QuantityAfter,
    MovementType Type,
    string? Reason,
    Guid PerformedByUserId,
    DateTime OccuredAt);