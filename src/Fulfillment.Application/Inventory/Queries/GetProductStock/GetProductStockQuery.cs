using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Inventory.Queries.GetProductStock;

public record GetProductStockQuery(Guid ProductId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<ProductStockDto>>;

public record ProductStockDto(
    Guid InventoryItemId,
    Guid WarehouseId,
    string WarehouseCode,
    string WarehouseName,
    int Quantity);