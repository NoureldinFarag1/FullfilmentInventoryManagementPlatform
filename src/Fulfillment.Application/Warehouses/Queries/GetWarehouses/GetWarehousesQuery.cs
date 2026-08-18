using Fulfillment.Application.Common.Models;
using MediatR;

namespace Fulfillment.Application.Warehouses.Queries.GetWarehouses;

public record GetWarehousesQuery(
    string? Search = null,
    bool? IsActive = null,
    int pageNumber = 1,
    int pageSize = 20) : IRequest<PaginatedList<WarehouseDto>>;
    
public record WarehouseDto (
    Guid Id,
    string Code,
    string Name,
    string? Address,
    bool IsActive);