using Fulfillment.Application.Common.Models;
using Fulfillment.Domain.Enums;
using MediatR;

namespace Fulfillment.Application.Inventory.Queries.GetStockMovements;

public record GetStockMovementsQuery(
    Guid? ProductId = null,
    Guid? WarehouseId = null,
    Guid? InventoryItemId = null,
    Guid? PerformedByUserId = null,
    MovementType? Type = null,
    DateTime? From = null,
    DateTime? To = null,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<StockMovementDto>>;

public record StockMovementDto(
    Guid Id,
    Guid InventoryItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    Guid WarehouseId,
    string WarehouseCode,
    int Delta,
    int QuantityAfter,
    MovementType Type,
    string? Reason,
    Guid PerformedByUserId,
    DateTime OccuredAt);