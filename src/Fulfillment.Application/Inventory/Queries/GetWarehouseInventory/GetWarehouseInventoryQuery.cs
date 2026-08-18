using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Inventory.Queries.GetWarehouseInventory;

public record GetWarehouseInventoryQuery(
    Guid WarehouseId,
    string? Search = null,
    int pageNumber =1,
    int pageSize = 20) : IRequest<PaginatedList<WarehouseInventoryDto>>;
    
public record WarehouseInventoryDto(
    Guid InventoryItemId,
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity);