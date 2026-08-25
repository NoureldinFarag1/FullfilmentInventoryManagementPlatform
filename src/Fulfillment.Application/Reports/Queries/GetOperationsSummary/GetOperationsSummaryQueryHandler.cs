using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Reports.Queries.GetOperationsSummary;

public class GetOperationsSummaryQueryHandler : IRequestHandler<GetOperationsSummaryQuery, OperationsSummaryDto>
{
    private readonly IApplicationDbContext _context;
    
    public GetOperationsSummaryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<OperationsSummaryDto> Handle(GetOperationsSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = _context.Orders.AsNoTracking();
        
        if(request.From is not null)
            orders = orders.Where(o=>o.OrderedAt >= request.From);
        
        if(request.To is not null)
            orders = orders.Where(o=>o.OrderedAt <= request.To);
        
        var byStatus = await orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Value = g.Sum(o => o.TotalAmount)
            })
            .ToListAsync(cancellationToken);
        
        var revenue = byStatus
            .Where(s => s.Status is OrderStatus.Confirmed or OrderStatus.Completed)
            .Sum(s => s.Value);

        var topProducts = await orders
            .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Completed)
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductSku, i.ProductName })
            .Select(g => new TopProductDto(
                g.Key.ProductSku,
                g.Key.ProductName,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.UnitPrice * i.Quantity)))
            .OrderByDescending(p => p.UnitsOrdered)
            .Take(5)
            .ToListAsync(cancellationToken);

        var stockByWarehouse = await _context.Warehouses
            .AsNoTracking()
            .Select(w => new WarehouseStockDto(
                w.Code,
                w.Name,
                w.InventoryItems.Sum(i => (int?)i.Quantity) ?? 0,
                w.InventoryItems.Count))
            .OrderBy(w => w.WarehouseCode)
            .ToListAsync(cancellationToken);

        var lowStockCount = await _context.Products
            .AsNoTracking()
            .CountAsync(p =>
                    p.LowStockThreshold != null &&
                    p.InventoryItems.Sum(i => (int?)i.Quantity) <= p.LowStockThreshold,
                cancellationToken);

        return new OperationsSummaryDto(
            request.From,
            request.To,
            byStatus.Sum(s => s.Count),
            revenue,
            byStatus
                .Select(s => new OrdersByStatusDto(s.Status.ToString(), s.Count, s.Value))
                .OrderBy(s => s.Status)
                .ToList(),
            topProducts,
            stockByWarehouse,
            lowStockCount);
    }
}