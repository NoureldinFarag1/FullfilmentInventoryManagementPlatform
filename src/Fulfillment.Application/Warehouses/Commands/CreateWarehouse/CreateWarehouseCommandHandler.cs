using Fulfillment.Application.Common.Exceptions;
using Fulfillment.Application.Common.Interfaces;
using Fulfillment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Fulfillment.Application.Warehouses.Commands.CreateWarehouse;

public class CreateWarehouseCommandHandler : IRequestHandler<CreateWarehouseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    
    public CreateWarehouseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<Guid> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();

        var exists = await _context.Warehouses
            .AnyAsync(w => w.Code == code, cancellationToken);
        
        if(exists)
            throw new ConflictException($"A warehouse with code {code} already exists");
        
        var warehouse = new Warehouse {
            Code = code,
            Name = request.Name.Trim(),
            Address = request.Address?.Trim()
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);
        return warehouse.Id;
    }
}