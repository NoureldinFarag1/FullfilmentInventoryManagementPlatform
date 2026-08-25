using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;

    public GetOrderByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OrderDetailDto> Handle(
        GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == request.Id)
            .Select(o => new OrderDetailDto(
                o.Id,
                o.ReferenceNumber,
                o.CustomerId,
                o.Customer.Name,
                o.Customer.Email,
                o.WarehouseId,
                o.Warehouse.Code,
                o.Status,
                o.OrderedAt,
                o.ConfirmedAt,
                o.CompletedAt,
                o.CancelledAt,
                o.TotalAmount,
                o.Notes,
                o.Items
                    .OrderBy(i => i.ProductName)
                    .Select(i => new OrderItemDto(
                        i.Id,
                        i.ProductId,
                        i.ProductSku,
                        i.ProductName,
                        i.Quantity,
                        i.UnitPrice,
                        i.UnitPrice * i.Quantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return order ?? throw new NotFoundException(nameof(Order), request.Id);
    }
}