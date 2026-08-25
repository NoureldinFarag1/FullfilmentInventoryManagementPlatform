using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Orders.Commands.AddOrderItem;

public class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand>
{
    private readonly IApplicationDbContext _context;

    public AddOrderItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o=>o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);
        
        var product = await _context.Products
            .FirstOrDefaultAsync(o => o.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var existingCount = order.Items.Count;
        var item = order.AddItem(product, request.Quantity);
        
        if(order.Items.Count > existingCount)
            _context.OrderItems.Add(item);
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}