using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Products.Commands.SetProductStatus;

public class SetProductStatusCommandHandler : IRequestHandler<SetProductStatusCommand>
{
    private readonly IApplicationDbContext _context;
    
    public SetProductStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task Handle(SetProductStatusCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p=>p.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        if (product.IsActive == request.IsActive)
            return;
        
        product.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
    }
}