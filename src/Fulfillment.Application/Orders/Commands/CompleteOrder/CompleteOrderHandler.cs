using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fulfillment.Application.Orders.Commands.CompleteOrder;

public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CompleteOrderCommandHandler> _logger;

    public CompleteOrderCommandHandler(IApplicationDbContext context, ILogger<CompleteOrderCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
                    ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Stock already left the warehouse at confirmation, so completion
        // moves no inventory. It only closes the order.
        order.Complete();

        await _context.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Order {OrderId} completed successfully", order.Id);
    }
}