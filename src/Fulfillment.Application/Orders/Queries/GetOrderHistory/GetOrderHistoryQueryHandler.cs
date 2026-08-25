using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using Fulfillment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Orders.Queries.GetOrderHistory;

public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, OrderHistoryDto>
{
    private readonly IApplicationDbContext _context;
    
    public GetOrderHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<OrderHistoryDto> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o=>o.Id == request.OrderId)
            .Select(o=> new
            {
                o.Id,
                o.ReferenceNumber,
                o.OrderedAt,
                o.ConfirmedAt,
                o.CompletedAt,
                o.CancelledAt,
                o.CreatedByUserId,
                o.TotalAmount,
                ItemCount = o.Items.Count
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(Order),request.OrderId);
        
        var timeline = new List<OrderHistoryEntryDto>
        {
            new(order.OrderedAt, "Created",
                $"Order raised with {order.ItemCount} lines, total {order.TotalAmount:N2}.",
                order.CreatedByUserId)
        };
        
        var reference = $"Order {order.Id}";

        var movements = await _context.StockMovements
            .AsNoTracking()
            .Where(m => m.Reason != null && m.Reason.Contains(order.Id.ToString()))
            .OrderBy(m => m.OccuredAt)
            .Select(m => new
            {
                m.OccuredAt,
                m.Type,
                m.Delta,
                m.PerformedByUserId,
                m.InventoryItem.Product.Sku
            })
            .ToListAsync(cancellationToken);

        foreach (var movement in movements)
        {
            var label = movement.Type == MovementType.OrderAllocation
                ? "Stock allocated"
                : "Stock returned";

            timeline.Add(new OrderHistoryEntryDto(
                movement.OccuredAt,
                label,
                $"{movement.Sku}: {movement.Delta:+#;-#;0} units.",
                movement.PerformedByUserId));
        }

        if (order.ConfirmedAt is not null)
            timeline.Add(new(order.ConfirmedAt.Value, "Confirmed", "Order accepted for processing.", null));

        if (order.CompletedAt is not null)
            timeline.Add(new(order.CompletedAt.Value, "Completed", "Order fulfilled.", null));

        if (order.CancelledAt is not null)
            timeline.Add(new(order.CancelledAt.Value, "Cancelled", "Order cancelled.", null));

        return new OrderHistoryDto(
            order.Id,
            order.ReferenceNumber,
            timeline.OrderBy(e => e.OccurredAt).ToList());
    }
}