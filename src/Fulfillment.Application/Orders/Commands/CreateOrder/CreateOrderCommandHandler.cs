using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateOrderCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: a repeated request with the same key returns the original
        // order rather than creating a second one.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingId = await _context.Orders
                .AsNoTracking()
                .Where(o => o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingId.HasValue)
                return existingId.Value;
        }

        var customerExists = await _context.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (!customerExists)
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        var warehouseExists = await _context.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId, cancellationToken);

        if (!warehouseExists)
            throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        var order = new Order(request.CustomerId, request.WarehouseId, request.IdempotencyKey);

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}