using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Inventory.Queries.GetProductStock;

public class GetProductStockQueryHandler
    : IRequestHandler<GetProductStockQuery, PaginatedList<ProductStockDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProductStockQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ProductStockDto>> Handle(
        GetProductStockQuery request, CancellationToken cancellationToken)
    {
        var exists = await _context.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!exists)
            throw new NotFoundException(nameof(Product), request.ProductId);

        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(i => i.ProductId == request.ProductId)
            .OrderBy(i => i.Warehouse.Code)
            .Select(i => new ProductStockDto(
                i.Id,
                i.WarehouseId,
                i.Warehouse.Code,
                i.Warehouse.Name,
                i.Quantity));

        return await PaginatedList<ProductStockDto>.CreateAsync(
            query, request.PageNumber, request.PageSize, cancellationToken);
    }
}