using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Inventory.Queries.GetWarehouseInventory;

public class GetWarehouseInventoryHandler : IRequestHandler<GetWarehouseInventoryQuery, PaginatedList<WarehouseInventoryDto>>
{
    private readonly IApplicationDbContext _context;
    
    public GetWarehouseInventoryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<PaginatedList<WarehouseInventoryDto>> Handle(GetWarehouseInventoryQuery request, CancellationToken cancellationToken)
    {
        var exists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == request.WarehouseId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(i => i.Product.Name.Contains(term) || i.Product.Sku.Contains(term));
        }

        var projected = query
            .OrderBy(i => i.Product.Name)
            .Select(i => new WarehouseInventoryDto(
                i.Id,
                i.ProductId,
                i.Product.Sku,
                i.Product.Name,
                i.Quantity));
        
        return await PaginatedList<WarehouseInventoryDto>.CreateAsync(
            projected, request.pageNumber, request.pageSize, cancellationToken);
    }
}