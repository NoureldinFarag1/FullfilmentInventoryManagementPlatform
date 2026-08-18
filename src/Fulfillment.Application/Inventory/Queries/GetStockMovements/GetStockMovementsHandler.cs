using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Inventory.Queries.GetStockMovements;

public class GetStockMovementsHandler : IRequestHandler<GetStockMovementsQuery, PaginatedList<StockMovementDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStockMovementsHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<PaginatedList<StockMovementDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.StockMovements.AsNoTracking();
        
        if(request.InventoryItemId is not null)
            query = query.Where(m=>m.InventoryItemId == request.InventoryItemId);

        if (request.ProductId is not null)
            query = query.Where(m => m.InventoryItem.ProductId == request.ProductId);
        
        if (request.WarehouseId is not null)
            query = query.Where(m => m.InventoryItem.WarehouseId == request.WarehouseId);

        if (request.PerformedByUserId is not null)
            query = query.Where(m => m.PerformedByUserId == request.PerformedByUserId);

        if (request.Type is not null)
            query = query.Where(m => m.Type == request.Type);

        if (request.From is not null)
            query = query.Where(m => m.OccuredAt >= request.From);

        if (request.To is not null)
            query = query.Where(m => m.OccuredAt <= request.To);

        var projected = query
            .OrderByDescending(m => m.OccuredAt)
            .Select(m => new StockMovementDto(
                m.Id,
                m.InventoryItemId,
                m.InventoryItem.ProductId,
                m.InventoryItem.Product.Sku,
                m.InventoryItem.Product.Name,
                m.InventoryItem.WarehouseId,
                m.InventoryItem.Warehouse.Code,
                m.Delta,
                m.QuantityAfter,
                m.Type,
                m.Reason,
                m.PerformedByUserId,
                m.OccuredAt
            ));
        
        return await PaginatedList<StockMovementDto>.CreateAsync(
            projected, request.PageNumber, request.PageSize, cancellationToken);
    }
}